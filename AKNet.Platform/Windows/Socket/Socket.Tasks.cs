using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;

namespace AKNet.Platform.Socket
{
    public partial class Socket
    {
        private AwaitableSocketAsyncEventArgs? _singleBufferReceiveEventArgs;
        private AwaitableSocketAsyncEventArgs? _singleBufferSendEventArgs;
        private TaskSocketAsyncEventArgs<int>? _multiBufferReceiveEventArgs;
        private TaskSocketAsyncEventArgs<int>? _multiBufferSendEventArgs;
        
        public Task<SocketReceiveFromResult> ReceiveFromAsync(ArraySegment<byte> buffer, EndPoint remoteEndPoint) =>
            ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint);
        
        public Task<SocketReceiveFromResult> ReceiveFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
        {
            ValidateBuffer(buffer);
            return ReceiveFromAsync(buffer, socketFlags, remoteEndPoint, default).AsTask();
        }

        public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken = default) =>
            ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint, cancellationToken);

        public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
        {
            ValidateReceiveFromEndpointAndState(remoteEndPoint, nameof(remoteEndPoint));

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<SocketReceiveFromResult>(cancellationToken);
            }

            AwaitableSocketAsyncEventArgs saea =
                Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ??
                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);

            Debug.Assert(saea.BufferList == null);
            saea.SetBuffer(buffer);
            saea.SocketFlags = socketFlags;
            saea.RemoteEndPoint = remoteEndPoint;
            saea._socketAddress = new SocketAddress(AddressFamily);
            if (remoteEndPoint!.AddressFamily != AddressFamily && AddressFamily == AddressFamily.InterNetworkV6 && IsDualMode)
            {
                saea.RemoteEndPoint = s_IPEndPointIPv6;
            }
            saea.WrapExceptionsForNetworkStream = false;
            return saea.ReceiveFromAsync(this, cancellationToken);
        }

        /// <summary>
        /// Receives data and returns the endpoint of the sending host.
        /// </summary>
        /// <param name="buffer">The buffer for the received data.</param>
        /// <param name="socketFlags">A bitwise combination of SocketFlags values that will be used when receiving the data.</param>
        /// <param name="receivedAddress">An <see cref="SocketAddress"/>, that will be updated with value of the remote peer.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
        /// <returns>An asynchronous task that completes with a <see cref="SocketReceiveFromResult"/> containing the number of bytes received and the endpoint of the sending host.</returns>
        public ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, SocketAddress receivedAddress, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(receivedAddress, nameof(receivedAddress));

            if (receivedAddress.Size < SocketAddress.GetMaximumAddressSize(AddressFamily))
            {
                throw new ArgumentOutOfRangeException(nameof(receivedAddress), SR.net_sockets_address_small);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            AwaitableSocketAsyncEventArgs saea =
                Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ??
                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);

            Debug.Assert(saea.BufferList == null);
            saea.SetBuffer(buffer);
            saea.SocketFlags = socketFlags;
            saea.RemoteEndPoint = null;
            saea._socketAddress = receivedAddress;
            saea.WrapExceptionsForNetworkStream = false;
            return saea.ReceiveFromSocketAddressAsync(this, cancellationToken);
        }

        /// <summary>
        /// Receives data and returns additional information about the sender of the message.
        /// </summary>
        /// <param name="buffer">The buffer for the received data.</param>
        /// <param name="remoteEndPoint">An endpoint of the same type as the endpoint of the remote host.</param>
        /// <returns>An asynchronous task that completes with a <see cref="SocketReceiveMessageFromResult"/> containing the number of bytes received and additional information about the sending host.</returns>
        public Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(ArraySegment<byte> buffer, EndPoint remoteEndPoint) =>
            ReceiveMessageFromAsync(buffer, SocketFlags.None, remoteEndPoint);

        /// <summary>
        /// Receives data and returns additional information about the sender of the message.
        /// </summary>
        /// <param name="buffer">The buffer for the received data.</param>
        /// <param name="socketFlags">A bitwise combination of SocketFlags values that will be used when receiving the data.</param>
        /// <param name="remoteEndPoint">An endpoint of the same type as the endpoint of the remote host.</param>
        /// <returns>An asynchronous task that completes with a <see cref="SocketReceiveMessageFromResult"/> containing the number of bytes received and additional information about the sending host.</returns>
        public Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
        {
            ValidateBuffer(buffer);
            return ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint, default).AsTask();
        }

        /// <summary>
        /// Receives data and returns additional information about the sender of the message.
        /// </summary>
        /// <param name="buffer">The buffer for the received data.</param>
        /// <param name="remoteEndPoint">An endpoint of the same type as the endpoint of the remote host.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
        /// <returns>An asynchronous task that completes with a <see cref="SocketReceiveMessageFromResult"/> containing the number of bytes received and additional information about the sending host.</returns>
        public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken = default) =>
            ReceiveMessageFromAsync(buffer, SocketFlags.None, remoteEndPoint, cancellationToken);

        /// <summary>
        /// Receives data and returns additional information about the sender of the message.
        /// </summary>
        /// <param name="buffer">The buffer for the received data.</param>
        /// <param name="socketFlags">A bitwise combination of SocketFlags values that will be used when receiving the data.</param>
        /// <param name="remoteEndPoint">An endpoint of the same type as the endpoint of the remote host.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
        /// <returns>An asynchronous task that completes with a <see cref="SocketReceiveMessageFromResult"/> containing the number of bytes received and additional information about the sending host.</returns>
        public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
        {
            ValidateReceiveFromEndpointAndState(remoteEndPoint, nameof(remoteEndPoint));
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<SocketReceiveMessageFromResult>(cancellationToken);
            }

            AwaitableSocketAsyncEventArgs saea =
                Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ??
                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);

            Debug.Assert(saea.BufferList == null);
            saea.SetBuffer(buffer);
            saea.SocketFlags = socketFlags;
            saea.RemoteEndPoint = remoteEndPoint;
            saea.WrapExceptionsForNetworkStream = false;
            return saea.ReceiveMessageFromAsync(this, cancellationToken);
        }

        /// <summary>
        /// Sends data to the specified remote host.
        /// </summary>
        /// <param name="buffer">The buffer for the data to send.</param>
        /// <param name="remoteEP">The remote host to which to send the data.</param>
        /// <returns>An asynchronous task that completes with the number of bytes sent.</returns>
        public Task<int> SendToAsync(ArraySegment<byte> buffer, EndPoint remoteEP) =>
            SendToAsync(buffer, SocketFlags.None, remoteEP);

        /// <summary>
        /// Sends data to the specified remote host.
        /// </summary>
        /// <param name="buffer">The buffer for the data to send.</param>
        /// <param name="socketFlags">A bitwise combination of SocketFlags values that will be used when sending the data.</param>
        /// <param name="remoteEP">The remote host to which to send the data.</param>
        /// <returns>An asynchronous task that completes with the number of bytes sent.</returns>
        public Task<int> SendToAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP)
        {
            ValidateBuffer(buffer);
            return SendToAsync(buffer, socketFlags, remoteEP, default).AsTask();
        }

        /// <summary>
        /// Sends data to the specified remote host.
        /// </summary>
        /// <param name="buffer">The buffer for the data to send.</param>
        /// <param name="remoteEP">The remote host to which to send the data.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An asynchronous task that completes with the number of bytes sent.</returns>
        public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, EndPoint remoteEP, CancellationToken cancellationToken = default) =>
            SendToAsync(buffer, SocketFlags.None, remoteEP, cancellationToken);

        /// <summary>
        /// Sends data to the specified remote host.
        /// </summary>
        /// <param name="buffer">The buffer for the data to send.</param>
        /// <param name="socketFlags">A bitwise combination of SocketFlags values that will be used when sending the data.</param>
        /// <param name="remoteEP">The remote host to which to send the data.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An asynchronous task that completes with the number of bytes sent.</returns>
        public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(remoteEP);

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            AwaitableSocketAsyncEventArgs saea =
                Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ??
                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);

            Debug.Assert(saea.BufferList == null);
            saea.SetBuffer(MemoryMarshal.AsMemory(buffer));
            saea.SocketFlags = socketFlags;
            saea.RemoteEndPoint = remoteEP;
            saea.WrapExceptionsForNetworkStream = false;
            return saea.SendToAsync(this, cancellationToken);
        }

        /// <summary>
        /// Sends data to the specified remote host.
        /// </summary>
        /// <param name="buffer">The buffer for the data to send.</param>
        /// <param name="socketFlags">A bitwise combination of SocketFlags values that will be used when sending the data.</param>
        /// <param name="socketAddress">The remote host to which to send the data.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An asynchronous task that completes with the number of bytes sent.</returns>
        public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, SocketAddress socketAddress, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(socketAddress);

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            AwaitableSocketAsyncEventArgs saea =
                Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ??
                new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);

            Debug.Assert(saea.BufferList == null);
            saea.SetBuffer(MemoryMarshal.AsMemory(buffer));
            saea.SocketFlags = socketFlags;
            saea._socketAddress = socketAddress;
            saea.RemoteEndPoint = null;
            saea.WrapExceptionsForNetworkStream = false;
            try
            {
                return saea.SendToAsync(this, cancellationToken);
            }
            finally
            {
                // detach user provided SA so we do not accidentally stomp on it later.
                saea._socketAddress = null;
            }
        }

        /// <summary>
        /// Sends the file <paramref name="fileName"/> to a connected <see cref="Socket"/> object.
        /// </summary>
        /// <param name="fileName">A <see cref="string"/> that contains the path and name of the file to be sent. This parameter can be <see langword="null"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="Socket"/> object has been closed.</exception>
        /// <exception cref="NotSupportedException">The <see cref="Socket"/> object is not connected to a remote host.</exception>
        /// <exception cref="FileNotFoundException">The file <paramref name="fileName"/> was not found.</exception>
        /// <exception cref="SocketException">An error occurred when attempting to access the socket.</exception>
        public ValueTask SendFileAsync(string? fileName, CancellationToken cancellationToken = default)
        {
            return SendFileAsync(fileName, default, default, TransmitFileOptions.UseDefaultWorkerThread, cancellationToken);
        }

        /// <summary>Validates the supplied array segment, throwing if its array or indices are null or out-of-bounds, respectively.</summary>
        private static void ValidateBuffer(ArraySegment<byte> buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer.Array, nameof(buffer.Array));
            if ((uint)buffer.Offset > (uint)buffer.Array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Offset));
            }
            if ((uint)buffer.Count > (uint)(buffer.Array.Length - buffer.Offset))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Count));
            }
        }

        /// <summary>Validates the supplied buffer list, throwing if it's null or empty.</summary>
        private static void ValidateBuffersList(IList<ArraySegment<byte>> buffers)
        {
            ArgumentNullException.ThrowIfNull(buffers);

            if (buffers.Count == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_sockets_zerolist, nameof(buffers)), nameof(buffers));
            }
        }
        
        private Task<int> GetTaskForSendReceive(bool pending, TaskSocketAsyncEventArgs<int> saea, bool fromNetworkStream, bool isReceive)
        {
            Task<int> t;
            if (pending)
            {
                bool responsibleForReturningToPool;
                t = saea.GetCompletionResponsibility(out responsibleForReturningToPool).Task;
                if (responsibleForReturningToPool)
                {
                    ReturnSocketAsyncEventArgs(saea, isReceive);
                }
            }
            else
            {
                if (saea.SocketError == SocketError.Success)
                {
                    t = Task.FromResult(fromNetworkStream & !isReceive ? 0 : saea.BytesTransferred);
                }
                else
                {
                    t = Task.FromException<int>(GetException(saea.SocketError, wrapExceptionsInIOExceptions: fromNetworkStream));
                }
                
                ReturnSocketAsyncEventArgs(saea, isReceive);
            }

            return t;
        }
        
        private static void CompleteSendReceive(Socket s, TaskSocketAsyncEventArgs<int> saea, bool isReceive)
        {
            SocketError error = saea.SocketError;
            int bytesTransferred = saea.BytesTransferred;
            bool wrapExceptionsInIOExceptions = saea._wrapExceptionsInIOExceptions;
            
            bool responsibleForReturningToPool;
            AsyncTaskMethodBuilder<int> builder = saea.GetCompletionResponsibility(out responsibleForReturningToPool);
            if (responsibleForReturningToPool)
            {
                s.ReturnSocketAsyncEventArgs(saea, isReceive);
            }
            
            if (error == SocketError.Success)
            {
                builder.SetResult(bytesTransferred);
            }
            else
            {
                builder.SetException(GetException(error, wrapExceptionsInIOExceptions));
            }
        }
        
        private static Exception GetException(SocketError error, bool wrapExceptionsInIOExceptions = false)
        {
            Exception e = ExceptionDispatchInfo.SetCurrentStackTrace(new SocketException((int)error));
            return wrapExceptionsInIOExceptions ? new IOException() : e;
        }
        
        private void ReturnSocketAsyncEventArgs(TaskSocketAsyncEventArgs<int> saea, bool isReceive)
        {
            saea._accessed = false;
            saea._builder = default;
            saea._wrapExceptionsInIOExceptions = false;
            
            ref TaskSocketAsyncEventArgs<int>? cache = ref isReceive ? ref _multiBufferReceiveEventArgs : ref _multiBufferSendEventArgs;
            if (Interlocked.CompareExchange(ref cache, saea, null) != null)
            {
                saea.Dispose();
            }
        }

        /// <summary>Dispose of any cached <see cref="TaskSocketAsyncEventArgs{TResult}"/> instances.</summary>
        private void DisposeCachedTaskSocketAsyncEventArgs()
        {
            Interlocked.Exchange(ref _multiBufferReceiveEventArgs, null)?.Dispose();
            Interlocked.Exchange(ref _multiBufferSendEventArgs, null)?.Dispose();
            Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null)?.Dispose();
            Interlocked.Exchange(ref _singleBufferSendEventArgs, null)?.Dispose();
        }
        
        private sealed class TaskSocketAsyncEventArgs<TResult> : SocketAsyncEventArgs
        {
            internal AsyncTaskMethodBuilder<TResult> _builder;
            internal bool _accessed;
            internal bool _wrapExceptionsInIOExceptions;

            internal TaskSocketAsyncEventArgs() : base(unsafeSuppressExecutionContextFlow: true)
            {

            }
            
            internal AsyncTaskMethodBuilder<TResult> GetCompletionResponsibility(out bool responsibleForReturningToPool)
            {
                lock (this)
                {
                    responsibleForReturningToPool = _accessed;
                    _accessed = true;
                    _ = _builder.Task;
                    return _builder;
                }
            }
        }

        internal sealed class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, IValueTaskSource, IValueTaskSource<int>, IValueTaskSource<Socket>, IValueTaskSource<SocketReceiveFromResult>, IValueTaskSource<SocketReceiveMessageFromResult>
        {
            private readonly Socket _owner;
            private readonly bool _isReadForCaching;
            private ManualResetValueTaskSourceCore<bool> _mrvtsc;
            private CancellationToken _cancellationToken;
            
            public AwaitableSocketAsyncEventArgs(Socket owner, bool isReceiveForCaching) :
                base(unsafeSuppressExecutionContextFlow: true) // avoid flowing context at lower layers as we only expose ValueTask, which handles it
            {
                _owner = owner;
                _isReadForCaching = isReceiveForCaching;
            }

            public bool WrapExceptionsForNetworkStream { get; set; }
            private void ReleaseForAsyncCompletion()
            {
                _cancellationToken = default;
                _mrvtsc.Reset();
                ReleaseForSyncCompletion();
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ReleaseForSyncCompletion()
            {
                ref AwaitableSocketAsyncEventArgs? cache = ref _isReadForCaching ? ref _owner._singleBufferReceiveEventArgs : ref _owner._singleBufferSendEventArgs;
                if (Interlocked.CompareExchange(ref cache, this, null) != null)
                {
                    Dispose();
                }
            }

            protected override void OnCompleted(SocketAsyncEventArgs _) => _mrvtsc.SetResult(true);

            public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Socket socket, CancellationToken cancellationToken)
            {
                if (socket.ReceiveFromAsync(this, cancellationToken))
                {
                    _cancellationToken = cancellationToken;
                    return new ValueTask<SocketReceiveFromResult>(this, _mrvtsc.Version);
                }

                int bytesTransferred = BytesTransferred;
                EndPoint remoteEndPoint = RemoteEndPoint!;
                SocketError error = SocketError;

                ReleaseForSyncCompletion();

                return error == SocketError.Success ?
                    new ValueTask<SocketReceiveFromResult>(new SocketReceiveFromResult() { ReceivedBytes = bytesTransferred, RemoteEndPoint = remoteEndPoint }) :
                    ValueTask.FromException<SocketReceiveFromResult>(CreateException(error));
            }

            internal ValueTask<int> ReceiveFromSocketAddressAsync(Socket socket, CancellationToken cancellationToken)
            {
                if (socket.ReceiveFromAsync(this, cancellationToken))
                {
                    _cancellationToken = cancellationToken;
                    return new ValueTask<int>(this, _mrvtsc.Version);
                }

                int bytesTransferred = BytesTransferred;
                SocketError error = SocketError;

                ReleaseForSyncCompletion();

                return error == SocketError.Success ?
                    new ValueTask<int>(bytesTransferred) :
                    ValueTask.FromException<int>(CreateException(error));
            }

            public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket socket, CancellationToken cancellationToken)
            {
                if (socket.ReceiveMessageFromAsync(this, cancellationToken))
                {
                    _cancellationToken = cancellationToken;
                    return new ValueTask<SocketReceiveMessageFromResult>(this, _mrvtsc.Version);
                }

                int bytesTransferred = BytesTransferred;
                EndPoint remoteEndPoint = RemoteEndPoint!;
                SocketFlags socketFlags = SocketFlags;
                IPPacketInformation packetInformation = ReceiveMessageFromPacketInfo;
                SocketError error = SocketError;

                ReleaseForSyncCompletion();

                return error == SocketError.Success ?
                    new ValueTask<SocketReceiveMessageFromResult>(new SocketReceiveMessageFromResult() { ReceivedBytes = bytesTransferred, RemoteEndPoint = remoteEndPoint, SocketFlags = socketFlags, PacketInformation = packetInformation }) :
                    ValueTask.FromException<SocketReceiveMessageFromResult>(CreateException(error));
            }

            public ValueTask<int> SendToAsync(Socket socket, CancellationToken cancellationToken)
            {
                if (socket.SendToAsync(this, cancellationToken))
                {
                    _cancellationToken = cancellationToken;
                    return new ValueTask<int>(this, _mrvtsc.Version);
                }

                int bytesTransferred = BytesTransferred;
                SocketError error = SocketError;

                ReleaseForSyncCompletion();

                return error == SocketError.Success ?
                    new ValueTask<int>(bytesTransferred) :
                    ValueTask.FromException<int>(CreateException(error));
            }

            public ValueTask DisconnectAsync(Socket socket, CancellationToken cancellationToken)
            {
                if (socket.DisconnectAsync(this, cancellationToken))
                {
                    _cancellationToken = cancellationToken;
                    return new ValueTask(this, _mrvtsc.Version);
                }

                SocketError error = SocketError;
                ReleaseForSyncCompletion();
                return error == SocketError.Success ? ValueTask.CompletedTask : ValueTask.FromException(CreateException(error));
            }
                
            public ValueTaskSourceStatus GetStatus(short token) => _mrvtsc.GetStatus(token);
            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _mrvtsc.OnCompleted(continuation, state, token, flags);
            
            int IValueTaskSource<int>.GetResult(short token)
            {
                if (token != _mrvtsc.Version)
                {
                    ThrowIncorrectTokenException();
                }

                SocketError error = SocketError;
                int bytes = BytesTransferred;
                CancellationToken cancellationToken = _cancellationToken;

                ReleaseForAsyncCompletion();

                if (error != SocketError.Success)
                {
                    ThrowException(error, cancellationToken);
                }
                return bytes;
            }

            void IValueTaskSource.GetResult(short token)
            {
                if (token != _mrvtsc.Version)
                {
                    ThrowIncorrectTokenException();
                }

                SocketError error = SocketError;
                CancellationToken cancellationToken = _cancellationToken;

                ReleaseForAsyncCompletion();

                if (error != SocketError.Success)
                {
                    ThrowException(error, cancellationToken);
                }
            }

            Socket IValueTaskSource<Socket>.GetResult(short token)
            {
                if (token != _mrvtsc.Version)
                {
                    ThrowIncorrectTokenException();
                }

                SocketError error = SocketError;
                Socket acceptSocket = AcceptSocket!;
                CancellationToken cancellationToken = _cancellationToken;

                AcceptSocket = null;

                ReleaseForAsyncCompletion();

                if (error != SocketError.Success)
                {
                    ThrowException(error, cancellationToken);
                }
                return acceptSocket;
            }

            SocketReceiveFromResult IValueTaskSource<SocketReceiveFromResult>.GetResult(short token)
            {
                if (token != _mrvtsc.Version)
                {
                    ThrowIncorrectTokenException();
                }

                SocketError error = SocketError;
                int bytes = BytesTransferred;
                EndPoint remoteEndPoint = RemoteEndPoint!;
                CancellationToken cancellationToken = _cancellationToken;

                ReleaseForAsyncCompletion();

                if (error != SocketError.Success)
                {
                    ThrowException(error, cancellationToken);
                }

                return new SocketReceiveFromResult() { ReceivedBytes = bytes, RemoteEndPoint = remoteEndPoint };
            }

            SocketReceiveMessageFromResult IValueTaskSource<SocketReceiveMessageFromResult>.GetResult(short token)
            {
                if (token != _mrvtsc.Version)
                {
                    ThrowIncorrectTokenException();
                }

                SocketError error = SocketError;
                int bytes = BytesTransferred;
                EndPoint remoteEndPoint = RemoteEndPoint!;
                SocketFlags socketFlags = SocketFlags;
                IPPacketInformation packetInformation = ReceiveMessageFromPacketInfo;
                CancellationToken cancellationToken = _cancellationToken;

                ReleaseForAsyncCompletion();

                if (error != SocketError.Success)
                {
                    ThrowException(error, cancellationToken);
                }

                return new SocketReceiveMessageFromResult() { ReceivedBytes = bytes, RemoteEndPoint = remoteEndPoint, SocketFlags = socketFlags, PacketInformation = packetInformation };
            }

            private static void ThrowIncorrectTokenException() => throw new InvalidOperationException(SR.InvalidOperation_IncorrectToken);

            private void ThrowException(SocketError error, CancellationToken cancellationToken)
            {
                if (error is SocketError.OperationAborted or SocketError.ConnectionAborted)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                throw CreateException(error, forAsyncThrow: false);
            }

            private Exception CreateException(SocketError error, bool forAsyncThrow = true)
            {
                Exception e = new SocketException((int)error);

                if (forAsyncThrow)
                {
                    e = ExceptionDispatchInfo.SetCurrentStackTrace(e);
                }

                return WrapExceptionsForNetworkStream ? new IOException() : e;
            }
        }
    }
}
