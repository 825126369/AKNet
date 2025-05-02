using AKNet.Common;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp5Quic.Common
{
    internal enum QuicStreamType
    {
        Unidirectional,
        Bidirectional
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

        private ReceiveBuffers _receiveBuffers = new ReceiveBuffers();
        private int _receivedNeedsEnable;

        private MsQuicBuffers _sendBuffers = new MsQuicBuffers();
        private int _sendLocked;
        private Exception? _sendException;

        private readonly long _defaultErrorCode;
        private readonly bool _canRead;
        private readonly bool _canWrite;
        private ulong _id = 0;
        private readonly QuicStreamType _type;
        private Action<QuicStreamType>? _decrementStreamCapacity;
        public ulong Id => _id;
        public QuicStreamType Type => _type;

        public QuicStream(QUIC_CONNECTION connectionHandle, QuicStreamType type, long defaultErrorCode)
        {
            var Flags = type == QuicStreamType.Unidirectional ? QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL : QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_NONE;
            if (QUIC_FAILED(MSQuicFunc.MsQuicStreamOpen(connectionHandle, Flags, NativeCallback, this, ref _handle)))
            {
                NetLog.LogError("StreamOpen failed");
            }
            
            _defaultErrorCode = defaultErrorCode;
            _canRead = type == QuicStreamType.Bidirectional;
            _canWrite = true;
            _type = type;
        }

        public QuicStream(QUIC_CONNECTION connectionHandle, QUIC_STREAM handle, QUIC_STREAM_OPEN_FLAGS flags, long defaultErrorCode)
        {
            GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
            try
            {
                _handle = handle;
                MSQuicFunc.MsQuicSetCallbackHandler_For_QUIC_STREAM(_handle, NativeCallback, _handle);
            }
            catch
            {
                context.Free();
                throw;
            }

            _defaultErrorCode = defaultErrorCode;
            _canRead = true;
            _canWrite = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL);
            _id = handle.ID;
            _type = flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL) ? QuicStreamType.Unidirectional : QuicStreamType.Bidirectional;
        }

        public void Start()
        {
            ulong status = MSQuicFunc.MsQuicStreamStart(_handle, QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL | QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_INDICATE_PEER_ACCEPT);
        }

        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int totalCopied = 0;
            do
            {
                int copied = _receiveBuffers.CopyTo(buffer, out bool complete, out bool empty);
                buffer = buffer.Slice(copied);
                totalCopied += copied;

                await Task.CompletedTask;
                if (complete)
                {
                    break;
                }
            } while (!buffer.IsEmpty && totalCopied == 0);

            if (totalCopied > 0 && Interlocked.CompareExchange(ref _receivedNeedsEnable, 0, 1) == 1)
            {
                if (QUIC_FAILED(MSQuicFunc.MsQuicStreamReceiveSetEnabled(_handle, true)))
                {
                    NetLog.LogError("StreamReceivedSetEnabled failed");
                }
            }
            return totalCopied;
        }

        public async Task WriteAsync(ReadOnlyMemory<byte> buffer)
        {
            Write(buffer, false);
            await Task.CompletedTask;
        }

        public void Write(ReadOnlyMemory<byte> buffer, bool completeWrites)
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
                ulong status = MSQuicFunc.MsQuicStreamSend(_handle, _sendBuffers.Buffers, _sendBuffers.Count, Flag);
                if (QUIC_FAILED(status))
                {
                    _sendBuffers.Reset();
                    Volatile.Write(ref _sendLocked, 0);
                }
            }
        }
        
        public void Abort(QuicAbortDirection abortDirection, long errorCode)
        {
            if (_disposed)
            {
                return;
            }

            QUIC_STREAM_SHUTDOWN_FLAGS flags = QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_NONE;
            if (flags == QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_NONE)
            {
                return;
            }
            
            if (QUIC_FAILED(MSQuicFunc.MsQuicStreamShutdown(_handle, flags, (ulong)errorCode)))
            {
                NetLog.LogError("StreamShutdown failed");
            }

            if (abortDirection.HasFlag(QuicAbortDirection.Read))
            {
                return;
            }
        }

        public void CompleteWrites()
        {
            if (QUIC_FAILED(MSQuicFunc.MsQuicStreamShutdown(_handle, QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_GRACEFUL, default)))
            {
                NetLog.LogError("StreamShutdown failed");
            }
        }

        private ulong HandleEventStartComplete(ref QUIC_STREAM_EVENT.START_COMPLETE_DATA data)
        {
            Debug.Assert(_decrementStreamCapacity != null);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleEventReceive(ref QUIC_STREAM_EVENT.RECEIVE_DATA data)
        {
            //ulong totalCopied = (ulong)_receiveBuffers.CopyFrom(new ReadOnlySpan<QUIC_BUFFER>(data.Buffers, (int)data.BufferCount, data.TotalBufferLength,
            //    data.Flags.HasFlag(QUIC_RECEIVE_FLAGS.QUIC_RECEIVE_FLAG_FIN));

            ulong totalCopied = 0;
            if (totalCopied < (ulong)data.TotalBufferLength)
            {
                Volatile.Write(ref _receivedNeedsEnable, 1);
            }

            data.TotalBufferLength = (int)totalCopied;
            return (_receiveBuffers.HasCapacity() && Interlocked.CompareExchange(ref _receivedNeedsEnable, 0, 1) == 1) ? MSQuicFunc.QUIC_STATUS_CONTINUE : MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventSendComplete(ref QUIC_STREAM_EVENT.SEND_COMPLETE_DATA data)
        {
            _sendBuffers.Reset();
            Volatile.Write(ref _sendLocked, 0);
            Exception? exception = Volatile.Read(ref _sendException);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventPeerSendShutdown()
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleEventPeerSendAborted(ref QUIC_STREAM_EVENT.PEER_SEND_ABORTED_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventPeerReceiveAborted(ref QUIC_STREAM_EVENT.PEER_RECEIVE_ABORTED_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventSendShutdownComplete(ref QUIC_STREAM_EVENT.SEND_SHUTDOWN_COMPLETE_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventShutdownComplete(ref QUIC_STREAM_EVENT.SHUTDOWN_COMPLETE_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventPeerAccepted()
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleStreamEvent(ref QUIC_STREAM_EVENT streamEvent)
        {
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

        private ulong NativeCallback(QUIC_STREAM stream, object context, QUIC_STREAM_EVENT streamEvent)
        {
            GCHandle stateHandle = GCHandle.FromIntPtr((IntPtr)context);
            if (!stateHandle.IsAllocated || !(stateHandle.Target is QuicStream instance))
            {
                return  MSQuicFunc.QUIC_STATUS_INVALID_STATE;
            }

            try
            {
                return instance.HandleStreamEvent(ref streamEvent);
            }
            catch (Exception ex)
            {
                return MSQuicFunc.QUIC_STATUS_INTERNAL_ERROR;
            }
        }

        public void StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS flags, ulong errorCode)
        {
            ulong status = MSQuicFunc.MsQuicStreamShutdown(_handle, flags, errorCode);
            if (QUIC_FAILED(status))
            {
                NetLog.Log($"{this} StreamShutdown({flags}) failed: {status}.");
            }
        }


        public static bool QUIC_SUCCESSED(ulong Status)
        {
            return Status != 0;
        }

        public static bool QUIC_FAILED(ulong Status)
        {
            return Status != 0;
        }
    }
}
