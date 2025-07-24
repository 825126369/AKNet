#if TARGET_WINDOWS
namespace AKNet.Platform
{
    using System.Runtime.InteropServices;
    using CXPLAT_CQE = OVERLAPPED_ENTRY;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct OVERLAPPED_ENTRY
    {
        public ulong lpCompletionKey;
        public OVERLAPPED* lpOverlapped;
        public ulong Internal;
        public int dwNumberOfBytesTransferred;
    }

    public unsafe struct OVERLAPPED
    {
        public ulong Internal;
        public ulong InternalHigh;

        [StructLayout(LayoutKind.Explicit)]
        public struct DUMMYUNIONNAME_DATA1
        {
            public struct DUMMYSTRUCTNAME_DATA2
            {
                public int Offset;
                public int OffsetHigh;
            }
            [FieldOffset(0)] public DUMMYSTRUCTNAME_DATA2 DUMMYUNIONNAME;
            [FieldOffset(0)] public void* Pointer;
        }
        public DUMMYUNIONNAME_DATA1 DUMMYUNIONNAME;
        public IntPtr hEvent;
    }

    public class CXPLAT_EVENTQ
    {
        internal IntPtr Queue;
        internal readonly CXPLAT_CQE[] events_inner = new CXPLAT_CQE[13];
        public readonly CXPLAT_SQE[] events = new CXPLAT_SQE[13];
        public int events_count = 0;
    }

    public unsafe class CXPLAT_SQE
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct CXPLAT_SQE_Inner
        {
            public const string Overlapped_FieldName = "Overlapped";
            public OVERLAPPED Overlapped;
            public CXPLAT_SQE parent;
#if DEBUG
            public bool IsQueued;
#endif
        }

        public CXPLAT_SQE_Inner* sqePtr;
        public Action<CXPLAT_CQE> Completion; //这个主要是给外部程序使用的
        public object Contex;
    }

    public static unsafe partial class OSPlatformFunc
    {
        public static bool CxPlatEventQInitialize(CXPLAT_EVENTQ queue)
        {
            queue.Queue = Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, IntPtr.Zero, 1);
            return queue.Queue != IntPtr.Zero;
        }

        public static void CxPlatEventQCleanup(CXPLAT_EVENTQ queue)
        {
            Interop.Kernel32.CloseHandle(queue.Queue);
        }

        public static bool CxPlatEventQAssociateHandle(CXPLAT_EVENTQ queue, IntPtr fileHandle)
        {
            return queue.Queue == Interop.Kernel32.CreateIoCompletionPort(fileHandle, queue.Queue, IntPtr.Zero, 0);
        }

        public static bool CxPlatEventQEnqueue(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe)
        {
#if DEBUG
            NetLog.Assert(!sqe.sqePtr->IsQueued);
#endif
            CxPlatZeroMemory(&sqe.sqePtr->Overlapped, sizeof(OVERLAPPED));
            return Interop.Kernel32.PostQueuedCompletionStatus(queue.Queue, 0, IntPtr.Zero, &sqe.sqePtr->Overlapped);
        }

        public static bool CxPlatEventQEnqueueEx(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe, int num_bytes)
        {
#if DEBUG
            NetLog.Assert(!sqe.sqePtr->IsQueued);
#endif
            CxPlatZeroMemory(&sqe.sqePtr->Overlapped, sizeof(OVERLAPPED));
            return Interop.Kernel32.PostQueuedCompletionStatus(queue.Queue, (uint)num_bytes, IntPtr.Zero, &sqe.sqePtr->Overlapped);
        }

        public static int CxPlatEventQDequeueEx(CXPLAT_EVENTQ queue, int wait_time)
        {
            fixed (CXPLAT_CQE* eventPtr = queue.events_inner)
            {
                int out_count = 0;
                if (!Interop.Kernel32.GetQueuedCompletionStatusEx(queue.Queue, eventPtr, queue.events_inner.Length, out out_count, wait_time, false))
                {
                    return 0;
                }

                NetLog.Assert(out_count != 0);
                NetLog.Assert(queue.events_inner[0].lpOverlapped != null || out_count == 1);
#if DEBUG
                if (queue.events_inner[0].lpOverlapped != null)
                {
                    for (int i = 0; i < out_count; ++i)
                    {
                        CXPLAT_SQE.CXPLAT_SQE_Inner* data = CXPLAT_CONTAINING_RECORD<CXPLAT_SQE.CXPLAT_SQE_Inner>(
                            queue.events_inner[i].lpOverlapped, CXPLAT_SQE.CXPLAT_SQE_Inner.Overlapped_FieldName);
                        data->IsQueued = true;
                        queue.events[i] = data->parent;
                    }
                }
#endif
                return queue.events_inner[0].lpOverlapped == null ? 0 : out_count;
            }
        }

        public static void CxPlatEventQReturn(CXPLAT_EVENTQ queue, int count)
        {

        }

        public static bool CxPlatSqeInitialize(CXPLAT_EVENTQ queue, Action<CXPLAT_CQE> completion, object contex, CXPLAT_SQE sqe)
        {
            sqe.Contex = contex;
            sqe.Completion = completion;
            sqe.sqePtr = (CXPLAT_SQE.CXPLAT_SQE_Inner*)Interop.Ucrtbase.malloc(sizeof(CXPLAT_SQE.CXPLAT_SQE_Inner));
            sqe.sqePtr->parent = sqe;
            CxPlatZeroMemory(sqe.sqePtr, sizeof(CXPLAT_SQE.CXPLAT_SQE_Inner));
#if DEBUG
            sqe.sqePtr->IsQueued = false;
#endif
            return true;
        }

        public static void CxPlatSqeInitializeEx(Action<CXPLAT_CQE> completion, object contex, CXPLAT_SQE sqe)
        {
            sqe.Contex = contex;
            sqe.Completion = completion;
            sqe.sqePtr = (CXPLAT_SQE.CXPLAT_SQE_Inner*)Interop.Ucrtbase.malloc(sizeof(CXPLAT_SQE.CXPLAT_SQE_Inner));
            sqe.sqePtr->parent = sqe;
            CxPlatZeroMemory(&sqe.sqePtr->Overlapped, sizeof(OVERLAPPED));
#if DEBUG
            sqe.sqePtr->IsQueued = false;
#endif
        }

        public static void CxPlatSqeCleanup(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe)
        {
            if (sqe.sqePtr != null)
            {
                Interop.Ucrtbase.free(sqe.sqePtr);
                sqe.sqePtr = null;
            }
        }

        public static CXPLAT_SQE CxPlatCqeGetSqe(CXPLAT_CQE cqe)
        {
            CXPLAT_SQE.CXPLAT_SQE_Inner* data = CXPLAT_CONTAINING_RECORD<CXPLAT_SQE.CXPLAT_SQE_Inner>(cqe.lpOverlapped, CXPLAT_SQE.CXPLAT_SQE_Inner.Overlapped_FieldName);
            return data->parent;
        }
    }
}
#endif