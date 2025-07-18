using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace AKNet.Platform.Socket
{
    public partial class Socket : IDisposable
    {
        internal const int DefaultCloseTimeout = -1; // NOTE: changing this default is a breaking change.
        private static readonly IPAddress s_IPAddressAnyMapToIPv6 = IPAddress.Any.MapToIPv6();
        private static readonly IPEndPoint s_IPEndPointIPv6 = new IPEndPoint(s_IPAddressAnyMapToIPv6, 0);

        private SafeSocketHandle _handle;
        internal EndPoint? _rightEndPoint;
        internal EndPoint? _remoteEndPoint;
        private EndPoint? _localEndPoint;
        private bool _isConnected;
        private bool _isDisconnected;
        private bool _willBlock = false; // Desired state of the socket from the user.
        private bool _willBlockInternal = false; // Actual state of the socket.
        private bool _isListening;
        private bool _nonBlockingConnectInProgress;
        private EndPoint? _pendingConnectRightEndPoint;
        private AddressFamily _addressFamily;
        private SocketType _socketType;
        private ProtocolType _protocolType;
        private bool _receivingPacketInformation = false;

        private int _closeTimeout = Socket.DefaultCloseTimeout;
        private int _disposed;
        
        public Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            SocketError errorCode = SocketPal.CreateSocket(addressFamily, socketType, protocolType, out _handle);
            if (errorCode != SocketError.Success)
            {
                Debug.Assert(_handle.IsInvalid);
                throw new SocketException((int)errorCode);
            }

            Debug.Assert(!_handle.IsInvalid);
            _addressFamily = addressFamily;
            _socketType = socketType;
            _protocolType = protocolType;
        }

        private static SafeSocketHandle ValidateHandle(SafeSocketHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException();
            }

            if (handle.IsInvalid)
            {
                throw new ArgumentException();
            }

            return handle;
        }
        
        public int Available
        {
            get
            {
                ThrowIfDisposed();
                int argp;
                SocketError errorCode = SocketPal.GetAvailable(_handle, out argp);
                if (errorCode != SocketError.Success)
                {
                    UpdateStatusAfterSocketErrorAndThrowException(errorCode);
                }

                return argp;
            }
        }
        
        public EndPoint? LocalEndPoint
        {
            get
            {
                ThrowIfDisposed();
                if (_localEndPoint == null)
                {
                    Span<byte> buffer = stackalloc byte[byte.MaxValue];
                    int size = buffer.Length;

                    unsafe
                    {
                        fixed (byte* ptr = buffer)
                        {
                            int nLength = 0;
                            SocketError errorCode = SocketPal.GetSockName(_handle, ptr, out nLength);
                            buffer = buffer.Slice(0, nLength);  
                            if (errorCode != SocketError.Success)
                            {
                                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
                            }
                        }
                    }

                    if (_addressFamily == AddressFamily.InterNetwork || _addressFamily == AddressFamily.InterNetworkV6)
                    {
                        _localEndPoint = IPEndPointExtensions.CreateIPEndPoint(buffer.Slice(0, size));
                    }
                    else
                    {
                        SocketAddress socketAddress = new SocketAddress(_rightEndPoint.AddressFamily, size);
                        buffer.Slice(0, size).CopyTo(socketAddress.Buffer.Span);
                        _localEndPoint = _rightEndPoint.Create(socketAddress);
                    }
                }

                return _localEndPoint;
            }
        }
        
        public EndPoint? RemoteEndPoint
        {
            get
            {
                ThrowIfDisposed();
                if (_remoteEndPoint == null)
                {
                    CheckNonBlockingConnectCompleted();
                    if (_rightEndPoint == null || !_isConnected)
                    {
                        return null;
                    }

                    Span<byte> buffer = stackalloc byte[SocketAddress.GetMaximumAddressSize(_addressFamily)];
                    int size = buffer.Length;
                    SocketError errorCode = SocketPal.GetPeerName( _handle, buffer, ref size);
                    if (errorCode != SocketError.Success)
                    {
                        UpdateStatusAfterSocketErrorAndThrowException(errorCode);
                    }

                    try
                    {
                        if (_addressFamily == AddressFamily.InterNetwork || _addressFamily == AddressFamily.InterNetworkV6)
                        {
                            _remoteEndPoint = IPEndPointExtensions.CreateIPEndPoint(buffer.Slice(0, size));
                        }
                        else
                        {
                            SocketAddress socketAddress = new SocketAddress(_rightEndPoint.AddressFamily, size);
                            buffer.Slice(0, size).CopyTo(socketAddress.Buffer.Span);
                            _remoteEndPoint = _rightEndPoint.Create(socketAddress);
                        }
                    }
                    catch
                    {
                    }
                }

                return _remoteEndPoint;
            }
        }

        public IntPtr Handle => SafeHandle.DangerousGetHandle();

        public SafeSocketHandle SafeHandle
        {
            get
            {
                _handle.SetExposed();
                return _handle;
            }
        }

        internal SafeSocketHandle InternalSafeHandle => _handle; // returns _handle without calling SetExposed.
        public bool Blocking
        {
            get
            {
                return _willBlock;
            }
            set
            {
                ThrowIfDisposed();
                bool current;
                SocketError errorCode = InternalSetBlocking(value, out current);
                if (errorCode != SocketError.Success)
                {
                    UpdateStatusAfterSocketErrorAndThrowException(errorCode);
                }
                _willBlock = current;
            }
        }
            
        public bool Connected
        {
            get
            {
                CheckNonBlockingConnectCompleted();
                return _isConnected;
            }
        }

        // Gets the socket's address family.
        public AddressFamily AddressFamily
        {
            get
            {
                return _addressFamily;
            }
        }

        // Gets the socket's socketType.
        public SocketType SocketType
        {
            get
            {
                return _socketType;
            }
        }

        // Gets the socket's protocol socketType.
        public ProtocolType ProtocolType
        {
            get
            {
                return _protocolType;
            }
        }

        public bool IsBound
        {
            get
            {
                return (_rightEndPoint != null);
            }
        }

        internal bool CanTryAddressFamily(AddressFamily family)
        {
            return (family == _addressFamily) || (family == AddressFamily.InterNetwork && IsDualMode);
        }
        
        public void Bind(EndPoint localEP)
        {
            ThrowIfDisposed();
            SocketAddress socketAddress = Serialize(ref localEP);
            DoBind(localEP, socketAddress);
        }

        private void DoBind(EndPoint endPointSnapshot, SocketAddress socketAddress)
        {
            IPEndPoint? ipEndPoint = endPointSnapshot as IPEndPoint;
            SocketError errorCode = SocketPal.Bind( _handle, _protocolType, socketAddress.Buffer.Span.Slice(0, socketAddress.Size));
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }
            
            _rightEndPoint = endPointSnapshot is UnixDomainSocketEndPoint unixEndPoint ? unixEndPoint.CreateBoundEndPoint() : endPointSnapshot;
        }
        
        public void Connect(EndPoint remoteEP)
        {
            ThrowIfDisposed();
            if (_isDisconnected)
            {
                throw new InvalidOperationException();
            }

            if (_isListening)
            {
                throw new InvalidOperationException();
            }

            DnsEndPoint? dnsEP = remoteEP as DnsEndPoint;
            if (dnsEP != null)
            {
                if (dnsEP.AddressFamily != AddressFamily.Unspecified && !CanTryAddressFamily(dnsEP.AddressFamily))
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }

                Connect(dnsEP.Host, dnsEP.Port);
                return;
            }

            SocketAddress socketAddress = Serialize(ref remoteEP);
            _pendingConnectRightEndPoint = remoteEP;
            _nonBlockingConnectInProgress = !Blocking;

            DoConnect(remoteEP, socketAddress);
        }

        public void Connect(IPAddress address, int port)
        {
            ThrowIfDisposed();
            ThrowIfConnectedStreamSocket();
            if (!CanTryAddressFamily(address.AddressFamily))
            {
                throw new NotSupportedException();
            }
            IPEndPoint remoteEP = new IPEndPoint(address, port);
            Connect(remoteEP);
        }

        public void Connect(string host, int port)
        {
            ThrowIfDisposed();
            if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
            {
                throw new NotSupportedException();
            }

            IPAddress? parsedAddress;
            if (IPAddress.TryParse(host, out parsedAddress))
            {
                Connect(parsedAddress, port);
            }
            else
            {
                IPAddress[] addresses = Dns.GetHostAddresses(host);
                Connect(addresses, port);
            }
        }

        public void Connect(IPAddress[] addresses, int port)
        {
            ThrowIfDisposed();
            if (addresses.Length == 0)
            {
                throw new ArgumentException();
            }
            
            if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
            {
                throw new NotSupportedException();
            }

            ThrowIfConnectedStreamSocket();
            ExceptionDispatchInfo? lastex = null;
            foreach (IPAddress address in addresses)
            {
                if (CanTryAddressFamily(address.AddressFamily))
                {
                    try
                    {
                        Connect(new IPEndPoint(address, port));
                        lastex = null;
                        break;
                    }
                    catch (Exception ex) when (!ExceptionCheck.IsFatal(ex))
                    {
                        lastex = ExceptionDispatchInfo.Capture(ex);
                    }
                }
            }

            lastex?.Throw();
            if (!Connected)
            {
                throw new ArgumentException();
            }
        }

        public void Close(int timeout = 0)
        {
            _closeTimeout = timeout;
            Dispose();
        }

        public int IOControl(int ioControlCode, byte[]? optionInValue, byte[]? optionOutValue)
        {
            ThrowIfDisposed();
            int realOptionLength;
            SocketError errorCode = SocketPal.WindowsIoctl(_handle, ioControlCode, optionInValue, optionOutValue, out realOptionLength);
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            return realOptionLength;
        }

        public int IOControl(IOControlCode ioControlCode, byte[]? optionInValue, byte[]? optionOutValue)
        {
            return IOControl(unchecked((int)ioControlCode), optionInValue, optionOutValue);
        }
        
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            ThrowIfDisposed();
            SetSocketOption(optionLevel, optionName, optionValue, false);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            ThrowIfDisposed();
            SocketError errorCode = SocketPal.SetSockOpt(_handle, optionLevel, optionName, optionValue);
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }
        }
        
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
        {
            SetSocketOption(optionLevel, optionName, (optionValue ? 1 : 0));
        }

        // Sets the specified option to the specified value.
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue)
        {
            ThrowIfDisposed();
#if NET5_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(optionValue);
#else
            if(optionValue == null)
            {
                throw new ArgumentException();
            }
#endif

            if (optionLevel == SocketOptionLevel.Socket && optionName == SocketOptionName.Linger)
            {
                LingerOption? lingerOption = optionValue as LingerOption;
                if (lingerOption == null)
                {
                    throw new ArgumentException();
                }
                if (lingerOption.LingerTime < 0 || lingerOption.LingerTime > (int)ushort.MaxValue)
                {
                    throw new ArgumentException();
                }
                SetLingerOption(lingerOption);
            }
            else if (optionLevel == SocketOptionLevel.IP && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
            {
                MulticastOption? multicastOption = optionValue as MulticastOption;
                if (multicastOption == null)
                {
                    throw new ArgumentException();
                }
                SetMulticastOption(optionName, multicastOption);
            }
            else if (optionLevel == SocketOptionLevel.IPv6 && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
            {
                IPv6MulticastOption? multicastOption = optionValue as IPv6MulticastOption;
                if (multicastOption == null)
                {
                    throw new ArgumentException();
                }
                SetIPv6MulticastOption(optionName, multicastOption);
            }
            else
            {
                throw new ArgumentException();
            }
        }
        
        public void SetRawSocketOption(int optionLevel, int optionName, ReadOnlySpan<byte> optionValue)
        {
            ThrowIfDisposed();
            SocketError errorCode = SocketPal.SetRawSockOpt(_handle, optionLevel, optionName, optionValue);
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }
        }

        public object? GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName)
        {
            ThrowIfDisposed();
            int optionValue;
            SocketError errorCode = SocketPal.GetSockOpt(
                _handle,
                optionLevel,
                optionName,
                out optionValue);
            
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }

            return optionValue;
        }

        public void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            ThrowIfDisposed();
            int optionLength = optionValue != null ? optionValue.Length : 0;
            SocketError errorCode = SocketPal.GetSockOpt(
                _handle,
                optionLevel,
                optionName,
                optionValue!,
                ref optionLength);
            
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }
        }

        public byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength)
        {
            ThrowIfDisposed();
            byte[] optionValue = new byte[optionLength];
            int realOptionLength = optionLength;
            SocketError errorCode = SocketPal.GetSockOpt(
                _handle,
                optionLevel,
                optionName,
                optionValue,
                ref realOptionLength);
            
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }

            if (optionLength != realOptionLength)
            {
                byte[] newOptionValue = new byte[realOptionLength];
                Buffer.BlockCopy(optionValue, 0, newOptionValue, 0, realOptionLength);
                optionValue = newOptionValue;
            }

            return optionValue;
        }
        
        public int GetRawSocketOption(int optionLevel, int optionName, Span<byte> optionValue)
        {
            ThrowIfDisposed();
            int realOptionLength = optionValue.Length;
            SocketError errorCode = SocketPal.GetRawSockOpt(_handle, optionLevel, optionName, optionValue, ref realOptionLength);
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }
            return realOptionLength;
        }
        
        public void SetIPProtectionLevel(IPProtectionLevel level)
        {
            if (level == IPProtectionLevel.Unspecified)
            {
                throw new ArgumentException();
            }

            if (_addressFamily == AddressFamily.InterNetworkV6)
            {
                SocketPal.SetIPProtectionLevel(this, SocketOptionLevel.IPv6, (int)level);
            }
            else if (_addressFamily == AddressFamily.InterNetwork)
            {
                SocketPal.SetIPProtectionLevel(this, SocketOptionLevel.IP, (int)level);
            }
            else
            {
                throw new NotSupportedException(SR.net_invalidversion);
            }
        }

        private static int ToTimeoutMicroseconds(TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                return -1;
            }

            ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);
            long totalMicroseconds = (long)timeout.TotalMicroseconds;
            ArgumentOutOfRangeException.ThrowIfGreaterThan(totalMicroseconds, int.MaxValue, nameof(timeout));

            return (int)totalMicroseconds;
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
                if (!CanTryAddressFamily(endPointSnapshot.AddressFamily))
                {
                    throw new ArgumentException();
                }

                if (endPointSnapshot.AddressFamily == AddressFamily.InterNetwork && IsDualMode)
                {
                    endPointSnapshot = s_IPEndPointIPv6;
                }
                e._socketAddress ??= new SocketAddress(AddressFamily);
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

            if (e == null)
            {
                throw new ArgumentException();
            }

            if (e.RemoteEndPoint == null)
            {
                throw new ArgumentException();
            }
            if (!CanTryAddressFamily(e.RemoteEndPoint.AddressFamily))
            {
                throw new ArgumentException();
            }

            SocketPal.CheckDualModePacketInfoSupport(this);
            EndPoint endPointSnapshot = e.RemoteEndPoint;
            e._socketAddress = Serialize(ref endPointSnapshot);
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

            if (e == null)
            {
                throw new ArgumentNullException();
            }

            EndPoint? endPointSnapshot = e.RemoteEndPoint;
            if (endPointSnapshot == null && e._socketAddress == null)
            {
                throw new ArgumentException();
            }

            if (e._socketAddress != null && endPointSnapshot is IPEndPoint ipep && e._socketAddress.Family == endPointSnapshot?.AddressFamily)
            {
                ipep.Serialize(e._socketAddress.Buffer.Span);
            }
            else if (endPointSnapshot != null)
            {
                e._socketAddress = Serialize(ref endPointSnapshot);
            }
                
            e.StartOperationCommon(this, SocketAsyncOperation.SendTo);

            EndPoint? oldEndPoint = _rightEndPoint;
            _rightEndPoint ??= endPointSnapshot;

            SocketError socketError;
            try
            {
                socketError = e.DoOperationSendTo(_handle, cancellationToken);
            }
            catch
            {
                _rightEndPoint = oldEndPoint;
                _localEndPoint = null;
                e.Complete();
                throw;
            }

            if (!CheckErrorAndUpdateStatus(socketError))
            {
                _rightEndPoint = oldEndPoint;
                _localEndPoint = null;
            }

            return socketError == SocketError.IOPending;
        }

        internal bool Disposed => _disposed > 0;

        internal static void GetIPProtocolInformation(AddressFamily addressFamily, SocketAddress socketAddress, out bool isIPv4, out bool isIPv6)
        {
            bool isIPv4MappedToIPv6 = socketAddress.Family == AddressFamily.InterNetworkV6 && socketAddress.GetIPAddress().IsIPv4MappedToIPv6;
            isIPv4 = addressFamily == AddressFamily.InterNetwork || isIPv4MappedToIPv6; // DualMode
            isIPv6 = addressFamily == AddressFamily.InterNetworkV6;
        }

        internal static int GetAddressSize(EndPoint endPoint)
        {
            AddressFamily fam = endPoint.AddressFamily;
            return fam == AddressFamily.InterNetwork ? SocketAddressPal.IPv4AddressSize :
                fam == AddressFamily.InterNetworkV6 ? SocketAddressPal.IPv6AddressSize :
                endPoint.Serialize().Size;
        }

        private SocketAddress Serialize(ref EndPoint remoteEP)
        {
            if (remoteEP is IPEndPoint ip)
            {
                IPAddress addr = ip.Address;
                if (addr.AddressFamily == AddressFamily.InterNetwork && IsDualMode)
                {
                    addr = addr.MapToIPv6();
                    remoteEP = new IPEndPoint(addr, ip.Port);
                }
            }
            else if (remoteEP is DnsEndPoint)
            {
                throw new ArgumentException();
            }

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
        
        internal void InternalShutdown(SocketShutdown how)
        {

            if (Disposed || _handle.IsInvalid)
            {
                return;
            }

            try
            {
                SocketPal.Shutdown(_handle, _isConnected, _isDisconnected, how);
            }
            catch (ObjectDisposedException) { }
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
                if (silent && _handle.IsInvalid)
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
            if (disconnectOnFailure && _isConnected && (_handle.IsInvalid || (errorCode != SocketError.WouldBlock &&
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

            if (!CanTryAddressFamily(remoteEndPoint.AddressFamily))
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
                throw new InvalidOperationException(SR.net_invasync);
            }
        }
        
        private static SafeFileHandle? OpenFileHandle(string? name) => string.IsNullOrEmpty(name) ? null : File.OpenHandle(name, FileMode.Open, FileAccess.Read);

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

        internal static void SocketListDangerousReleaseRefs(IList? socketList, ref int refsAdded)
        {
            if (socketList == null)
            {
                return;
            }

            for (int i = 0; (i < socketList.Count) && (refsAdded > 0); i++)
            {
                Socket socket = (Socket)socketList[i]!;
                socket.InternalSafeHandle.DangerousRelease();
                refsAdded--;
            }
        }

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

    }
}
