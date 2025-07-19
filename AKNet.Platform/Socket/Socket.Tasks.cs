//using System;
//using System.Diagnostics;
//using System.Runtime.CompilerServices;
//using System.Runtime.ExceptionServices;
//using System.Runtime.InteropServices;
//using System.Threading.Tasks.Sources;

//namespace AKNet.Platform.Socket
//{
//    public partial class Socket
//    {
//        private AwaitableSocketAsyncEventArgs? _singleBufferReceiveEventArgs;
//        private AwaitableSocketAsyncEventArgs? _singleBufferSendEventArgs;
//        private TaskSocketAsyncEventArgs<int>? _multiBufferReceiveEventArgs;
//        private TaskSocketAsyncEventArgs<int>? _multiBufferSendEventArgs;
        
//        public Task<SocketReceiveFromResult> ReceiveFromAsync(ArraySegment<byte> buffer, EndPoint remoteEndPoint) =>
//            ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint);
        
//        public Task<SocketReceiveFromResult> ReceiveFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
//        {
//            ValidateBuffer(buffer);
//            return ReceiveFromAsync(buffer, socketFlags, remoteEndPoint, default).AsTask();
//        }

//        public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken = default) =>
//            ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint, cancellationToken);

//        public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
//        {
//            ValidateReceiveFromEndpointAndState(remoteEndPoint, nameof(remoteEndPoint));

//            if (cancellationToken.IsCancellationRequested)
//            {
//                return new ValueTask<SocketReceiveFromResult>(Task.FromCanceled<SocketReceiveFromResult>(cancellationToken));
//            }

//            AwaitableSocketAsyncEventArgs saea =
//                Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ??
//                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);

//            Debug.Assert(saea.BufferList == null);
//            saea.SetBuffer(buffer);
//            saea.SocketFlags = socketFlags;
//            saea.RemoteEndPoint = remoteEndPoint;
//            saea._socketAddress = new SocketAddress(AddressFamily);
//            if (remoteEndPoint!.AddressFamily != AddressFamily && AddressFamily == AddressFamily.InterNetworkV6 && IsDualMode)
//            {
//                saea.RemoteEndPoint = s_IPEndPointIPv6;
//            }

//            saea.WrapExceptionsForNetworkStream = false;
//            return saea.ReceiveFromAsync(this, cancellationToken);
//        }
            
//        public ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, SocketAddress receivedAddress, CancellationToken cancellationToken = default)
//        {
//            ThrowIfDisposed();
//            if (receivedAddress == null)
//            {
//                throw new ArgumentNullException();
//            }
            
//            if (receivedAddress.Size < SocketAddress.GetMaximumAddressSize(AddressFamily))
//            {
//                throw new ArgumentOutOfRangeException();
//            }

//            if (cancellationToken.IsCancellationRequested)
//            {
//                return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
//            }

//            AwaitableSocketAsyncEventArgs saea = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ?? 
//                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);

//            Debug.Assert(saea.BufferList == null);
//            saea.SetBuffer(buffer);
//            saea.SocketFlags = socketFlags;
//            saea.RemoteEndPoint = null;
//            saea._socketAddress = receivedAddress;
//            saea.WrapExceptionsForNetworkStream = false;
//            return saea.ReceiveFromSocketAddressAsync(this, cancellationToken);
//        }
        
//        public Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(ArraySegment<byte> buffer, EndPoint remoteEndPoint) =>
//            ReceiveMessageFromAsync(buffer, SocketFlags.None, remoteEndPoint);
        
//        public Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
//        {
//            ValidateBuffer(buffer);
//            return ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint, default).AsTask();
//        }
        
//        public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken = default) =>
//            ReceiveMessageFromAsync(buffer, SocketFlags.None, remoteEndPoint, cancellationToken);
        
//        public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
//        {
//            ValidateReceiveFromEndpointAndState(remoteEndPoint, nameof(remoteEndPoint));
//            if (cancellationToken.IsCancellationRequested)
//            {
//                return new ValueTask<SocketReceiveMessageFromResult>(Task.FromCanceled<SocketReceiveMessageFromResult>(cancellationToken));
//            }

