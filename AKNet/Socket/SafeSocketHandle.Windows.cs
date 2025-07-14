using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AKNet.Socket
{
    public partial class SafeSocketHandle
    {
        private ThreadPoolBoundHandle? _iocpBoundHandle;
        private bool _skipCompletionPortOnSuccess;
        internal void SetExposed() { }

        internal ThreadPoolBoundHandle? IOCPBoundHandle
        {
            get
            {
                return _iocpBoundHandle;
            }
        }

        internal ThreadPoolBoundHandle? GetThreadPoolBoundHandle() => !_released ? _iocpBoundHandle : null;
        internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandle(bool trySkipCompletionPortOnSuccess)
        {
            if (_released)
            {
                ThrowSocketDisposedException();
            }

            if (_iocpBoundHandle != null)
            {
                return _iocpBoundHandle;
            }

            lock (this)
            {
                ThreadPoolBoundHandle boundHandle = _iocpBoundHandle;
                if (boundHandle == null)
                {
                    try
                    {
                        Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
                        boundHandle = ThreadPoolBoundHandle.BindHandle(this);
                    }
                    catch (Exception exception)
                    {
                        bool closed = IsClosed;
                        bool alreadyBound = !IsInvalid && !IsClosed && (exception is ArgumentException);
                        CloseAsIs(abortive: false);
                        if (closed)
                        {
                            ThrowSocketDisposedException(exception);
                        }

                        if (alreadyBound)
                        {
                            throw new InvalidOperationException();
                        }

                        throw;
                    }

                    if (trySkipCompletionPortOnSuccess && CompletionPortHelper.SkipCompletionPortOnSuccess(boundHandle.Handle))
                    {
                        _skipCompletionPortOnSuccess = true;
                    }

                    Volatile.Write(ref _iocpBoundHandle, boundHandle);
                }
                return boundHandle;
            }
        }

        internal bool SkipCompletionPortOnSuccess
        {
            get
            {
                Debug.Assert(_iocpBoundHandle != null);
                return _skipCompletionPortOnSuccess;
            }
        }
        
        private unsafe bool OnHandleClose()
        {
            if (_iocpBoundHandle != null)
            {
                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
                if (!OwnsHandle)
                {
                    Kernel32.CancelIoEx(handle, null);
                }

                _iocpBoundHandle.Dispose();
            }

            // On Unix, we use the return value of OnHandleClose to cause an abortive close.
            // On Windows, this is handled in TryUnblockSocket.
            return false;
        }

        /// <returns>Returns whether operations were canceled.</returns>
        private unsafe bool TryUnblockSocket(bool _ /*abortive*/)
        {
            // Try to cancel all pending IO.
            return Interop.Kernel32.CancelIoEx(handle, null);
        }

        private SocketError DoCloseHandle(bool abortive)
        {
            SocketError errorCode;

            // If abortive is not set, we're not running on the finalizer thread, so it's safe to block here.
            // We can honor the linger options set on the socket.  It also means closesocket() might return
            // WSAEWOULDBLOCK, in which case we need to do some recovery.
            if (!abortive)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle}, Following 'blockable' branch");
                errorCode = Interop.Winsock.closesocket(handle);
#if DEBUG
                _closeSocketResult = errorCode;
#endif
                if (errorCode == SocketError.SocketError) errorCode = (SocketError)Marshal.GetLastPInvokeError();

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle}, closesocket()#1:{errorCode}");

                // If it's not WSAEWOULDBLOCK, there's no more recourse - we either succeeded or failed.
                if (errorCode != SocketError.WouldBlock)
                {
                    return errorCode;
                }

                // The socket must be non-blocking with a linger timeout set.
                // We have to set the socket to blocking.
                int nonBlockCmd = 0;
                errorCode = Interop.Winsock.ioctlsocket(
                    handle,
                    Interop.Winsock.IoctlSocketConstants.FIONBIO,
                    ref nonBlockCmd);
                if (errorCode == SocketError.SocketError) errorCode = (SocketError)Marshal.GetLastPInvokeError();

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle}, ioctlsocket()#1:{errorCode}");

                // If that succeeded, try again.
                if (errorCode == SocketError.Success)
                {
                    errorCode = Interop.Winsock.closesocket(handle);
#if DEBUG
                    _closeSocketResult = errorCode;
#endif
                    if (errorCode == SocketError.SocketError) errorCode = (SocketError)Marshal.GetLastPInvokeError();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle}, closesocket#2():{errorCode}");

                    // If it's not WSAEWOULDBLOCK, there's no more recourse - we either succeeded or failed.
                    if (errorCode != SocketError.WouldBlock)
                    {
                        return errorCode;
                    }
                }

                // It failed.  Fall through to the regular abortive close.
            }

            // By default or if non abortive path failed, set linger timeout to zero to get an abortive close (RST).
            Interop.Winsock.Linger lingerStruct;
            lingerStruct.OnOff = 1;
            lingerStruct.Time = 0;

            errorCode = Interop.Winsock.setsockopt(
                handle,
                SocketOptionLevel.Socket,
                SocketOptionName.Linger,
                ref lingerStruct,
                4);
#if DEBUG
            _closeSocketLinger = errorCode;
#endif
            if (errorCode == SocketError.SocketError) errorCode = (SocketError)Marshal.GetLastPInvokeError();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle}, setsockopt():{errorCode}");

            if (errorCode != SocketError.Success && errorCode != SocketError.InvalidArgument && errorCode != SocketError.ProtocolOption)
            {
                // Too dangerous to try closesocket() - it might block!
                return errorCode;
            }

            errorCode = Interop.Winsock.closesocket(handle);
#if DEBUG
            _closeSocketResult = errorCode;
#endif
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle}, closesocket#3():{(errorCode == SocketError.SocketError ? (SocketError)Marshal.GetLastPInvokeError() : errorCode)}");

            return errorCode;
        }

        private static void ThrowSocketDisposedException(Exception? innerException = null) =>
            throw new ObjectDisposedException(typeof(Socket).FullName, innerException);
    }
}
