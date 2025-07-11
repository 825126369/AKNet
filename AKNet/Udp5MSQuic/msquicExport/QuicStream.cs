using AKNet.Common;
using System;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal enum QuicStreamType
    {
        Unidirectional, //单向流
        Bidirectional //双向流
    }

    internal enum QuicAbortDirection
    {
        Read = 1,
        Write = 2,
        Both = Read | Write
    }

    internal class QuicStream
    {
        private readonly QUIC_STREAM _handle;
        private bool _disposed;
        private readonly AkCircularBuffer _receiveBuffers = new AkCircularBuffer();
        private int _receivedNeedsEnable;
        private MsQuicBuffers _sendBuffers = new MsQuicBuffers();
        private int _sendLocked;
        private Exception? _sendException;

        private const ulong _defaultErrorCode = 100;

        private readonly bool _canRead;
        private readonly bool _canWrite;
        public ulong _id;
        public QuicStreamType nType;
        private readonly QuicConnection mConnection;
        public QuicStream(QuicConnection mConnection, QuicStreamType nType)
        {
            this.nType = nType;
            this.mConnection = mConnection;

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
            this.mConnection = mConnection;
            this._handle = mStreamHandle;
            MSQuicFunc.MsQuicSetCallbackHandler_For_QUIC_STREAM(mStreamHandle, NativeCallback, this);
            _canRead = true;
            _canWrite = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL);
            this.nType = flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL) ? QuicStreamType.Unidirectional : QuicStreamType.Bidirectional;
        }

        public void Start()
        {
            int status = MSQuicFunc.MsQuicStreamStart(_handle,  QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL | QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_INDICATE_PEER_ACCEPT);
            if (MSQuicFunc.QUIC_FAILED(status))
            {
                NetLog.LogError("Start Error");
            }
        }

        public int Read(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int totalCopied = 0;
            lock (_receiveBuffers)
            {
                int copied = _receiveBuffers.WriteToMax(0, buffer.Span);
                totalCopied += copied;
            }
            
            if (totalCopied > 0)
            {
                if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicStreamReceiveSetEnabled(_handle, true)))
                {
                    NetLog.LogError("StreamReceivedSetEnabled failed");
                }
            }
            return totalCopied;
        }

        public void Send(ReadOnlyMemory<byte> buffer)
        {
            _sendBuffers.Initialize(buffer);
            QUIC_SEND_FLAGS Flag = QUIC_SEND_FLAGS.QUIC_SEND_FLAG_NONE;
            int status = MSQuicFunc.MsQuicStreamSend(_handle, _sendBuffers.Buffers, _sendBuffers.Count, Flag, this);
            if (MSQuicFunc.QUIC_FAILED(status))
            {
                NetLog.LogError("MsQuicStreamSend Error");
                _sendBuffers.Reset();
            }
        }

        private void WriteAsync(ReadOnlyMemory<byte> buffer, bool completeWrites)
        {
            Write(buffer, completeWrites);
        }

        private void Write(ReadOnlyMemory<byte> buffer, bool completeWrites)
        {
            if (_disposed)
            {
                return;
            }

            if (!_canWrite)
            {
                return;
            }

            if (buffer.IsEmpty)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _sendLocked, 1, 0) == 0)
            {
                _sendBuffers.Initialize(buffer);
                QUIC_SEND_FLAGS Flag = completeWrites ? QUIC_SEND_FLAGS.QUIC_SEND_FLAG_FIN : QUIC_SEND_FLAGS.QUIC_SEND_FLAG_NONE;
                int status = MSQuicFunc.MsQuicStreamSend(_handle, _sendBuffers.Buffers, _sendBuffers.Count, Flag, this);
                if (MSQuicFunc.QUIC_FAILED(status))
                {
                    NetLog.LogError("MsQuicStreamSend Error");
                    _sendBuffers.Reset();
                    Volatile.Write(ref _sendLocked, 0);
                }
            }
        }

        public void CompleteWrites()
        {
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicStreamShutdown(_handle, QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_GRACEFUL, default)))
            {
                NetLog.LogError("StreamShutdown failed");
            }
        }

        private int HandleEventStartComplete(ref QUIC_STREAM_EVENT.START_COMPLETE_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        public bool orHaveReceiveData()
        {
            return _receiveBuffers.Length > 0;
        }

        private int HandleEventReceive(ref QUIC_STREAM_EVENT.RECEIVE_DATA data)
        {
            const int MaxBufferedBytes = 64 * 1024;
            int totalCopied = 0;
            lock (_receiveBuffers)
            {
                for (int i = 0; i < data.BufferCount; i++)
                {
                    ReadOnlySpan<byte> mSpan = data.Buffers[i].GetSpan();
                    int nCopyLength = Math.Min(mSpan.Length, MaxBufferedBytes - _receiveBuffers.Length);
                    if (nCopyLength <= 0)
                    {
                        break;
                    }
                    else
                    {
                        _receiveBuffers.WriteFrom(mSpan.Slice(0, nCopyLength));
                        totalCopied += nCopyLength;
                    }
                }
            }
            
            data.TotalBufferLength = (int)totalCopied;
            mConnection.mOption.ReceiveStreamDataFunc?.Invoke(this);

            if (_receiveBuffers.Length < MaxBufferedBytes && Interlocked.CompareExchange(ref _receivedNeedsEnable, 0, 1) == 1)
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
            //Volatile.Write(ref _sendLocked, 0);
            //Exception? exception = Volatile.Read(ref _sendException);
            mConnection.mOption.SendFinishFunc?.Invoke(this);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private int HandleEventPeerSendShutdown()
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventPeerSendAborted(ref QUIC_STREAM_EVENT.PEER_SEND_ABORTED_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private int HandleEventPeerReceiveAborted(ref QUIC_STREAM_EVENT.PEER_RECEIVE_ABORTED_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private int HandleEventSendShutdownComplete(ref QUIC_STREAM_EVENT.SEND_SHUTDOWN_COMPLETE_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private int HandleEventShutdownComplete(ref QUIC_STREAM_EVENT.SHUTDOWN_COMPLETE_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private int HandleEventPeerAccepted()
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleStreamEvent(ref QUIC_STREAM_EVENT streamEvent)
        {
            NetLog.Log("Stream Event: " + streamEvent.Type.ToString());
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

        public void Close()
        {
            int status = MSQuicFunc.MsQuicStreamShutdown(_handle, QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_NONE, 0);
        }
    }
}
