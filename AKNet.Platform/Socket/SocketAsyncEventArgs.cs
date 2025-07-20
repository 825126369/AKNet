using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        private Socket? _acceptSocket;
        private Socket? _connectSocket;
        private Memory<byte> _buffer;
        private int _offset;
        private int _count;
        private bool _bufferIsExplicitArray;
        private IList<ArraySegment<byte>>? _bufferList;
        private List<ArraySegment<byte>>? _bufferListInternal;
        private int _bytesTransferred;
        private bool _disconnectReuseSocket;
        private SocketAsyncOperation _completedOperation;
        private IPPacketInformation _receiveMessageFromPacketInfo;
        private EndPoint? _remoteEndPoint;
        private int _sendPacketsSendSize;
        private SendPacketsElement[]? _sendPacketsElements;
        private TransmitFileOptions _sendPacketsFlags;
        private SocketError _socketError;
        private Exception? _connectByNameError;
        private SocketFlags _socketFlags;
        private object? _userToken;
        private byte[]? _acceptBuffer;
        private int _acceptAddressBufferCount;
        internal SocketAddress? _socketAddress;
        private readonly bool _flowExecutionContext;
        private static readonly ContextCallback s_executionCallback = ExecutionCallback;
        private Socket? _currentSocket;
        private bool _userSocket;
        private bool _disposeCalled;
        
        public const int OperationState_Configuring = -1;
        public const int OperationState_Free = 0;
        public const int OperationState_InProgress = 1;
        public const int OperationState_Disposed = 2;
        private int _operating;

        private ExecutionContext? _context;

        private CancellationTokenSource? _multipleConnectCancellation;

        public SocketAsyncEventArgs() : this(unsafeSuppressExecutionContextFlow: false)
        {
        }
        
        public SocketAsyncEventArgs(bool unsafeSuppressExecutionContextFlow)
        {
            _flowExecutionContext = !unsafeSuppressExecutionContextFlow;
            InitializeInternals();
        }

        public byte[]? Buffer
        {
            get
            {
                if (_bufferIsExplicitArray)
                {
                    bool success = MemoryMarshal.TryGetArray(_buffer, out ArraySegment<byte> arraySegment);
                    Debug.Assert(success);
                    return arraySegment.Array;
                }

                return null;
            }
        }

        public Memory<byte> MemoryBuffer => _buffer;
        public int Offset => _offset;
        public int Count => _count;
        
        public TransmitFileOptions SendPacketsFlags
        {
            get { return _sendPacketsFlags; }
            set { _sendPacketsFlags = value; }
        }
        
        public IList<ArraySegment<byte>>? BufferList
        {
            get { return _bufferList; }
            set
            {
                StartConfiguring();
                try
                {
                    if (value != null)
                    {
                        if (!_buffer.Equals(default))
                        {
                            throw new ArgumentException(SR.net_ambiguousbuffers);
                        }
                        
                        int bufferCount = value.Count;
                        if (_bufferListInternal == null)
                        {
                            _bufferListInternal = new List<ArraySegment<byte>>(bufferCount);
                        }
                        else
                        {
                            _bufferListInternal.Clear();
                        }

                        for (int i = 0; i < bufferCount; i++)
                        {
                            ArraySegment<byte> buffer = value[i];
                            _bufferListInternal.Add(buffer);
                        }
                    }
                    else
                    {
                        _bufferListInternal?.Clear();
                    }

                    _bufferList = value;

                    SetupMultipleBuffers();
                }
                finally
                {
                    Complete();
                }
            }
        }

        public int BytesTransferred
        {
            get { return _bytesTransferred; }
        }

        public event EventHandler<SocketAsyncEventArgs>? Completed;
        private void OnCompletedInternal()
        {
            if (LastOperation <= SocketAsyncOperation.Connect)
            {
                
            }

            OnCompleted(this);
        }

        protected virtual void OnCompleted(SocketAsyncEventArgs e)
        {
            Completed?.Invoke(e._currentSocket, e);
        }
        
        public bool DisconnectReuseSocket
        {
            get { return _disconnectReuseSocket; }
            set { _disconnectReuseSocket = value; }
        }

        public SocketAsyncOperation LastOperation
        {
            get { return _completedOperation; }
        }

        public IPPacketInformation ReceiveMessageFromPacketInfo
        {
            get { return _receiveMessageFromPacketInfo; }
        }

        public EndPoint? RemoteEndPoint
        {
            get { return _remoteEndPoint; }
            set { _remoteEndPoint = value; }
        }

        public SendPacketsElement[]? SendPacketsElements
        {
            get { return _sendPacketsElements; }
            set
            {
                StartConfiguring();
                try
                {
                    _sendPacketsElements = value;
                }
                finally
                {
                    Complete();
                }
            }
        }

        public int SendPacketsSendSize
        {
            get { return _sendPacketsSendSize; }
            set { _sendPacketsSendSize = value; }
        }

        public SocketError SocketError
        {
            get { return _socketError; }
            set { _socketError = value; }
        }

        public Exception? ConnectByNameError
        {
            get { return _connectByNameError; }
        }

        public SocketFlags SocketFlags
        {
            get { return _socketFlags; }
            set { _socketFlags = value; }
        }

        public object? UserToken
        {
            get { return _userToken; }
            set { _userToken = value; }
        }

        public void SetBuffer(int offset, int count)
        {
            StartConfiguring();
            try
            {
                if (!_buffer.Equals(default))
                {
                    if (!_bufferIsExplicitArray)
                    {
                        throw new InvalidOperationException();
                    }

                    _offset = offset;
                    _count = count;
                }
            }
            finally
            {
                Complete();
            }
        }

        internal void CopyBufferFrom(SocketAsyncEventArgs source)
        {
            StartConfiguring();
            try
            {
                _buffer = source._buffer;
                _offset = source._offset;
                _count = source._count;
                _bufferIsExplicitArray = source._bufferIsExplicitArray;
            }
            finally
            {
                Complete();
            }
        }

        public void SetBuffer(byte[]? buffer, int offset, int count)
        {
            StartConfiguring();
            try
            {
                if (buffer == null)
                {
                    _buffer = default;
                    _offset = 0;
                    _count = 0;
                    _bufferIsExplicitArray = false;
                }
                else
                {
                    if (_bufferList != null)
                    {
                        throw new ArgumentException();
                    }

                    _buffer = buffer;
                    _offset = offset;
                    _count = count;
                    _bufferIsExplicitArray = true;
                }
            }
            finally
            {
                Complete();
            }
        }

        public void SetBuffer(Memory<byte> buffer)
        {
            StartConfiguring();
            try
            {
                if (buffer.Length != 0 && _bufferList != null)
                {
                    throw new ArgumentException();
                }

                _buffer = buffer;
                _offset = 0;
                _count = buffer.Length;
                _bufferIsExplicitArray = false;
            }
            finally
            {
                Complete();
            }
        }

        internal bool HasMultipleBuffers => _bufferList != null;

        internal void SetResults(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            _socketError = socketError;
            _connectByNameError = null;
            _bytesTransferred = bytesTransferred;
            _socketFlags = flags;
        }

        internal void SetResults(Exception exception, int bytesTransferred, SocketFlags flags)
        {
            _connectByNameError = exception;
            _bytesTransferred = bytesTransferred;
            _socketFlags = flags;

            if (exception == null)
            {
                _socketError = SocketError.Success;
            }
            else
            {
                SocketException? socketException = exception as SocketException;
                if (socketException != null)
                {
                    _socketError = socketException.SocketErrorCode;
                }
                else if (exception is OperationCanceledException)
                {
                    _socketError = SocketError.OperationAborted;
                }
                else
                {
                    _socketError = SocketError.SocketError;
                }
            }
        }

        private static void ExecutionCallback(object? state)
        {
            var thisRef = (SocketAsyncEventArgs)state!;
            thisRef.OnCompletedInternal();
        }
        
        internal void Complete()
        {
            CompleteCore();
            _context = null;
            _operating = OperationState_Free;
            if (_disposeCalled)
            {
                Dispose();
            }
        }
        
        public void Dispose()
        {
            _disposeCalled = true;
            if (Interlocked.CompareExchange(ref _operating, OperationState_Disposed, OperationState_Free) != OperationState_Free)
            {
                return;
            }

            FreeInternals();
            GC.SuppressFinalize(this);
        }

        ~SocketAsyncEventArgs()
        {
            if (!Environment.HasShutdownStarted)
            {
                FreeInternals();
            }
        }
        
        private void StartConfiguring()
        {
            int status = Interlocked.CompareExchange(ref _operating, OperationState_Configuring, OperationState_Free);
            if (status != OperationState_Free)
            {
                ThrowForNonFreeStatus(status);
            }
        }

        private void ThrowForNonFreeStatus(int status)
        {
            Debug.Assert(status == OperationState_InProgress || status == OperationState_Configuring || status == OperationState_Disposed, $"Unexpected status: {status}");
            throw new InvalidOperationException();
        }
        
        internal void StartOperationCommon(Socket? socket, SocketAsyncOperation operation)
        {
            int status = Interlocked.CompareExchange(ref _operating, OperationState_InProgress, OperationState_Free);
            if (status != OperationState_Free)
            {
                ThrowForNonFreeStatus(status);
            }
            
            _completedOperation = operation;
            _currentSocket = socket;

            if (_flowExecutionContext)
            {
                _context = ExecutionContext.Capture();
            }
            StartOperationCommonCore();
        }

        partial void StartOperationCommonCore();
        internal void FinishOperationSyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            SetResults(socketError, bytesTransferred, flags);
            Socket? currentSocket = _currentSocket;
            if (currentSocket != null)
            {
                currentSocket.UpdateStatusAfterSocketError(socketError);
                if (_completedOperation == SocketAsyncOperation.Connect && !_userSocket)
                {
                    currentSocket.Dispose();
                    _currentSocket = null;
                }
            }

            Complete();
        }

        internal void FinishOperationAsyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            ExecutionContext? context = _context; // store context before it's cleared as part of finishing the operation
            FinishOperationSyncFailure(socketError, bytesTransferred, flags);
            if (context == null)
            {
                OnCompletedInternal();
            }
            else
            {
                ExecutionContext.Run(context, s_executionCallback, this);
            }
        }

        internal void FinishOperationSyncSuccess(int bytesTransferred, SocketFlags flags)
        {
            SetResults(SocketError.Success, bytesTransferred, flags);
            SocketError socketError;
            switch (_completedOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    UpdateReceivedSocketAddress(_socketAddress!);
                    if (_remoteEndPoint == null)
                    {
                        _socketAddress = null;
                    }
                    else if (!SocketAddress.Equals(_socketAddress!, _remoteEndPoint))
                    {
                        try
                        {
                            if (_remoteEndPoint!.AddressFamily == AddressFamily.InterNetworkV6 && _socketAddress!.Family == AddressFamily.InterNetwork)
                            {
                                _remoteEndPoint = new IPEndPoint(_socketAddress.GetIPAddress().MapToIPv6(), _socketAddress.GetPort());
                            }
                            else
                            {
                                _remoteEndPoint = _remoteEndPoint!.Create(_socketAddress!);
                            }
                        }
                        catch
                        {
                        }
                    }
                    break;

                case SocketAsyncOperation.ReceiveMessageFrom:
                    UpdateReceivedSocketAddress(_socketAddress!);
                    if (!SocketAddress.Equals(_socketAddress!, _remoteEndPoint))
                    {
                        try
                        {
                            if (_remoteEndPoint!.AddressFamily == AddressFamily.InterNetworkV6 && _socketAddress!.Family == AddressFamily.InterNetwork)
                            {
                                _remoteEndPoint = new IPEndPoint(_socketAddress.GetIPAddress().MapToIPv6(), _socketAddress.GetPort());
                            }
                            else
                            {
                                _remoteEndPoint = _remoteEndPoint!.Create(_socketAddress!);
                            }
                        }
                        catch
                        {
                        }
                    }

                    FinishOperationReceiveMessageFrom();
                    break;
            }

            Complete();
        }

        internal void FinishOperationAsyncSuccess(int bytesTransferred, SocketFlags flags)
        {
            ExecutionContext? context = _context; // store context before it's cleared as part of finishing the operation

            FinishOperationSyncSuccess(bytesTransferred, flags);
            if (context == null)
            {
                OnCompletedInternal();
            }
            else
            {
                ExecutionContext.Run(context, s_executionCallback, this);
            }
        }

        private void FinishOperationSync(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            Debug.Assert(socketError != SocketError.IOPending);

            if (socketError == SocketError.Success)
            {
                FinishOperationSyncSuccess(bytesTransferred, flags);
            }
            else
            {
                FinishOperationSyncFailure(socketError, bytesTransferred, flags);
            }
        }

    }
}