//            AwaitableSocketAsyncEventArgs saea = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ??
//                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);

//            Debug.Assert(saea.BufferList == null);
//            saea.SetBuffer(buffer);
//            saea.SocketFlags = socketFlags;
//            saea.RemoteEndPoint = remoteEndPoint;
//            saea.WrapExceptionsForNetworkStream = false;
//            return saea.ReceiveMessageFromAsync(this, cancellationToken);
//        }
        
//        public Task<int> SendToAsync(ArraySegment<byte> buffer, EndPoint remoteEP) =>
//            SendToAsync(buffer, SocketFlags.None, remoteEP);
        
//        public Task<int> SendToAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP)
//        {
//            ValidateBuffer(buffer);
//            return SendToAsync(buffer, socketFlags, remoteEP, default).AsTask();
//        }
        
//        public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, EndPoint remoteEP, CancellationToken cancellationToken = default) =>
//            SendToAsync(buffer, SocketFlags.None, remoteEP, cancellationToken);
        
//        public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default)
//        {
//            if (remoteEP == null)
//            {
//                throw new ArgumentNullException();
//            }

//            if (cancellationToken.IsCancellationRequested)
//            {
//                return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
//            }

//            AwaitableSocketAsyncEventArgs saea =
//                Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ??
//                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);

//            Debug.Assert(saea.BufferList == null);
//            saea.SetBuffer(MemoryMarshal.AsMemory(buffer));
//            saea.SocketFlags = socketFlags;
//            saea.RemoteEndPoint = remoteEP;
//            saea.WrapExceptionsForNetworkStream = false;
//            return saea.SendToAsync(this, cancellationToken);
//        }
        
//        public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, SocketAddress socketAddress, CancellationToken cancellationToken = default)
//        {
//            ThrowIfDisposed();
//            if (socketAddress == null)
//            {
//                throw new ArgumentNullException();
//            }

//            if (cancellationToken.IsCancellationRequested)
//            {
//                return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
//            }

//            AwaitableSocketAsyncEventArgs saea =
//                Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ??
//                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);

//            Debug.Assert(saea.BufferList == null);
//            saea.SetBuffer(MemoryMarshal.AsMemory(buffer));
//            saea.SocketFlags = socketFlags;
//            saea._socketAddress = socketAddress;
//            saea.RemoteEndPoint = null;
//            saea.WrapExceptionsForNetworkStream = false;

//            try
//            {
//                return saea.SendToAsync(this, cancellationToken);
//            }
//            finally
//            {
//                saea._socketAddress = null;
//            }
//        }
        
//        private static void ValidateBuffer(ArraySegment<byte> buffer)
//        {
//            if (buffer.Array != null)
//            {
//                throw new ArgumentNullException();
//            }

//            if ((uint)buffer.Offset > (uint)buffer.Array.Length)
//            {
//                throw new ArgumentOutOfRangeException(nameof(buffer.Offset));
//            }
//            if ((uint)buffer.Count > (uint)(buffer.Array.Length - buffer.Offset))
//            {
//                throw new ArgumentOutOfRangeException(nameof(buffer.Count));
//            }
//        }
        
//        private static void ValidateBuffersList(IList<ArraySegment<byte>> buffers)
//        {
//            if (buffers == null)
//            {
//                throw new ArgumentNullException();
//            }

//            if (buffers.Count == 0)
//            {
//                throw new ArgumentException();
//            }
//        }
        
//        private Task<int> GetTaskForSendReceive(bool pending, TaskSocketAsyncEventArgs<int> saea, bool fromNetworkStream, bool isReceive)
//        {
//            Task<int> t;
//            if (pending)
//            {
//                bool responsibleForReturningToPool;
//                t = saea.GetCompletionResponsibility(out responsibleForReturningToPool).Task;
//                if (responsibleForReturningToPool)
//                {
//                    ReturnSocketAsyncEventArgs(saea, isReceive);
//                }
//            }
//            else
//            {
//                if (saea.SocketError == SocketError.Success)
//                {
//                    t = Task.FromResult(fromNetworkStream & !isReceive ? 0 : saea.BytesTransferred);
//                }
//                else
//                {
//                    t = Task.FromException<int>(GetException(saea.SocketError, wrapExceptionsInIOExceptions: fromNetworkStream));
//                }
                
