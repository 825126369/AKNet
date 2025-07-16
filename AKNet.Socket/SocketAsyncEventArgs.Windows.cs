using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AKNet.Socket
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        private long _asyncCompletionOwnership;
        private MemoryHandle _singleBufferHandle;
        private WSABuffer[]? _wsaBufferArrayPinned;
        private MemoryHandle[]? _multipleBufferMemoryHandles;
        private byte[]? _wsaMessageBufferPinned;
        private byte[]? _controlBufferPinned;
        private WSABuffer[]? _wsaRecvMsgWSABufferArrayPinned;
        private IntPtr _socketAddressPtr;
        private SafeFileHandle[]? _sendPacketsFileHandles;
        private PreAllocatedOverlapped _preAllocatedOverlapped;
        private readonly StrongBox<SocketAsyncEventArgs?> _strongThisRef = new StrongBox<SocketAsyncEventArgs?>();
        private CancellationTokenRegistration _registrationToCancelPendingIO;
        private unsafe NativeOverlapped* _pendingOverlappedForCancellation;

        private PinState _pinState;
        private enum PinState : byte
        {
            None = 0,
            MultipleBuffer,
            SendPackets
        }
        
        private void InitializeInternals()
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            _preAllocatedOverlapped = new PreAllocatedOverlapped(s_completionPortCallback, _strongThisRef, null);
        }

        private void FreeInternals()
        {
            FreePinHandles();
            FreeOverlapped();
        }

        private unsafe NativeOverlapped* AllocateNativeOverlapped()
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            Debug.Assert(_operating == OperationState_InProgress, $"Expected {nameof(_operating)} == {nameof(OperationState_InProgress)}, got {_operating}");
            Debug.Assert(_currentSocket != null, "_currentSocket is null");
            Debug.Assert(_currentSocket.SafeHandle != null, "_currentSocket.SafeHandle is null");
            Debug.Assert(_preAllocatedOverlapped != null, "_preAllocatedOverlapped is null");

            ThreadPoolBoundHandle boundHandle = _currentSocket.GetOrAllocateThreadPoolBoundHandle();
            return boundHandle.AllocateNativeOverlapped(_preAllocatedOverlapped);
        }

        private unsafe void FreeNativeOverlapped(ref NativeOverlapped* overlapped)
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            Debug.Assert(overlapped != null, "overlapped is null");
            Debug.Assert(_operating == OperationState_InProgress, $"Expected _operating == OperationState.InProgress, got {_operating}");
            Debug.Assert(_currentSocket != null, "_currentSocket is null");
            Debug.Assert(_currentSocket.SafeHandle != null, "_currentSocket.SafeHandle is null");
            Debug.Assert(_currentSocket.SafeHandle.IOCPBoundHandle != null, "_currentSocket.SafeHandle.IOCPBoundHandle is null");
            Debug.Assert(_preAllocatedOverlapped != null, "_preAllocatedOverlapped is null");

            _currentSocket.SafeHandle.IOCPBoundHandle.FreeNativeOverlapped(overlapped);
            overlapped = null;
        }

        partial void StartOperationCommonCore()
        {
            _strongThisRef.Value = this;
        }
        
        private unsafe SocketError GetIOCPResult(bool success, ref NativeOverlapped* overlapped)
        {
            if (success)
            {
                if (_currentSocket!.SafeHandle.SkipCompletionPortOnSuccess)
                {
                    FreeNativeOverlapped(ref overlapped);
                    return SocketError.Success;
                }
                return SocketError.IOPending;
            }
            else
            {
                SocketError socketError = SocketPal.GetLastSocketError();
                Debug.Assert(socketError != SocketError.Success);
                if (socketError != SocketError.IOPending)
                {
                    FreeNativeOverlapped(ref overlapped);
                    return socketError;
                }
                return SocketError.IOPending;
            }
        }
        
        private unsafe SocketError ProcessIOCPResult(bool success, int bytesTransferred, ref NativeOverlapped* overlapped, Memory<byte> bufferToPin, CancellationToken cancellationToken)
        {
            SocketError socketError = GetIOCPResult(success, ref overlapped);
            SocketFlags socketFlags = SocketFlags.None;

            if (socketError == SocketError.IOPending)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    Debug.Assert(_pendingOverlappedForCancellation == null);
                    _pendingOverlappedForCancellation = overlapped;
                    _registrationToCancelPendingIO = cancellationToken.Register(s =>
                    {
                        var thisRef = (SocketAsyncEventArgs)s!;
                        SafeSocketHandle handle = thisRef._currentSocket!.SafeHandle;
                        if (!handle.IsClosed)
                        {
                            try
                            {
                                bool canceled = Interop.Kernel32.CancelIoEx(handle, thisRef._pendingOverlappedForCancellation);
                            }
                            catch (ObjectDisposedException)
                            {
                                
                            }
                        }
                    }, this);
                }
                if (!bufferToPin.Equals(default))
                {
                    _singleBufferHandle = bufferToPin.Pin();
                }
                
                long packedResult = Interlocked.Exchange(ref _asyncCompletionOwnership, 1);
                if (packedResult == 0)
                {
                    return SocketError.IOPending;
                }
                
                Debug.Assert(((ulong)packedResult & 0x8000000000000000) != 0, "Top bit should have been set");
                bytesTransferred = (int)((packedResult >> 32) & 0x7FFFFFFF);
                socketError = (SocketError)(packedResult & 0xFFFFFFFF);
                if (socketError != SocketError.Success)
                {
                    GetOverlappedResultOnError(ref socketError, ref *(uint*)&bytesTransferred, ref socketFlags, overlapped);
                }
                FreeNativeOverlapped(ref overlapped);
            }
            
            FinishOperationSync(socketError, bytesTransferred, socketFlags);
            return socketError;
        }

        internal unsafe SocketError DoOperationReceiveMessageFrom(AKNetSocket socket, SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            _wsaMessageBufferPinned = new byte[sizeof(Interop.Winsock.WSAMsg)];
            IPAddress? ipAddress = (_socketAddress.Family == AddressFamily.InterNetworkV6 ? _socketAddress.GetIPAddress() : null);
            bool ipv4 = (_currentSocket!.AddressFamily == AddressFamily.InterNetwork || (ipAddress != null && ipAddress.IsIPv4MappedToIPv6)); // DualMode
            bool ipv6 = _currentSocket.AddressFamily == AddressFamily.InterNetworkV6;

            if (ipv6 && (_controlBufferPinned == null || _controlBufferPinned.Length != sizeof(Interop.Winsock.ControlDataIPv6)))
            {
                _controlBufferPinned = new byte[sizeof(Interop.Winsock.ControlDataIPv6)];
            }
            else if (ipv4 && (_controlBufferPinned == null || _controlBufferPinned.Length != sizeof(Interop.Winsock.ControlData)))
            {
                _controlBufferPinned = new byte[sizeof(Interop.Winsock.ControlData)];
            }
            
            WSABuffer[] wsaRecvMsgWSABufferArray;
            uint wsaRecvMsgWSABufferCount;
            if (_bufferList == null)
            {
                _wsaRecvMsgWSABufferArrayPinned ??= new WSABuffer[1];
                fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
                {
                    _wsaRecvMsgWSABufferArrayPinned[0].Pointer = (IntPtr)bufferPtr + _offset;
                    _wsaRecvMsgWSABufferArrayPinned[0].Length = _count;
                    wsaRecvMsgWSABufferArray = _wsaRecvMsgWSABufferArrayPinned;
                    wsaRecvMsgWSABufferCount = 1;
                    return Core();
                }
            }
            else
            {
                wsaRecvMsgWSABufferArray = _wsaBufferArrayPinned!;
                wsaRecvMsgWSABufferCount = (uint)_bufferListInternal.Count;
                return Core();
            }
            
            SocketError Core()
            {
                // Fill in WSAMessageBuffer.
                Interop.Winsock.WSAMsg* pMessage = (Interop.Winsock.WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0);
                pMessage->socketAddress = PtrSocketAddressBuffer();
                pMessage->addressLength = (uint)SocketAddress.GetMaximumAddressSize(_socketAddress!.Family);
                fixed (void* ptrWSARecvMsgWSABufferArray = &wsaRecvMsgWSABufferArray[0])
                {
                    pMessage->buffers = (IntPtr)ptrWSARecvMsgWSABufferArray;
                }
                pMessage->count = wsaRecvMsgWSABufferCount;

                if (_controlBufferPinned != null)
                {
                    Debug.Assert(_controlBufferPinned.Length > 0);
                    fixed (void* ptrControlBuffer = &_controlBufferPinned[0])
                    {
                        pMessage->controlBuffer.Pointer = (IntPtr)ptrControlBuffer;
                    }
                    pMessage->controlBuffer.Length = _controlBufferPinned.Length;
                }
                pMessage->flags = _socketFlags;

                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    SocketError socketError = socket.WSARecvMsg(
                        handle,
                        Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0),
                        out int bytesTransferred,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _bufferList == null ? _buffer : default, cancellationToken);
                }
                catch when (overlapped is not null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationSendTo(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            return _bufferList == null ? DoOperationSendToSingleBuffer(handle, cancellationToken) : DoOperationSendToMultiBuffer(handle);
        }

        internal unsafe SocketError DoOperationSendToSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    SocketError socketError = Interop.Winsock.WSASendTo(
                        handle,
                        ref wsaBuffer,
                        1,
                        out int bytesTransferred,
                        _socketFlags,
                        _socketAddress!.Buffer.Span,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _buffer, cancellationToken);
                }
                catch when (overlapped is not null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationSendToMultiBuffer(SafeSocketHandle handle)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                SocketError socketError = Interop.Winsock.WSASendTo(
                    handle,
                    _wsaBufferArrayPinned!,
                    _bufferListInternal!.Count,
                    out int bytesTransferred,
                    _socketFlags,
                    _socketAddress!.Buffer.Span,
                    overlapped,
                    IntPtr.Zero);

                return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, bufferToPin: default, cancellationToken: default);
            }
            catch when (overlapped is not null)
            {
                FreeNativeOverlapped(ref overlapped);
                throw;
            }
        }

        // Ensures Overlapped object exists with appropriate multiple buffers pinned.
        private void SetupMultipleBuffers()
        {
            if (_bufferListInternal == null || _bufferListInternal.Count == 0)
            {
                // No buffer list is set so unpin any existing multiple buffer pinning.
                if (_pinState == PinState.MultipleBuffer)
                {
                    FreePinHandles();
                }
            }
            else
            {
                // Need to setup a new Overlapped.
                FreePinHandles();
                try
                {
                    int bufferCount = _bufferListInternal.Count;

                    // Number of things to pin is number of buffers.
                    // Ensure we have properly sized object array.
                    if (_multipleBufferMemoryHandles == null || (_multipleBufferMemoryHandles.Length < bufferCount))
                    {
                        _multipleBufferMemoryHandles = new MemoryHandle[bufferCount];
                    }

                    // Pin the buffers.
                    for (int i = 0; i < bufferCount; i++)
                    {
                        _multipleBufferMemoryHandles[i] = _bufferListInternal[i].Array.AsMemory().Pin();
                    }

                    if (_wsaBufferArrayPinned == null || _wsaBufferArrayPinned.Length < bufferCount)
                    {
                        _wsaBufferArrayPinned = GC.AllocateUninitializedArray<WSABuffer>(bufferCount, pinned: true);
                    }

                    for (int i = 0; i < bufferCount; i++)
                    {
                        ArraySegment<byte> localCopy = _bufferListInternal[i];
                        _wsaBufferArrayPinned[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(localCopy.Array!, localCopy.Offset);
                        _wsaBufferArrayPinned[i].Length = localCopy.Count;
                    }

                    _pinState = PinState.MultipleBuffer;
                }
                catch (Exception)
                {
                    FreePinHandles();
                    throw;
                }
            }
        }
        
        private unsafe void AllocateSocketAddressBuffer()
        {
            //_socketAddress!.Size = SocketAddress.GetMaximumAddressSize(_socketAddress!.Family);
            int size = SocketAddress.GetMaximumAddressSize(_socketAddress!.Family);

            if (_socketAddressPtr == IntPtr.Zero)
            {
                _socketAddressPtr = (IntPtr)NativeMemory.Alloc((uint)(_socketAddress!.Size + sizeof(IntPtr)));
            }

            *((int*)_socketAddressPtr) = size;
        }

        private unsafe IntPtr PtrSocketAddressBuffer()
        {
            Debug.Assert(_socketAddressPtr != IntPtr.Zero);
            return _socketAddressPtr + sizeof(IntPtr);
        }

        private IntPtr PtrSocketAddressSize()
        {
            Debug.Assert(_socketAddressPtr != IntPtr.Zero);
            return _socketAddressPtr;
        }

        // Cleans up any existing Overlapped object and related state variables.
        private void FreeOverlapped()
        {
            // Free the preallocated overlapped object. This in turn will unpin
            // any pinned buffers.
            if (_preAllocatedOverlapped != null)
            {
                Debug.Assert(OperatingSystem.IsWindows());
                _preAllocatedOverlapped.Dispose();
                _preAllocatedOverlapped = null!;
            }
        }

        private unsafe void FreePinHandles()
        {
            _pinState = PinState.None;

            if (_multipleBufferMemoryHandles != null)
            {
                for (int i = 0; i < _multipleBufferMemoryHandles.Length; i++)
                {
                    _multipleBufferMemoryHandles[i].Dispose();
                    _multipleBufferMemoryHandles[i] = default;
                }
            }

            if (_socketAddressPtr != IntPtr.Zero)
            {
                NativeMemory.Free((void*)_socketAddressPtr);
                _socketAddressPtr = IntPtr.Zero;
            }

            Debug.Assert(_singleBufferHandle.Equals(default(MemoryHandle)));
        }

        internal unsafe void LogBuffer(int size)
        {
            if (_bufferList != null)
            {
                for (int i = 0; i < _bufferListInternal!.Count; i++)
                {
                    WSABuffer wsaBuffer = _wsaBufferArrayPinned![i];
                    NetEventSource.DumpBuffer(this, new ReadOnlySpan<byte>((byte*)wsaBuffer.Pointer, Math.Min(wsaBuffer.Length, size)));
                    if ((size -= wsaBuffer.Length) <= 0)
                    {
                        break;
                    }
                }
            }
            else if (_buffer.Length != 0)
            {
                    
            }
        }

        private unsafe void UpdateReceivedSocketAddress(SocketAddress socketAddress)
        {
            Debug.Assert(_socketAddressPtr != IntPtr.Zero);
            int size = *((int*)_socketAddressPtr);
            socketAddress!.Size = size;
            new Span<byte>((void*)PtrSocketAddressBuffer(), size).CopyTo(socketAddress.Buffer.Span);
        }

        private void CompleteCore()
        {
            _strongThisRef.Value = null; // null out this reference from the overlapped so this isn't kept alive artificially

            if (_asyncCompletionOwnership != 0)
            {
                // If the state isn't 0, then the operation didn't complete synchronously, in which case there's state to cleanup.
                CleanupIOCPResult();
            }

            // Separate out to help inline the CompleteCore fast path, as CompleteCore is used with all operations.
            // We want to optimize for the case where the async operation actually completes synchronously, without
            // having registered any state yet, in particular for sends and receives.
            void CleanupIOCPResult()
            {
                // Remove any cancellation state.  First dispose the registration
                // to ensure that cancellation will either never fine or will have completed
                // firing before we continue.  Only then can we safely null out the overlapped.
                _registrationToCancelPendingIO.Dispose();
                _registrationToCancelPendingIO = default;
                unsafe
                {
                    _pendingOverlappedForCancellation = null;
                }

                // Release any GC handles.
                _singleBufferHandle.Dispose();
                _singleBufferHandle = default;

                // Finished cleanup.
                _asyncCompletionOwnership = 0;
            }
        }

        private unsafe void FinishOperationReceiveMessageFrom()
        {
            Interop.Winsock.WSAMsg* PtrMessage = (Interop.Winsock.WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned!, 0);
            _socketFlags = PtrMessage->flags;

            if (_controlBufferPinned!.Length == sizeof(Interop.Winsock.ControlData))
            {
                // IPv4.
                _receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((Interop.Winsock.ControlData*)PtrMessage->controlBuffer.Pointer);
            }
            else if (_controlBufferPinned.Length == sizeof(Interop.Winsock.ControlDataIPv6))
            {
                // IPv6.
                _receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((Interop.Winsock.ControlDataIPv6*)PtrMessage->controlBuffer.Pointer);
            }
            else
            {
                // Other.
                _receiveMessageFromPacketInfo = default;
            }
        }

        private void FinishOperationSendPackets()
        {
            // Close the files if open.
            if (_sendPacketsFileHandles != null)
            {
                for (int i = 0; i < _sendPacketsFileHandles.Length; i++)
                {
                    _sendPacketsFileHandles[i]?.Dispose();
                }

                _sendPacketsFileHandles = null;
            }
        }

        private static readonly unsafe IOCompletionCallback s_completionPortCallback = delegate (uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            var saeaBox = (StrongBox<SocketAsyncEventArgs>)(ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped)!);

            Debug.Assert(saeaBox.Value != null);
            SocketAsyncEventArgs saea = saeaBox.Value;

            if (saea._asyncCompletionOwnership == 0)
            {
                Debug.Assert(numBytes <= int.MaxValue, "We rely on being able to set the top bit to ensure the whole packed result isn't 0.");
                long packedResult = (long)((1ul << 63) | ((ulong)numBytes << 32) | errorCode);
                if (Interlocked.Exchange(ref saea._asyncCompletionOwnership, packedResult) == 0)
                {
                    return;
                }
            }
            
            if ((SocketError)errorCode == SocketError.Success)
            {
                saea.FreeNativeOverlapped(ref nativeOverlapped);
                saea.FinishOperationAsyncSuccess((int)numBytes, SocketFlags.None);
            }
            else
            {
                SocketError socketError = (SocketError)errorCode;
                SocketFlags socketFlags = SocketFlags.None;
                saea.GetOverlappedResultOnError(ref socketError, ref numBytes, ref socketFlags, nativeOverlapped);

                saea.FreeNativeOverlapped(ref nativeOverlapped);
                saea.FinishOperationAsyncFailure(socketError, (int)numBytes, socketFlags);
            }
        };

        private unsafe void GetOverlappedResultOnError(ref SocketError socketError, ref uint numBytes, ref SocketFlags socketFlags, NativeOverlapped* nativeOverlapped)
        {
            if (socketError != SocketError.OperationAborted)
            {
                if (_currentSocket!.Disposed)
                {
                    socketError = SocketError.OperationAborted;
                }
                else
                {
                    try
                    {
                        Interop.Winsock.WSAGetOverlappedResult(_currentSocket.SafeHandle, nativeOverlapped, out numBytes, wait: false, out socketFlags);
                        socketError = SocketPal.GetLastSocketError();
                    }
                    catch
                    {
                        socketError = SocketError.OperationAborted;
                    }
                }
            }
        }
    }
}
