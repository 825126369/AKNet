using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if TARGET_WINDOWS
namespace AKNet.Socket
{
    internal sealed partial class PortableThreadPool
    {
        private readonly IntPtr[] _ioPorts = new IntPtr[IOCompletionPortCount];
        private int _ioPortSelectorForRegister = int.MaxValue;
        private int _ioPortSelectorForQueue = int.MaxValue;
        private IOCompletionPoller[]? _ioCompletionPollers;

        private static short DetermineIOCompletionPortCount()
        {
            const short DefaultIOPortCount = 1;
            const short MaxIOPortCount = 1 << 10;
            short ioPortCount = DefaultIOPortCount;
            return ioPortCount == 0 ? DefaultIOPortCount : Math.Min(ioPortCount, MaxIOPortCount);
        }

        private static int DetermineIOCompletionPollerCount()
        {
            int ioPollerCount = Environment.ProcessorCount;
            if (IOCompletionPortCount == 1)
            {
                return ioPollerCount;
            }
            
            if (ioPollerCount <= IOCompletionPortCount)
            {
                return IOCompletionPortCount;
            }

            int rem = ioPollerCount % IOCompletionPortCount;
            if (rem != 0)
            {
                ioPollerCount += IOCompletionPortCount - rem;
            }

            return ioPollerCount;
        }

        private void InitializeIOOnWindows()
        {
            Debug.Assert(IOCompletionPollerCount % IOCompletionPortCount == 0);
            int numConcurrentThreads = IOCompletionPollerCount / IOCompletionPortCount;
            for (int i = 0; i < IOCompletionPortCount; i++)
            {
                _ioPorts[i] = CreateIOCompletionPort(numConcurrentThreads);
            }
        }

        private static nint CreateIOCompletionPort(int numConcurrentThreads)
        {
            nint port = Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, UIntPtr.Zero, numConcurrentThreads);
            if (port == 0)
            {
                int hr = Marshal.GetHRForLastWin32Error();
                Environment.FailFast($"Failed to create an IO completion port. HR: {hr}");
            }
            return port;
        }

        public void RegisterForIOCompletionNotifications(nint handle)
        {
            Debug.Assert(_ioPorts != null);
            if (_ioCompletionPollers == null)
            {
                EnsureIOCompletionPollers();
            }

            int selectedPortIndex = IOCompletionPortCount == 1
                    ? 0
                    : Interlocked.Increment(ref _ioPortSelectorForRegister) % (int)IOCompletionPortCount;

            nint selectedPort = _ioPorts[selectedPortIndex];
            Debug.Assert(selectedPort != 0);
            IntPtr port = Interop.Kernel32.CreateIoCompletionPort(handle, selectedPort, UIntPtr.Zero, 0);
            if (port == IntPtr.Zero)
            {
                throw new Exception();
            }

            Debug.Assert(port == selectedPort);
        }