//                ReturnSocketAsyncEventArgs(saea, isReceive);
//            }

//            return t;
//        }
        
//        private static void CompleteSendReceive(Socket s, TaskSocketAsyncEventArgs<int> saea, bool isReceive)
//        {
//            SocketError error = saea.SocketError;
//            int bytesTransferred = saea.BytesTransferred;
//            bool wrapExceptionsInIOExceptions = saea._wrapExceptionsInIOExceptions;
            
//            bool responsibleForReturningToPool;
//            AsyncTaskMethodBuilder<int> builder = saea.GetCompletionResponsibility(out responsibleForReturningToPool);
//            if (responsibleForReturningToPool)
//            {
//                s.ReturnSocketAsyncEventArgs(saea, isReceive);
//            }
            
//            if (error == SocketError.Success)
//            {
//                builder.SetResult(bytesTransferred);
//            }
//            else
//            {
//                builder.SetException(GetException(error, wrapExceptionsInIOExceptions));
//            }
//        }
        
//        private static Exception GetException(SocketError error, bool wrapExceptionsInIOExceptions = false)
//        {
//            Exception e = ExceptionDispatchInfo.Capture(new SocketException((int)error));
//            return wrapExceptionsInIOExceptions ? new IOException() : e;
//        }
        
//        private void ReturnSocketAsyncEventArgs(TaskSocketAsyncEventArgs<int> saea, bool isReceive)
//        {
//            saea._accessed = false;
//            saea._builder = default;
//            saea._wrapExceptionsInIOExceptions = false;
            
//            ref TaskSocketAsyncEventArgs<int>? cache = ref isReceive ? ref _multiBufferReceiveEventArgs : ref _multiBufferSendEventArgs;
//            if (Interlocked.CompareExchange(ref cache, saea, null) != null)
//            {
//                saea.Dispose();
//            }
//        }
        
//        private void DisposeCachedTaskSocketAsyncEventArgs()
//        {
//            Interlocked.Exchange(ref _multiBufferReceiveEventArgs, null)?.Dispose();
//            Interlocked.Exchange(ref _multiBufferSendEventArgs, null)?.Dispose();
//            Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null)?.Dispose();
//            Interlocked.Exchange(ref _singleBufferSendEventArgs, null)?.Dispose();
//        }
        
//        private sealed class TaskSocketAsyncEventArgs<TResult> : SocketAsyncEventArgs
//        {
//            internal AsyncTaskMethodBuilder<TResult> _builder;
//            internal bool _accessed;
//            internal bool _wrapExceptionsInIOExceptions;

//            internal TaskSocketAsyncEventArgs() : base(unsafeSuppressExecutionContextFlow: true)
//            {

//            }
            
//            internal AsyncTaskMethodBuilder<TResult> GetCompletionResponsibility(out bool responsibleForReturningToPool)
//            {
//                lock (this)
//                {
//                    responsibleForReturningToPool = _accessed;
//                    _accessed = true;
//                    _ = _builder.Task;
//                    return _builder;
//                }
//            }
//        }

//        internal sealed class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, IValueTaskSource, IValueTaskSource<int>, IValueTaskSource<Socket>, IValueTaskSource<SocketReceiveFromResult>, IValueTaskSource<SocketReceiveMessageFromResult>
//        {
//            private readonly Socket _owner;
//            private readonly bool _isReadForCaching;
//            private ManualResetValueTaskSourceCore<bool> _mrvtsc;
//            private CancellationToken _cancellationToken;
            
//            public AwaitableSocketAsyncEventArgs(Socket owner, bool isReceiveForCaching) :
//                base(unsafeSuppressExecutionContextFlow: true)
//            {
//                _owner = owner;
//                _isReadForCaching = isReceiveForCaching;
//            }

