using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    public partial class Socket
    {
        private SafeHandle _handle;
        private CXPLAT_EVENTQ _eventQ;
        
        //public Socket(int addressFamily, int socketType, int protocolType)
        //{
        //    SocketError errorCode = SocketPal.CreateSocket(addressFamily, socketType, protocolType, out _handle);
        //    if (errorCode != SocketError.Success)
        //    {
        //        NetLog.LogError("CreateSocket: " + errorCode);
        //    }
        //}

        //public SafeHandle SafeHandle
        //{
        //    get
        //    {
        //        return _handle;
        //    }
        //}
        
        //public void Bind(IPEndPoint localEP)
        //{
        //    ThrowIfDisposed();
        //    SocketAddress socketAddress = Serialize(localEP);
        //    if(SocketPal.Bind(_handle, socketAddress.Buffer.Span.Slice(0, socketAddress.Size)) != 0)
        //    {
        //        NetLog.LogError("Bind Error: " + Marshal.GetLastWin32Error());
        //    }
        //}
        
        //public void Connect(IPEndPoint remoteEP)
        //{
        //    ThrowIfDisposed();
        //    SocketAddress socketAddress = Serialize(remoteEP);
        //    DoConnect(remoteEP, socketAddress);
        //}
        
        //public void Shutdown(SocketShutdown how)
        //{
        //    ThrowIfDisposed();
        //    SocketError errorCode = SocketPal.Shutdown(_handle, how);
        //}

        //public bool ReceiveMessageFromAsync(CXPLAT_EVENTQ e) => ReceiveMessageFromAsync(e, default);
        //private bool ReceiveMessageFromAsync(CXPLAT_EVENTQ e, CancellationToken cancellationToken)
        //{
        //    ThrowIfDisposed();
        //    IPEndPoint endPointSnapshot = e.RemoteEndPoint as IPEndPoint;
        //    e._socketAddress = Serialize(endPointSnapshot);
        //    e.RemoteEndPoint = endPointSnapshot;
        //    e.StartOperationCommon(this, SocketAsyncOperation.ReceiveMessageFrom);
        //    SocketError socketError;
        //    try
        //    {
        //        socketError = e.DoOperationReceiveMessageFrom(this, cancellationToken);
        //    }
        //    catch
        //    {
        //        e.Complete();
        //        throw;
        //    }

        //    return socketError == SocketError.IOPending;
        //}

        //public bool SendMessageToAsync(SocketAsyncEventArgs e) => SendMessageToAsync(e, default);

        //private bool SendMessageToAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        //{
        //    ThrowIfDisposed();
        //    if (e == null || e.RemoteEndPoint == null)
        //    {
        //        throw new ArgumentNullException();
        //    }
            
        //    //if (e._socketAddress == null)
        //    //{
        //    //    e._socketAddress = Serialize(e.RemoteEndPoint);
        //    //}
            
        //    e.StartOperationCommon(this, SocketAsyncOperation.SendTo);
        //    SocketError socketError;
        //    try
        //    {
        //        socketError = e.DoOperationSendTo(_handle, cancellationToken);
        //    }
        //    catch
        //    {
        //        e.Complete();
        //        throw;
        //    }

        //    return socketError == SocketError.IOPending;
        //}

        //public SocketAddress Serialize(IPEndPoint remoteEP)
        //{
        //    return remoteEP.Serialize();
        //}

        //private void DoConnect(EndPoint endPointSnapshot, SocketAddress socketAddress)
        //{
        //    SocketError errorCode;
        //    try
        //    {
        //        errorCode = SocketPal.Connect(SafeHandle, socketAddress.Buffer.Slice(0, socketAddress.Size));
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }

        //    if (errorCode != SocketError.Success)
        //    {
        //        SocketException socketException = SocketExceptionFactory.CreateSocketException((int)errorCode, endPointSnapshot);
        //        throw socketException;
        //    }
        //}

        //~Socket()
        //{
        //    Dispose(false);
        //}

        //internal void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue, bool silent)
        //{
        //    //SocketError errorCode;
        //    //try
        //    //{
        //    //    errorCode = SocketPal.SetSockOpt(_handle, optionLevel, optionName, optionValue);
        //    //}
        //    //catch
        //    //{
        //    //    if (silent)
        //    //    {
        //    //        return;
        //    //    }
        //    //    throw;
        //    //}
        //}
        
        //private void ThrowIfDisposed()
        //{
        //    //if (Disposed)
        //    //{
        //    //    throw new ObjectDisposedException("Disposed");
        //    //}
        //}

        //private void Dispose(bool disposing)
        //{
        //    //if (Interlocked.Exchange(ref _disposed, 1) == 1)
        //    //{
        //    //    return;
        //    //}

        //    //if (_handle != null)
        //    //{
        //    //    _handle.Dispose();
        //    //    _handle = null;
        //    //}
        //}

        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        //public void Close(int timeout = 0)
        //{
        //    Dispose();
        //}
    }
}