        public unsafe void QueueNativeOverlapped(NativeOverlapped* nativeOverlapped)
        {
            Debug.Assert(nativeOverlapped != null);
            Debug.Assert(_ioPorts != null);

            if (_ioCompletionPollers == null)
            {
                EnsureIOCompletionPollers();
            }

            int selectedPortIndex = IOCompletionPortCount == 1
                    ? 0
                    : Interlocked.Increment(ref _ioPortSelectorForQueue) % (int)IOCompletionPortCount;
            nint selectedPort = _ioPorts[selectedPortIndex];
            Debug.Assert(selectedPort != 0);
            if (!Interop.Kernel32.PostQueuedCompletionStatus(selectedPort, 0, UIntPtr.Zero, (IntPtr)nativeOverlapped))
            {
                throw new Exception();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureIOCompletionPollers()
        {
            _threadAdjustmentLock.Acquire();
            try
            {
                if (_ioCompletionPollers != null)
                {
                    return;
                }

                IOCompletionPoller[] pollers = new IOCompletionPoller[IOCompletionPollerCount];
                for (int i = 0; i < IOCompletionPollerCount; ++i)
                {
                    pollers[i] = new IOCompletionPoller(_ioPorts[i % IOCompletionPortCount]);
                }

                _ioCompletionPollers = pollers;
            }
            catch (Exception ex)
            {
                Environment.FailFast("Failed to initialize IO completion pollers.", ex);
            }
            finally
            {
                _threadAdjustmentLock.Release();
            }
        }

        private sealed unsafe class IOCompletionPoller
        {
            private const int NativeEventCapacity =
#if DEBUG
                32;
#else
                1024;
#endif

            private readonly nint _port;
            private readonly Interop.Kernel32.OVERLAPPED_ENTRY* _nativeEvents;
            private readonly ThreadPoolTypedWorkItemQueue<Event, Callback>? _events;
            private readonly Thread _thread;

            public IOCompletionPoller(nint port)
            {
                Debug.Assert(port != 0);
                _port = port;

                if (!UnsafeInlineIOCompletionCallbacks)
                {
                    _nativeEvents =
                        (Interop.Kernel32.OVERLAPPED_ENTRY*)
                        NativeMemory.Alloc(NativeEventCapacity, (nuint)sizeof(Interop.Kernel32.OVERLAPPED_ENTRY));
                    _events = new ThreadPoolTypedWorkItemQueue<Event, Callback>();
                    _thread = new Thread(Poll, SmallStackSizeBytes);
                    if (IOCompletionPollerCount * 4 < Environment.ProcessorCount)
                    {
                        _thread.Priority = ThreadPriority.AboveNormal;
                    }
                }
                else
                {
                    // These threads may run user code, use the default stack size
                    _thread = new Thread(PollAndInlineCallbacks);
                }

                _thread.IsThreadPoolThread = true;
                _thread.IsBackground = true;
                _thread.Name = ".NET ThreadPool IO";
                _thread.UnsafeStart();
            }

            private void Poll()
            {
                Debug.Assert(_nativeEvents != null);
                Debug.Assert(_events != null);

                while (
                    Interop.Kernel32.GetQueuedCompletionStatusEx(
                        _port,
                        _nativeEvents,
                        NativeEventCapacity,
                        out int nativeEventCount,
                        Timeout.Infinite,
                        false))
                {
                    Debug.Assert(nativeEventCount > 0);
                    Debug.Assert(nativeEventCount <= NativeEventCapacity);

                    for (int i = 0; i < nativeEventCount; ++i)
                    {
                        Interop.Kernel32.OVERLAPPED_ENTRY* nativeEvent = &_nativeEvents[i];
                        if (nativeEvent->lpOverlapped != null) // shouldn't be null since null is not posted
                        {
                            _events.BatchEnqueue(new Event(nativeEvent->lpOverlapped, nativeEvent->dwNumberOfBytesTransferred));
                        }
                    }

                    _events.CompleteBatchEnqueue();
                }

                ThrowHelper.ThrowApplicationException(Marshal.GetHRForLastWin32Error());
            }

            private void PollAndInlineCallbacks()
            {
                Debug.Assert(_nativeEvents == null);
                Debug.Assert(_events == null);

                while (true)
                {
                    uint errorCode = Interop.Errors.ERROR_SUCCESS;
                    if (!Interop.Kernel32.GetQueuedCompletionStatus(
                            _port,
                            out uint bytesTransferred,
                            out _,
                            out nint nativeOverlappedPtr,
                            Timeout.Infinite))
                    {
                        errorCode = (uint)Marshal.GetLastPInvokeError();
                    }

                    var nativeOverlapped = (NativeOverlapped*)nativeOverlappedPtr;
                    if (nativeOverlapped == null) // shouldn't be null since null is not posted
                    {
                        continue;
                    }

                    if (NativeRuntimeEventSource.Log.IsEnabled())
                    {
                        NativeRuntimeEventSource.Log.ThreadPoolIODequeue(nativeOverlapped);
                    }

                    IOCompletionCallbackHelper.PerformSingleIOCompletionCallback(errorCode, bytesTransferred, nativeOverlapped);
                }
            }

            private struct Callback : IThreadPoolTypedWorkItemQueueCallback<Event>
            {
                public static void Invoke(Event e)
                {
                    if (NativeRuntimeEventSource.Log.IsEnabled())
                    {
                        NativeRuntimeEventSource.Log.ThreadPoolIODequeue(e.nativeOverlapped);
                    }

                    // The NtStatus code for the operation is in the InternalLow field
                    uint ntStatus = (uint)(nint)e.nativeOverlapped->InternalLow;
                    uint errorCode = Interop.Errors.ERROR_SUCCESS;
                    if (!Interop.StatusOptions.NT_SUCCESS(ntStatus))
                    {
                        errorCode = Interop.NtDll.RtlNtStatusToDosError((int)ntStatus);
                    }

                    IOCompletionCallbackHelper.PerformSingleIOCompletionCallback(errorCode, e.bytesTransferred, e.nativeOverlapped);
                }
            }

            private readonly struct Event
            {
                public readonly NativeOverlapped* nativeOverlapped;
                public readonly uint bytesTransferred;

                public Event(NativeOverlapped* nativeOverlapped, uint bytesTransferred)
                {
                    this.nativeOverlapped = nativeOverlapped;
                    this.bytesTransferred = bytesTransferred;
                }
            }
        }
    }
}
#endif
