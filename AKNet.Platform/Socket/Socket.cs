using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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

        internal SafeHandle InternalSafeHandle => _handle; // returns _handle without calling SetExposed.
        
        public void Bind(EndPoint localEP)
        {
            ThrowIfDisposed();
            SocketAddress socketAddress = Serialize(localEP);
            DoBind(localEP, socketAddress);
        }

        private void DoBind(EndPoint endPointSnapshot, SocketAddress socketAddress)
        {
            IPEndPoint? ipEndPoint = endPointSnapshot as IPEndPoint;
            SocketError errorCode = SocketPal.Bind( _handle, socketAddress.Buffer.Span.Slice(0, socketAddress.Size));
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }
        }
        
        public void Connect(EndPoint remoteEP)
        {
            ThrowIfDisposed();
            SocketAddress socketAddress = Serialize(remoteEP);
            DoConnect(remoteEP, socketAddress);
        }
            
        public void Shutdown(SocketShutdown how)
        {
            ThrowIfDisposed();
            SocketError errorCode = SocketPal.Shutdown(_handle, _isConnected, _isDisconnected, how);
            if (errorCode != SocketError.Success && errorCode != SocketError.NotSocket)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            SetToDisconnected();
            InternalSetBlocking(_willBlockInternal);
        }
        
        public bool ReceiveFromAsync(SocketAsyncEventArgs e) => ReceiveFromAsync(e, default);
        private bool ReceiveFromAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (e == null)
            {
                throw new ArgumentNullException();
            }

            EndPoint? endPointSnapshot = e.RemoteEndPoint;
            if (e._socketAddress == null)
            {
                if (endPointSnapshot is DnsEndPoint)
                {
                    throw new ArgumentException();
                }

                if (endPointSnapshot == null)
                {
                    throw new ArgumentException();
                }

                e._socketAddress ??= new SocketAddress(_addressFamily);
            }
            
            e.RemoteEndPoint = endPointSnapshot;
            e.StartOperationCommon(this, SocketAsyncOperation.ReceiveFrom);

            SocketError socketError;
            try
            {
                socketError = e.DoOperationReceiveFrom(_handle, cancellationToken);
            }
            catch
            {
                e.Complete();
                throw;
            }

            bool pending = (socketError == SocketError.IOPending);
            return pending;
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

        public SocketAddress Serialize(EndPoint remoteEP)
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

        internal void SetReceivingPacketInformation()
        {
            if (!_receivingPacketInformation)
            {
                SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
                SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
                _receivingPacketInformation = true;
            }
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
        
        private SocketError InternalSetBlocking(bool desired, out bool current)
        {
            if (Disposed)
            {
                current = _willBlock;
                return SocketError.Success;
            }
            
            bool willBlock = false;
            SocketError errorCode;
            try
            {
                errorCode = SocketPal.SetBlocking(_handle, desired, out willBlock);
            }
            catch (ObjectDisposedException)
            {
                errorCode = SocketError.NotSocket;
            }

            if (errorCode == SocketError.Success)
            {
                _willBlockInternal = willBlock;
            }

            current = _willBlockInternal;
            return errorCode;
        }
        
        internal void InternalSetBlocking(bool desired)
        {
            InternalSetBlocking(desired, out _);
        }

        internal void SetToConnected()
        {
            if (_isConnected)
            {
                return;
            }

            Debug.Assert(_nonBlockingConnectInProgress == false);

            _isConnected = true;
            _isDisconnected = false;
            _rightEndPoint ??= _pendingConnectRightEndPoint;
            _pendingConnectRightEndPoint = null;
            UpdateLocalEndPointOnConnect();
        }

        private void UpdateLocalEndPointOnConnect()
        {
            if (IsWildcardEndPoint(_localEndPoint))
            {
                _localEndPoint = null;
            }
        }

        private static bool IsWildcardEndPoint(EndPoint? endPoint)
        {
            if (endPoint == null)
            {
                return false;
            }

            if (endPoint is IPEndPoint ipEndpoint)
            {
                IPAddress address = ipEndpoint.Address;
                return IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address) || s_IPAddressAnyMapToIPv6.Equals(address);
            }

            return false;
        }

        internal void SetToDisconnected()
        {
            if (!_isConnected)
            {
                return;
            }

            _isConnected = false;
            _isDisconnected = true;
        }

        private void UpdateStatusAfterSocketOptionErrorAndThrowException(SocketError error, [CallerMemberName] string? callerName = null)
        {
            bool disconnectOnFailure = error != SocketError.ProtocolOption && error != SocketError.OperationNotSupported;
            UpdateStatusAfterSocketErrorAndThrowException(error, disconnectOnFailure, callerName);
        }

        private void UpdateStatusAfterSocketErrorAndThrowException(SocketError error, bool disconnectOnFailure = true, [CallerMemberName] string? callerName = null)
        {
            var socketException = new SocketException((int)error);
            UpdateStatusAfterSocketError(socketException, disconnectOnFailure);
            throw socketException;
        }
        
        internal void UpdateStatusAfterSocketError(SocketException socketException, bool disconnectOnFailure = true)
        {
            UpdateStatusAfterSocketError(socketException.SocketErrorCode, disconnectOnFailure);
        }

        internal void UpdateStatusAfterSocketError(SocketError errorCode, bool disconnectOnFailure = true)
        {
            if (disconnectOnFailure && _isConnected && (_handle == IntPtr.Zero || (errorCode != SocketError.WouldBlock &&
                    errorCode != SocketError.IOPending && errorCode != SocketError.NoBufferSpaceAvailable &&
                    errorCode != SocketError.TimedOut && errorCode != SocketError.OperationAborted)))
            {
                SetToDisconnected();
            }
        }

        private bool CheckErrorAndUpdateStatus(SocketError errorCode)
        {
            if (errorCode == SocketError.Success || errorCode == SocketError.IOPending)
            {
                return true;
            }

            UpdateStatusAfterSocketError(errorCode);
            return false;
        }
        
        private void ValidateReceiveFromEndpointAndState(EndPoint remoteEndPoint, string remoteEndPointArgumentName)
        {
            if (remoteEndPoint is DnsEndPoint)
            {
                throw new ArgumentException();
            }

            if (_rightEndPoint == null)
            {
                throw new InvalidOperationException();
            }
        }
        
        private void ValidateBlockingMode()
        {
            if (_willBlock && !_willBlockInternal)
            {
                throw new InvalidOperationException();
            }
        }
        
        private void UpdateReceiveSocketErrorForDisposed(ref SocketError socketError, int bytesTransferred)
        {
            if (bytesTransferred == 0 && Disposed)
            {
                socketError = IsConnectionOriented ? SocketError.ConnectionAborted : SocketError.Interrupted;
            }
        }

        private void UpdateSendSocketErrorForDisposed(ref SocketError socketError)
        {
            if (Disposed)
            {
                socketError = IsConnectionOriented ? SocketError.ConnectionAborted : SocketError.Interrupted;
            }
        }

        private void UpdateConnectSocketErrorForDisposed(ref SocketError socketError)
        {
            if (Disposed)
            {
                socketError = SocketError.NotSocket;
            }
        }

        private void UpdateAcceptSocketErrorForDisposed(ref SocketError socketError)
        {
            if (Disposed)
            {
                socketError = SocketError.Interrupted;
            }
        }

        private void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("Disposed");
            }
        }

        private void ThrowIfConnectedStreamSocket()
        {
            if (_isConnected && _socketType == SocketType.Stream)
            {
                throw new SocketException((int)SocketError.IsConnected);
            }
        }

        private bool IsConnectionOriented => _socketType == SocketType.Stream;

        private static SocketError GetSocketErrorFromFaultedTask(Task t)
        {
            Debug.Assert(t.IsCanceled || t.IsFaulted);
            if (t.IsCanceled)
            {
                return SocketError.OperationAborted;
            }

            Debug.Assert(t.Exception != null);
            if (t.Exception.InnerException is SocketException)
            {
                var se = t.Exception.InnerException as SocketException;
                return se.SocketErrorCode;
            }
            else if (t.Exception.InnerException is ObjectDisposedException)
            {
                return SocketError.OperationAborted;
            }
            else if (t.Exception.InnerException is OperationCanceledException)
            {
                return SocketError.OperationAborted;
            }
            else
            {
                return SocketError.SocketError;
            }
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            SetToDisconnected();
            if (_handle != IntPtr.Zero)
            {
                _handle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Close(int timeout = 0)
        {
            _closeTimeout = timeout;
            Dispose();
        }
    }
}
