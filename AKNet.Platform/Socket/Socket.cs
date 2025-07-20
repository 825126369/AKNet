using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    public partial class Socket : IDisposable
    {
        private SafeHandle _handle;
        private CXPLAT_EVENTQ _eventQ;
        
        public Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            SocketError errorCode = SocketPal.CreateSocket(addressFamily, socketType, protocolType, out _handle);
            if (errorCode != SocketError.Success)
            {
                throw new SocketException((int)errorCode);
            }
        }

        public SafeHandle SafeHandle
        {
            get
            {
                return _handle;
            }
        }
        
        public void Bind(IPEndPoint localEP)
        {
            ThrowIfDisposed();
            SocketAddress socketAddress = Serialize(localEP);
            if(SocketPal.Bind(_handle, socketAddress.Buffer.Span.Slice(0, socketAddress.Size)) != 0)
            {
                NetLog.LogError("Bind Error: " + Marshal.GetLastWin32Error());
            }
        }
        
        public void Connect(IPEndPoint remoteEP)
        {
            ThrowIfDisposed();
            SocketAddress socketAddress = Serialize(remoteEP);
            DoConnect(remoteEP, socketAddress);
        }
            
        public void Shutdown(SocketShutdown how)
        {
            ThrowIfDisposed();
            SocketError errorCode = SocketPal.Shutdown(_handle, how);
            if (errorCode != SocketError.Success && errorCode != SocketError.NotSocket)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }
        }

        public bool ReceiveMessageFromAsync(SocketAsyncEventArgs e) => ReceiveMessageFromAsync(e, default);
        private bool ReceiveMessageFromAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            EndPoint endPointSnapshot = e.RemoteEndPoint;
            e._socketAddress = Serialize(endPointSnapshot);
            e.RemoteEndPoint = endPointSnapshot;
            SetReceivingPacketInformation();
            e.StartOperationCommon(this, SocketAsyncOperation.ReceiveMessageFrom);
            SocketError socketError;
            try
            {
                socketError = e.DoOperationReceiveMessageFrom(this, _handle, cancellationToken);
            }
            catch
            {
                e.Complete();
                throw;
            }

            return socketError == SocketError.IOPending;
        }

        public bool SendToAsync(SocketAsyncEventArgs e) => SendToAsync(e, default);

        private bool SendToAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (e == null || e.RemoteEndPoint == null)
            {
                throw new ArgumentNullException();
            }
            
            if (e._socketAddress == null)
            {
                e._socketAddress = Serialize(e.RemoteEndPoint);
            }
                
            e.StartOperationCommon(this, SocketAsyncOperation.SendTo);
            SocketError socketError;
            try
            {
                socketError = e.DoOperationSendTo(_handle, cancellationToken);
            }
            catch
            {
                _localEndPoint = null;
                e.Complete();
                throw;
            }

            if (!CheckErrorAndUpdateStatus(socketError))
            {
                _localEndPoint = null;
            }

            return socketError == SocketError.IOPending;
        }

        internal bool Disposed => _disposed > 0;

        internal static int GetAddressSize(EndPoint endPoint)
        {
            AddressFamily fam = endPoint.AddressFamily;
            return fam == AddressFamily.InterNetwork ? SocketAddressPal.IPv4AddressSize :
                fam == AddressFamily.InterNetworkV6 ? SocketAddressPal.IPv6AddressSize :
                endPoint.Serialize().Size;
        }

        public SocketAddress Serialize(IPEndPoint remoteEP)
        {
            return remoteEP.Serialize();
        }

        private void DoConnect(EndPoint endPointSnapshot, SocketAddress socketAddress)
        {
            SocketError errorCode;
            try
            {
                errorCode = SocketPal.Connect(_handle, socketAddress.Buffer.Slice(0, socketAddress.Size));
            }
            catch (Exception ex)
            {
                throw;
            }

            if (errorCode != SocketError.Success)
            {
                UpdateConnectSocketErrorForDisposed(ref errorCode);
                SocketException socketException = SocketExceptionFactory.CreateSocketException((int)errorCode, endPointSnapshot);
                UpdateStatusAfterSocketError(socketException);
                throw socketException;
            }

            _pendingConnectRightEndPoint = endPointSnapshot;
            _nonBlockingConnectInProgress = false;
            SetToConnected();
        }

        ~Socket()
        {
            Dispose(false);
        }

        internal void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue, bool silent)
        {
            SocketError errorCode;
            try
            {
                errorCode = SocketPal.SetSockOpt(_handle, optionLevel, optionName, optionValue);
            }
            catch
            {
                if (silent)
                {
                    return;
                }
                throw;
            }
        }
        
        private void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("Disposed");
            }
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            if (_handle != null)
            {
                _handle.Dispose();
                _handle = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Close(int timeout = 0)
        {
            Dispose();
        }
    }
}
