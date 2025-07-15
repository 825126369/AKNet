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
                        ThreadPool.BindHandle(this);
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
                    Interop.Kernel32.CancelIoEx(handle, null);
                }

                _iocpBoundHandle.Dispose();
            }
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
            if (!abortive)
            {
                errorCode = Interop.Winsock.closesocket(handle);
                if (errorCode == SocketError.SocketError) errorCode = (SocketError)Marshal.GetLastWin32Error();
                if (errorCode != SocketError.WouldBlock)
                {
                    return errorCode;
                }

                int nonBlockCmd = 0;
                errorCode = Interop.Winsock.ioctlsocket(handle, Interop.Winsock.IoctlSocketConstants.FIONBIO, ref nonBlockCmd);

                if (errorCode == SocketError.SocketError)
                {
                    errorCode = (SocketError)Marshal.GetLastWin32Error();
                }
                
                if (errorCode == SocketError.Success)
                {
                    errorCode = Interop.Winsock.closesocket(handle);
                    if (errorCode == SocketError.SocketError)
                    {
                        errorCode = (SocketError)Marshal.GetLastWin32Error();
                    }
                        
                    if (errorCode != SocketError.WouldBlock)
                    {
                        return errorCode;
                    }
                }
            }
                
            Interop.Winsock.Linger lingerStruct;
            lingerStruct.OnOff = 1;
            lingerStruct.Time = 0;

            errorCode = Interop.Winsock.setsockopt(
                handle,
                SocketOptionLevel.Socket,
                SocketOptionName.Linger,
                ref lingerStruct,
                4);
            
            if (errorCode == SocketError.SocketError) errorCode = (SocketError)Marshal.GetLastWin32Error();

            if (errorCode != SocketError.Success && errorCode != SocketError.InvalidArgument && errorCode != SocketError.ProtocolOption)
            {
                return errorCode;
            }

            errorCode = Interop.Winsock.closesocket(handle);
            return errorCode;
        }

        private static void ThrowSocketDisposedException(Exception? innerException = null) =>
            throw new ObjectDisposedException(typeof(AKNetSocket).FullName, innerException);

        protected internal void SetHandle(IntPtr handle) => this.handle = handle;
    }
}