//            public bool WrapExceptionsForNetworkStream { get; set; }
//            private void ReleaseForAsyncCompletion()
//            {
//                _cancellationToken = default;
//                _mrvtsc.Reset();
//                ReleaseForSyncCompletion();
//            }
            
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            private void ReleaseForSyncCompletion()
//            {
//                ref AwaitableSocketAsyncEventArgs? cache = ref _isReadForCaching ? ref _owner._singleBufferReceiveEventArgs : ref _owner._singleBufferSendEventArgs;
//                if (Interlocked.CompareExchange(ref cache, this, null) != null)
//                {
//                    Dispose();
//                }
//            }

//            protected override void OnCompleted(SocketAsyncEventArgs _) => _mrvtsc.SetResult(true);

//            public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Socket socket, CancellationToken cancellationToken)
//            {
//                if (socket.ReceiveFromAsync(this, cancellationToken))
//                {
//                    _cancellationToken = cancellationToken;
//                    return new ValueTask<SocketReceiveFromResult>(this, _mrvtsc.Version);
//                }

//                int bytesTransferred = BytesTransferred;
//                EndPoint remoteEndPoint = RemoteEndPoint!;
//                SocketError error = SocketError;

//                ReleaseForSyncCompletion();

//                return error == SocketError.Success ?
//                    new ValueTask<SocketReceiveFromResult>(new SocketReceiveFromResult() { ReceivedBytes = bytesTransferred, RemoteEndPoint = remoteEndPoint }) :
//                    new ValueTask<SocketReceiveFromResult>(Task.FromException<SocketReceiveFromResult>(CreateException(error)));
//            }

//            internal ValueTask<int> ReceiveFromSocketAddressAsync(Socket socket, CancellationToken cancellationToken)
//            {
//                if (socket.ReceiveFromAsync(this, cancellationToken))
//                {
//                    _cancellationToken = cancellationToken;
//                    return new ValueTask<int>(this, _mrvtsc.Version);
//                }

//                int bytesTransferred = BytesTransferred;
//                SocketError error = SocketError;

//                ReleaseForSyncCompletion();

//                return error == SocketError.Success ?
//                    new ValueTask<int>(bytesTransferred) :
//                    new ValueTask<int>(Task.FromException<int>(CreateException(error)));
//            }

//            public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket socket, CancellationToken cancellationToken)
//            {
//                if (socket.ReceiveMessageFromAsync(this, cancellationToken))
//                {
//                    _cancellationToken = cancellationToken;
//                    return new ValueTask<SocketReceiveMessageFromResult>(this, _mrvtsc.Version);
//                }

//                int bytesTransferred = BytesTransferred;
//                EndPoint remoteEndPoint = RemoteEndPoint!;
//                SocketFlags socketFlags = SocketFlags;
//                IPPacketInformation packetInformation = ReceiveMessageFromPacketInfo;
//                SocketError error = SocketError;

//                ReleaseForSyncCompletion();

//                return error == SocketError.Success ?
//                    new ValueTask<SocketReceiveMessageFromResult>(new SocketReceiveMessageFromResult() { ReceivedBytes = bytesTransferred, RemoteEndPoint = remoteEndPoint, SocketFlags = socketFlags, PacketInformation = packetInformation }) :
//                    new ValueTask<SocketReceiveMessageFromResult>(Task.FromException<SocketReceiveMessageFromResult>(CreateException(error)));
//            }

//            public ValueTask<int> SendToAsync(Socket socket, CancellationToken cancellationToken)
//            {
//                if (socket.SendToAsync(this, cancellationToken))
//                {
//                    _cancellationToken = cancellationToken;
//                    return new ValueTask<int>(this, _mrvtsc.Version);
//                }

//                int bytesTransferred = BytesTransferred;
//                SocketError error = SocketError;

//                ReleaseForSyncCompletion();
//                return error == SocketError.Success ? new ValueTask<int>(bytesTransferred) : new ValueTask(CreateException(error));
//            }
                
//            public ValueTaskSourceStatus GetStatus(short token) => _mrvtsc.GetStatus(token);
//            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
//                _mrvtsc.OnCompleted(continuation, state, token, flags);
            
