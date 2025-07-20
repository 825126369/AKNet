#if TARGET_WINDOWS

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AKNet.Platform.Interop.Kernel32;

namespace AKNet.Platform.Socket
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        private long _asyncCompletionOwnership;
        private MemoryHandle _singleBufferHandle;
        private WSABUF[]? _wsaBufferArrayPinned;
        private MemoryHandle[]? _multipleBufferMemoryHandles;
        private byte[]? _wsaMessageBufferPinned;
        private byte[]? _controlBufferPinned;
        private WSABUF[]? _wsaRecvMsgWSABufferArrayPinned;
        private IntPtr _socketAddressPtr;
        private IntPtr[]? _sendPacketsFileHandles;
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

        private unsafe OVERLAPPED* AllocateNativeOverlapped()
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            Debug.Assert(_operating == OperationState_InProgress, $"Expected {nameof(_operating)} == {nameof(OperationState_InProgress)}, got {_operating}");
            Debug.Assert(_currentSocket != null, "_currentSocket is null");
            Debug.Assert(_currentSocket.SafeHandle != null, "_currentSocket.SafeHandle is null");
            Debug.Assert(_preAllocatedOverlapped != null, "_preAllocatedOverlapped is null");

            return null;
            //ThreadPoolBoundHandle boundHandle = _currentSocket.GetOrAllocateThreadPoolBoundHandle();
            //return boundHandle.AllocateNativeOverlapped(_preAllocatedOverlapped);
        }

        private unsafe void FreeNativeOverlapped(ref OVERLAPPED* overlapped)
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            Debug.Assert(overlapped != null, "overlapped is null");
            Debug.Assert(_operating == OperationState_InProgress, $"Expected _operating == OperationState.InProgress, got {_operating}");
            Debug.Assert(_currentSocket != null, "_currentSocket is null");
            Debug.Assert(_currentSocket.SafeHandle != null, "_currentSocket.SafeHandle is null");
            Debug.Assert(_preAllocatedOverlapped != null, "_preAllocatedOverlapped is null");
            overlapped = null;
        }

        void StartOperationCommonCore()
        {
            _strongThisRef.Value = this;
        }

        private unsafe SocketError GetIOCPResult(bool success, ref NativeOverlapped* overlapped)
        {
            return SocketError.Success;
            //if (success)
            //{
            //    if (_currentSocket!.SafeHandle)
            //    {
            //        FreeNativeOverlapped(ref overlapped);
            //        return SocketError.Success;
            //    }
            //    return SocketError.IOPending;
            //}
            //else
            //{
            //    SocketError socketError = SocketPal.GetLastSocketError();
            //    Debug.Assert(socketError != SocketError.Success);
            //    if (socketError != SocketError.IOPending)
            //    {
            //        FreeNativeOverlapped(ref overlapped);
            //        return socketError;
            //    }
            //    return SocketError.IOPending;
            //}
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

        internal unsafe SocketError DoOperationReceiveMessageFrom(Socket socket, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            _wsaMessageBufferPinned = new byte[sizeof(WSAMsg)];
            if (_controlBufferPinned == null || _controlBufferPinned.Length != sizeof(ControlDataIPv6))
            {
                _controlBufferPinned = new byte[sizeof(ControlDataIPv6)];
            }

            WSABUF[] wsaRecvMsgWSABufferArray;
            uint wsaRecvMsgWSABufferCount;
            if (_bufferList == null)
            {
                _wsaRecvMsgWSABufferArrayPinned = new WSABUF[1];
                fixed (byte* bufferPtr = _buffer.Span.Slice(_offset))
                {
                    _wsaRecvMsgWSABufferArrayPinned[0].buf = (IntPtr)bufferPtr;
                    _wsaRecvMsgWSABufferArrayPinned[0].len = _count;
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
                WSAMsg* pMessage = (WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0);
                pMessage->socketAddress = PtrSocketAddressBuffer();
                pMessage->addressLength = (uint)SocketAddress.GetMaximumAddressSize(_socketAddress.Family);

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
                        pMessage->controlBuffer.buf = (IntPtr)ptrControlBuffer;
                    }
                    pMessage->controlBuffer.len = _controlBufferPinned.Length;
                }
                pMessage->flags = _socketFlags;

                OVERLAPPED* overlapped = AllocateNativeOverlapped();
                try
                {
                    SocketError socketError = Interop.Winsock.WSARecvFrom(
                        socket.SafeHandle,
                        Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0),
                        out int bytesTransferred,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _bufferList == null ? _buffer : default, cancellationToken);
                }
                catch when (overlapped != null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationSendTo(IntPtr handle, CancellationToken cancellationToken)
        {
            return _bufferList == null ? DoOperationSendToSingleBuffer(handle, cancellationToken) : DoOperationSendToMultiBuffer(handle);
        }

        internal unsafe SocketError DoOperationSendToSingleBuffer(IntPtr handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            fixed (byte* bufferPtr = _buffer.Span.Slice(_offset))
            {
                Overlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    var wsaBuffer = new WSABUF { len = _count, buf = (IntPtr)bufferPtr };

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

        internal unsafe SocketError DoOperationSendToMultiBuffer(IntPtr handle)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            Overlapped* overlapped = AllocateNativeOverlapped();
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

        private void SetupMultipleBuffers()
        {
            if (_bufferListInternal == null || _bufferListInternal.Count == 0)
            {
                if (_pinState == PinState.MultipleBuffer)
                {
                    FreePinHandles();
                }
            }
            else
            {
                FreePinHandles();
                try
                {
                    int bufferCount = _bufferListInternal.Count;
                    if (_multipleBufferMemoryHandles == null || (_multipleBufferMemoryHandles.Length < bufferCount))
                    {
                        _multipleBufferMemoryHandles = new MemoryHandle[bufferCount];
                    }

                    for (int i = 0; i < bufferCount; i++)
                    {
                        _multipleBufferMemoryHandles[i] = _bufferListInternal[i].Array.AsMemory().Pin();
                    }

                    if (_wsaBufferArrayPinned == null || _wsaBufferArrayPinned.Length < bufferCount)
                    {
                        _wsaBufferArrayPinned = new WSABuffer[bufferCount];
                    }

                    for (int i = 0; i < bufferCount; i++)
                    {
                        ArraySegment<byte> localCopy = _bufferListInternal[i];
                        _wsaBufferArrayPinned[i].buf = Marshal.UnsafeAddrOfPinnedArrayElement(localCopy.Array!, localCopy.Offset);
                        _wsaBufferArrayPinned[i].len = localCopy.Count;
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
            int size = SocketAddress.GetMaximumAddressSize(_socketAddress!.Family);
            if (_socketAddressPtr == IntPtr.Zero)
            {
                _socketAddressPtr = (IntPtr)Marshal.AllocHGlobal((int)(_socketAddress!.Size + sizeof(IntPtr)));
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

        private void FreeOverlapped()
        {
            if (_preAllocatedOverlapped != null)
            {
                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
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
                    WSABUF wsaBuffer = _wsaBufferArrayPinned![i];
                    if ((size -= wsaBuffer.len) <= 0)
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
            _strongThisRef.Value = null;
            if (_asyncCompletionOwnership != 0)
            {
                CleanupIOCPResult();
            }

            void CleanupIOCPResult()
            {
                _registrationToCancelPendingIO.Dispose();
                _registrationToCancelPendingIO = default;
                unsafe
                {
                    _pendingOverlappedForCancellation = null;
                }

                _singleBufferHandle.Dispose();
                _singleBufferHandle = default;
                _asyncCompletionOwnership = 0;
            }
        }

        private unsafe void FinishOperationReceiveMessageFrom()
        {
            WSAMsg* PtrMessage = (WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned!, 0);
            _socketFlags = PtrMessage->flags;
            if (_controlBufferPinned!.Length == sizeof(ControlData))
            {
                _receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((ControlData*)PtrMessage->controlBuffer.buf);
            }
            else if (_controlBufferPinned.Length == sizeof(ControlDataIPv6))
            {
                _receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((ControlDataIPv6*)PtrMessage->controlBuffer.buf);
            }
            else
            {
                _receiveMessageFromPacketInfo = default;
            }
        }

        public static readonly unsafe IOCompletionCallback s_completionPortCallback = delegate(uint errorCode, uint numBytes, Overlapped* nativeOverlapped)
        {
            StrongBox<SocketAsyncEventArgs> saeaBox = null;
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


        private unsafe void GetOverlappedResultOnError(ref SocketError socketError, ref uint numBytes, ref SocketFlags socketFlags, Overlapped* nativeOverlapped)
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
#endif
