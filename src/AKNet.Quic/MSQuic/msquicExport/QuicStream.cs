/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Diagnostics;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.MSQuic.Common
{
    internal enum QuicStreamType
    {
        Unidirectional, //单向流
        Bidirectional   //双向流
    }

    internal enum QuicAbortDirection
    {
        Read = 1,
        Write = 2,
        Both = Read | Write
    }

    internal partial class QuicStream
    {
        private readonly QUIC_STREAM _handle;
        private ReceiveBuffers _receiveBuffers = new ReceiveBuffers();
        private int _receivedNeedsEnable;
        private MsQuicBuffers _sendBuffers = new MsQuicBuffers();
        private int _sendLocked;
        private Exception _sendException;
        internal const int _defaultErrorCode = 100;
        private readonly bool _canRead;
        private readonly bool _canWrite;
        public ulong _id;
        public ulong Id => _id;
        public QuicStreamType nType;
        private Action<QuicStreamType>? _decrementStreamCapacity;
        private int _disposed = 0;
        private readonly KKValueTaskSource _startedTcs = new KKValueTaskSource();
        private readonly KKValueTaskSource _shutdownTcs = new KKValueTaskSource();

        private readonly KKResettableValueTaskSource _receiveTcs = new KKResettableValueTaskSource()
        {
            CancellationAction = target =>
            {
                try
                {
                    if (target is QuicStream stream)
                    {
                        stream.Abort(QuicAbortDirection.Read, QuicStream._defaultErrorCode);
                        stream._receiveTcs.TrySetResult();
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            }
        };

        private readonly KKResettableValueTaskSource _sendTcs = new KKResettableValueTaskSource()
        {
            CancellationAction = target =>
            {
                try
                {
                    if (target is QuicStream stream)
                    {
                        stream.Abort(QuicAbortDirection.Write, QuicStream._defaultErrorCode);
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            }
        };

        public QuicStream(QuicConnection mConnection, QuicStreamType nType)
        {
            this.nType = nType;
            var Flags = nType == QuicStreamType.Unidirectional ? 
                QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL : QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_NONE;

            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicStreamOpen(mConnection._handle, Flags, NativeCallback, this, out _handle)))
            {
                NetLog.LogError("StreamOpen failed");
            }
            
            _canRead = nType == QuicStreamType.Bidirectional;
            _canWrite = true;
        }

        public QuicStream(QuicConnection mConnection, QUIC_STREAM mStreamHandle, QUIC_STREAM_OPEN_FLAGS flags)
        {
            _id = mStreamHandle.ID;
            this._handle = mStreamHandle;
            MSQuicFunc.MsQuicSetCallbackHandler_For_QUIC_STREAM(mStreamHandle, NativeCallback, this);
            _canRead = true;
            _canWrite = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL);
            this.nType = flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL) ? QuicStreamType.Unidirectional : QuicStreamType.Bidirectional;
        }

        public void CompleteWrites()
        {
            if (_disposed > 0)
            {
                throw new ObjectDisposedException(this.ToString());
            }

            if (_sendTcs.IsCompleted)
            {
                return;
            }

            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicStreamShutdown(_handle, QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_GRACEFUL, default)))
            {
                NetLog.LogError("StreamShutdown failed");
            }
        }

        private int HandleEventStartComplete(ref QUIC_STREAM_EVENT.START_COMPLETE_DATA data)
        {
            Debug.Assert(_decrementStreamCapacity != null);
            _id = data.ID;
            if (MSQuicFunc.QUIC_SUCCEEDED(data.Status))
            {
                _decrementStreamCapacity(nType);
                if (data.PeerAccepted)
                {
                    _startedTcs.TrySetResult();
                }
                else
                {
                    NetLog.LogError("HandleEventStartComplete Error");
                }
            }
            else
            {
                NetLog.LogError("HandleEventStartComplete Error");
                _startedTcs.TrySetException(new Exception());
            }

            _decrementStreamCapacity = null;
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventReceive(ref QUIC_STREAM_EVENT.RECEIVE_DATA data)
        {
            int totalCopied = _receiveBuffers.WriteFrom(
                data.Buffers.AsSpan().Slice(0, data.BufferCount),
                (int)data.TotalBufferLength,
                data.Flags.HasFlag(QUIC_RECEIVE_FLAGS.QUIC_RECEIVE_FLAG_FIN));

            NetLog.Assert(totalCopied >= 0);
            if (totalCopied < data.TotalBufferLength)
            {
                Volatile.Write(ref _receivedNeedsEnable, 1);
            }
            _receiveTcs.TrySetResult();

            data.TotalBufferLength = totalCopied;
            if (_receiveBuffers.HasCapacity() && Interlocked.CompareExchange(ref _receivedNeedsEnable, 0, 1) == 1)
            {
                return MSQuicFunc.QUIC_STATUS_CONTINUE;
            }
            else
            {
                return MSQuicFunc.QUIC_STATUS_SUCCESS;
            }
        }
        
        private int HandleEventSendComplete(ref QUIC_STREAM_EVENT.SEND_COMPLETE_DATA data)
        {
            _sendBuffers.Reset();
            Exception? exception = Volatile.Read(ref _sendException);
            if (exception != null)
            {
                _sendTcs.TrySetException(exception, final: true);
            }

            if (data.Canceled)
            {
                NetLog.LogError("HandleEventSendComplete Canceled");
            }
            else
            {
                _sendTcs.TrySetResult();
            }
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventPeerSendShutdown()
        {
            _receiveBuffers.SetFinal();
            _receiveTcs.TrySetResult();
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventPeerSendAborted(ref QUIC_STREAM_EVENT.PEER_SEND_ABORTED_DATA data)
        {
            _receiveTcs.TrySetException(new Exception(), final: true);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventPeerReceiveAborted(ref QUIC_STREAM_EVENT.PEER_RECEIVE_ABORTED_DATA data)
        {
            _sendTcs.TrySetException(new Exception(), final: true);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private int HandleEventSendShutdownComplete(ref QUIC_STREAM_EVENT.SEND_SHUTDOWN_COMPLETE_DATA data)
        {
            if (data.Graceful)
            {
                _sendTcs.TrySetResult(final: true);
            }
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventShutdownComplete(ref QUIC_STREAM_EVENT.SHUTDOWN_COMPLETE_DATA data)
        {
            if (data.ConnectionShutdown)
            {
                bool shutdownByApp = data.ConnectionShutdownByApp;
                bool closedRemotely = data.ConnectionClosedRemotely;
                Exception exception = new Exception();
                _startedTcs.TrySetException(exception);
                _receiveTcs.TrySetException(exception, final: true);
                _sendTcs.TrySetException(exception, final: true);
            }
            _startedTcs.TrySetException(new Exception());
            _shutdownTcs.TrySetResult();
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventPeerAccepted()
        {
            _startedTcs.TrySetResult();
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleStreamEvent(ref QUIC_STREAM_EVENT streamEvent)
        {
            //NetLog.Log("Stream Event: " + streamEvent.Type.ToString());
            switch (streamEvent.Type)
            {
                case QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_START_COMPLETE:
                    HandleEventStartComplete(ref streamEvent.START_COMPLETE);
                    break;
                case QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_RECEIVE:
                    HandleEventReceive(ref streamEvent.RECEIVE);
                    break;
                case QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_SEND_COMPLETE:
                    HandleEventSendComplete(ref streamEvent.SEND_COMPLETE);
                    break;
                case QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_PEER_SEND_SHUTDOWN:
                    HandleEventPeerSendShutdown();
                    break;
                case QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_PEER_SEND_ABORTED:
                    HandleEventPeerSendAborted(ref streamEvent.PEER_SEND_ABORTED);
                    break;
                case QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_PEER_RECEIVE_ABORTED:
                    HandleEventPeerReceiveAborted(ref streamEvent.PEER_RECEIVE_ABORTED);
                    break;
                case QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_SEND_SHUTDOWN_COMPLETE:
                    HandleEventSendShutdownComplete(ref streamEvent.SEND_SHUTDOWN_COMPLETE);
                    break;
                case QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_SHUTDOWN_COMPLETE:
                    HandleEventShutdownComplete(ref streamEvent.SHUTDOWN_COMPLETE);
                    break;
                case QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_PEER_ACCEPTED:
                    HandleEventPeerAccepted();
                    break;
            };

            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int NativeCallback(QUIC_STREAM stream, object context, ref QUIC_STREAM_EVENT streamEvent)
        {
            QuicStream instance =  context as QuicStream;
            return instance.HandleStreamEvent(ref streamEvent);
        }

        internal ValueTask StartAsync(Action<QuicStreamType> decrementStreamCapacity, CancellationToken cancellationToken = default)
        {
            Debug.Assert(!_startedTcs.IsCompleted);

            _startedTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken);
            _decrementStreamCapacity = decrementStreamCapacity;
            int status = MSQuicFunc.MsQuicStreamStart(_handle, QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL | QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_INDICATE_PEER_ACCEPT);
            if (MSQuicFunc.QUIC_FAILED(status))
            {
                NetLog.LogError("MsQuicStreamStart Error");
                _decrementStreamCapacity = decrementStreamCapacity;
                _startedTcs.TrySetException(new Exception());
            }

            return valueTask;
        }

        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_disposed > 0)
            {
                throw new ObjectDisposedException(this.ToString());
            }

            if (!_canRead)
            {
                throw new InvalidOperationException(this.ToString());
            }

            if (_receiveTcs.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int totalCopied = 0;
            int nLoopCount = 0;
            do
            {
                if (!_receiveTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
                {
                    throw new InvalidOperationException();
                }

                int copied = _receiveBuffers.WriteTo(buffer, out bool complete, out bool empty);
                buffer = buffer.Slice(copied);
                totalCopied += copied;

                if (complete)
                {
                    _receiveTcs.TrySetResult(final: true);
                }

                if (totalCopied > 0 || !empty)
                {
                    _receiveTcs.TrySetResult();
                }
                await valueTask.ConfigureAwait(false);
                if (complete)
                {
                    break;
                }
                nLoopCount++;
            } while (!buffer.IsEmpty && totalCopied == 0);

            //NetLog.Log("nLoopCount: " + nLoopCount);
            //NetLog.Log("Thread.CurrentThread.ManagedThreadId: " + Thread.CurrentThread.ManagedThreadId);

            if (totalCopied > 0 && Interlocked.CompareExchange(ref _receivedNeedsEnable, 0, 1) == 1)
            {
                if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicStreamReceiveSetEnabled(_handle, true)))
                {
                    NetLog.LogError("StreamReceivedSetEnabled failed");
                }
            }

            return totalCopied;
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffer, false, cancellationToken);
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool completeWrites, CancellationToken cancellationToken = default)
        {
            if (_disposed > 0)
            {
                throw new ObjectDisposedException(this.ToString());
            }

            if (!_canWrite)
            {
                throw new InvalidOperationException();
            }

            _sendBuffers.Initialize(buffer);
            int status = MSQuicFunc.MsQuicStreamSend(
                _handle,
                _sendBuffers.Buffers,
                (int)_sendBuffers.Count,
                QUIC_SEND_FLAGS.QUIC_SEND_FLAG_NONE,
                null);

            if (MSQuicFunc.QUIC_FAILED(status))
            {
                NetLog.LogError("MsQuicStreamSend Error: " + status);
                return new ValueTask(Task.FromException(new Exception()));
            }

            if (!_sendTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
            {
                return new ValueTask(Task.FromException(new InvalidOperationException()));
            }
            return valueTask;
        }

        public void Abort(QuicAbortDirection abortDirection, int errorCode)
        {
            if (_disposed > 0)
            {
                return;
            }

            QUIC_STREAM_SHUTDOWN_FLAGS flags = QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_NONE;
            if (abortDirection.HasFlag(QuicAbortDirection.Read) && !_receiveTcs.IsCompleted)
            {
                flags |= QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE;
            }
            if (abortDirection.HasFlag(QuicAbortDirection.Write) && !_sendTcs.IsCompleted)
            {
                flags |= QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND;
            }
            
            if (flags == QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_NONE)
            {
                return;
            }

            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicStreamShutdown(_handle, flags, errorCode)))
            {
                throw new Exception();
            }
            
            if (abortDirection.HasFlag(QuicAbortDirection.Read))
            {
                _receiveTcs.TrySetException(new InvalidOperationException());
            }

            NetLog.LogError("Abort: " + abortDirection);
            if (abortDirection.HasFlag(QuicAbortDirection.Write))
            {
                var exception = new Exception();
                Interlocked.CompareExchange(ref _sendException, exception, null);
                if (Interlocked.CompareExchange(ref _sendLocked, 1, 0) == 0)
                {
                    _sendTcs.TrySetException(_sendException, final: true);
                    Volatile.Write(ref _sendLocked, 0);
                }
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            if (!_startedTcs.IsCompletedSuccessfully)
            {
                StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_ABORT | QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE, _defaultErrorCode);
            }
            else
            {
                if (!_receiveTcs.IsCompleted)
                {
                    StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE, _defaultErrorCode);
                }
                if (!_sendTcs.IsCompleted)
                {
                    StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_GRACEFUL, _defaultErrorCode);
                }
            }
            
            if (_shutdownTcs.TryInitialize(out ValueTask valueTask, this))
            {
                await valueTask.ConfigureAwait(false);
            }
            Debug.Assert(_startedTcs.IsCompleted);

            void StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS flags, int errorCode)
            {
                int status = MSQuicFunc.MsQuicStreamShutdown(_handle, flags, errorCode);
                if (MSQuicFunc.QUIC_FAILED(status))
                {
                   
                }
                else
                {
                    if (flags.HasFlag(QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE) && !_receiveTcs.IsCompleted)
                    {
                        _receiveTcs.TrySetException(new Exception(), final: true);
                    }
                    if (flags.HasFlag(QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND) && !_sendTcs.IsCompleted)
                    {
                        _sendTcs.TrySetException(new Exception(), final: true);
                    }
                }
            }
        }
    }
}