//            int IValueTaskSource<int>.GetResult(short token)
//            {
//                if (token != _mrvtsc.Version)
//                {
//                    ThrowIncorrectTokenException();
//                }

//                SocketError error = SocketError;
//                int bytes = BytesTransferred;
//                CancellationToken cancellationToken = _cancellationToken;

//                ReleaseForAsyncCompletion();

//                if (error != SocketError.Success)
//                {
//                    ThrowException(error, cancellationToken);
//                }
//                return bytes;
//            }

//            void IValueTaskSource.GetResult(short token)
//            {
//                if (token != _mrvtsc.Version)
//                {
//                    ThrowIncorrectTokenException();
//                }

//                SocketError error = SocketError;
//                CancellationToken cancellationToken = _cancellationToken;

//                ReleaseForAsyncCompletion();

//                if (error != SocketError.Success)
//                {
//                    ThrowException(error, cancellationToken);
//                }
//            }

//            Socket IValueTaskSource<Socket>.GetResult(short token)
//            {
//                if (token != _mrvtsc.Version)
//                {
//                    ThrowIncorrectTokenException();
//                }

//                SocketError error = SocketError;
//                Socket acceptSocket = AcceptSocket!;
//                CancellationToken cancellationToken = _cancellationToken;

//                AcceptSocket = null;

//                ReleaseForAsyncCompletion();

//                if (error != SocketError.Success)
//                {
//                    ThrowException(error, cancellationToken);
//                }
//                return acceptSocket;
//            }

//            SocketReceiveFromResult IValueTaskSource<SocketReceiveFromResult>.GetResult(short token)
//            {
//                if (token != _mrvtsc.Version)
//                {
//                    ThrowIncorrectTokenException();
//                }

//                SocketError error = SocketError;
//                int bytes = BytesTransferred;
//                EndPoint remoteEndPoint = RemoteEndPoint!;
//                CancellationToken cancellationToken = _cancellationToken;

//                ReleaseForAsyncCompletion();

//                if (error != SocketError.Success)
//                {
//                    ThrowException(error, cancellationToken);
//                }

//                return new SocketReceiveFromResult() { ReceivedBytes = bytes, RemoteEndPoint = remoteEndPoint };
//            }

//            SocketReceiveMessageFromResult IValueTaskSource<SocketReceiveMessageFromResult>.GetResult(short token)
//            {
//                if (token != _mrvtsc.Version)
//                {
//                    ThrowIncorrectTokenException();
//                }

//                SocketError error = SocketError;
//                int bytes = BytesTransferred;
//                EndPoint remoteEndPoint = RemoteEndPoint!;
//                SocketFlags socketFlags = SocketFlags;
//                IPPacketInformation packetInformation = ReceiveMessageFromPacketInfo;
//                CancellationToken cancellationToken = _cancellationToken;

//                ReleaseForAsyncCompletion();

//                if (error != SocketError.Success)
//                {
//                    ThrowException(error, cancellationToken);
//                }

//                return new SocketReceiveMessageFromResult() { ReceivedBytes = bytes, RemoteEndPoint = remoteEndPoint, SocketFlags = socketFlags, PacketInformation = packetInformation };
//            }

//            private static void ThrowIncorrectTokenException() => throw new InvalidOperationException(SR.InvalidOperation_IncorrectToken);

//            private void ThrowException(SocketError error, CancellationToken cancellationToken)
//            {
//                if (error is SocketError.OperationAborted or SocketError.ConnectionAborted)
//                {
//                    cancellationToken.ThrowIfCancellationRequested();
//                }

//                throw CreateException(error, forAsyncThrow: false);
//            }

//            private Exception CreateException(SocketError error, bool forAsyncThrow = true)
//            {
//                Exception e = new SocketException((int)error);

//                if (forAsyncThrow)
//                {
//                    e = ExceptionDispatchInfo.SetCurrentStackTrace(e);
//                }

//                return WrapExceptionsForNetworkStream ? new IOException() : e;
//            }
//        }
//    }
//}
