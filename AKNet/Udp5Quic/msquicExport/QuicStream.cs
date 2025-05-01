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

    internal sealed partial class QuicStream
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
        private long _id = -1;
        private readonly QuicStreamType _type;
        private Action<QuicStreamType>? _decrementStreamCapacity;
        public long Id => _id;
        public QuicStreamType Type => _type;
        public Task ReadsClosed => _receiveTcs.GetFinalTask(this);
        public Task WritesClosed => _sendTcs.GetFinalTask(this);
        
        public QuicStream(QUIC_CONNECTION connectionHandle, QuicStreamType type, long defaultErrorCode)
        {
            try
            {
                if (QUIC_FAILED(MSQuicFunc.MsQuicStreamOpen(connectionHandle,
                    type == QuicStreamType.Unidirectional ? QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL : QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_NONE,
                    NativeCallback,
                    null,
                    ref _handle)))
                {
                    NetLog.LogError("StreamOpen failed");
                }
            }

            _defaultErrorCode = defaultErrorCode;

            _canRead = type == QuicStreamType.Bidirectional;
            _canWrite = true;
            if (!_canRead)
            {
                _receiveTcs.TrySetResult(final: true);
            }
            _type = type;
        }

        public QuicStream(QUIC_CONNECTION connectionHandle, QUIC_STREAM handle, QUIC_STREAM_OPEN_FLAGS flags, long defaultErrorCode)
        {
            GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
            try
            {
                _handle = handle;
                delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int> nativeCallback = &NativeCallback;
                MsQuicApi.Api.SetCallbackHandler(
                    _handle,
                    nativeCallback,
                    (void*)GCHandle.ToIntPtr(context));
            }
            catch
            {
                context.Free();
                throw;
            }

            _defaultErrorCode = defaultErrorCode;

            _canRead = true;
            _canWrite = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL);
            if (!_canWrite)
            {
                _sendTcs.TrySetResult(final: true);
            }

            _id = handle.ID
            _type = flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL) ? QuicStreamType.Unidirectional : QuicStreamType.Bidirectional;
            _startedTcs.TrySetResult();
        }

        internal ValueTask StartAsync(Action<QuicStreamType> decrementStreamCapacity, CancellationToken cancellationToken = default)
        {
            Debug.Assert(!_startedTcs.IsCompleted);
            _startedTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken);
            _decrementStreamCapacity = decrementStreamCapacity;

            ulong status = MSQuicFunc.MsQuicStreamStart(_handle, QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL | QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_INDICATE_PEER_ACCEPT);
            if (QUIC_FAILED(status))
            {
                _decrementStreamCapacity = null;
            }

            return valueTask;
        }
        
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_receiveTcs.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            
            int totalCopied = 0;
            do
            {
                if (!_receiveTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
                {
                    NetLog.LogError("read");
                }
            
                int copied = _receiveBuffers.CopyTo(buffer, out bool complete, out bool empty);
                buffer = buffer.Slice(copied);
                totalCopied += copied;

                // Make sure the task transitions into final state before the method finishes.
                if (complete)
                {
                    _receiveTcs.TrySetResult(final: true);
                }

                // Unblock the next await to end immediately, i.e. there were/are any data in the buffer.
                if (totalCopied > 0 || !empty)
                {
                    _receiveTcs.TrySetResult();
                }

                // This will either wait for RECEIVE event (no data in buffer) or complete immediately and reset the task.
                await valueTask.ConfigureAwait(false);

                // This is the last read, finish even despite not copying anything.
                if (complete)
                {
                    break;
                }
            } while (!buffer.IsEmpty && totalCopied == 0);  // Exit the loop if target buffer is full we at least copied something.

            if (totalCopied > 0 && Interlocked.CompareExchange(ref _receivedNeedsEnable, 0, 1) == 1)
            {
                
                    ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.StreamReceiveSetEnabled(
                        _handle,
                        1),
                    "StreamReceivedSetEnabled failed");
                
            }
            return totalCopied;
        }

        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => WriteAsync(buffer, completeWrites: false, cancellationToken);


        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool completeWrites, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(QuicStream))));
            }

            if (!_canWrite)
            {
                return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new InvalidOperationException(SR.net_quic_writing_notallowed)));
            }

            if (_sendTcs.IsCompleted && cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            if (!_sendTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
            {
                return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "write"))));
            }

            if (valueTask.IsCompleted)
            {
                return valueTask;
            }

            if (buffer.IsEmpty)
            {
                _sendTcs.TrySetResult();
                if (completeWrites)
                {
                    CompleteWrites();
                }
                return valueTask;
            }

            if (Interlocked.CompareExchange(ref _sendLocked, 1, 0) == 0)
            {
                _sendBuffers.Initialize(buffer);
                ulong status = MSQuicFunc.MsQuicStreamSend(_handle, _sendBuffers.Buffers, _sendBuffers.Count,
                    completeWrites ? QUIC_SEND_FLAGS.QUIC_SEND_FLAG_FIN : QUIC_SEND_FLAGS.QUIC_SEND_FLAG_NONE);

                if (QUIC_FAILED(status))
                {
                    _sendBuffers.Reset();
                    Volatile.Write(ref _sendLocked, 0);
                }
            }

            return valueTask;
        }
        
        public void Abort(QuicAbortDirection abortDirection, long errorCode)
        {
            if (_disposed)
            {
                return;
            }
            ThrowHelper.ValidateErrorCode(nameof(errorCode), errorCode, $"{nameof(Abort)}.{nameof(errorCode)}");

            QUIC_STREAM_SHUTDOWN_FLAGS flags = QUIC_STREAM_SHUTDOWN_FLAGS.NONE;
            if (abortDirection.HasFlag(QuicAbortDirection.Read) && !_receiveTcs.IsCompleted)
            {
                flags |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE;
            }
            if (abortDirection.HasFlag(QuicAbortDirection.Write) && !_sendTcs.IsCompleted)
            {
                flags |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_SEND;
            }
            // Nothing to abort, the requested sides to abort are already closed.
            if (flags == QUIC_STREAM_SHUTDOWN_FLAGS.NONE)
            {
                return;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"{this} Aborting {abortDirection} with {errorCode}");
            }
            unsafe
            {
                ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.StreamShutdown(
                    _handle,
                    flags,
                    (ulong)errorCode),
                    "StreamShutdown failed");
            }

            if (abortDirection.HasFlag(QuicAbortDirection.Read))
            {
                _receiveTcs.TrySetException(ThrowHelper.GetOperationAbortedException(SR.net_quic_reading_aborted), final: true);
            }
            if (abortDirection.HasFlag(QuicAbortDirection.Write))
            {
                var exception = ThrowHelper.GetOperationAbortedException(SR.net_quic_writing_aborted);
                Interlocked.CompareExchange(ref _sendException, exception, null);
                if (Interlocked.CompareExchange(ref _sendLocked, 1, 0) == 0)
                {
                    _sendTcs.TrySetException(_sendException, final: true);
                    Volatile.Write(ref _sendLocked, 0);
                }
            }
        }

        /// <summary>
        /// Gracefully completes the writing side of the stream.
        /// Equivalent to using <see cref="WriteAsync(System.ReadOnlyMemory{byte},bool,System.Threading.CancellationToken)"/> with <c>completeWrites: true</c>.
        /// </summary>
        /// <remarks>
        /// Corresponds to an empty <see href="https://www.rfc-editor.org/rfc/rfc9000.html#frame-stream">STREAM</see> frame with <c>FIN</c> flag set to <c>true</c>.
        /// </remarks>
        public void CompleteWrites()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Nothing to complete, the writing side is already closed.
            if (_sendTcs.IsCompleted)
            {
                return;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"{this} Completing writes.");
            }
            unsafe
            {
                ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.StreamShutdown(
                    _handle,
                    QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL,
                    default),
                    "StreamShutdown failed");
            }
        }

        private ulong HandleEventStartComplete(ref QUIC_STREAM_EVENT.START_COMPLETE_DATA data)
        {
            Debug.Assert(_decrementStreamCapacity != null);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleEventReceive(ref QUIC_STREAM_EVENT.RECEIVE_DATA data)
        {
            ulong totalCopied = (ulong)_receiveBuffers.CopyFrom(
                new ReadOnlySpan<QUIC_BUFFER>(data.Buffers, (int)data.BufferCount),
                (int)data.TotalBufferLength,
                data.Flags.HasFlag(QUIC_RECEIVE_FLAGS.QUIC_RECEIVE_FLAG_FIN));

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
            _receiveBuffers.SetFinal();
            _receiveTcs.TrySetResult();
            return QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventPeerSendAborted(ref QUIC_STREAM_EVENT.PEER_SEND_ABORTED_DATA data)
        {
            _receiveTcs.TrySetException(ThrowHelper.GetStreamAbortedException((long)data.ErrorCode), final: true);
            return QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventPeerReceiveAborted(ref QUIC_STREAM_EVENT.PEER_RECEIVE_ABORTED_DATA data)
        {
            _sendTcs.TrySetException(ThrowHelper.GetStreamAbortedException((long)data.ErrorCode), final: true);
            return QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventSendShutdownComplete(ref QUIC_STREAM_EVENT.SEND_SHUTDOWN_COMPLETE_DATA data)
        {
            if (data.Graceful != 0)
            {
                _sendTcs.TrySetResult(final: true);
            }
            // If Graceful == 0, we either aborted write, received PEER_RECEIVE_ABORTED or will receive SHUTDOWN_COMPLETE(ConnectionClose) later, all of which completes the _sendTcs.
            return QUIC_STATUS_SUCCESS;
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

        private static ulong NativeCallback(QUIC_STREAM stream, object context, QUIC_STREAM_EVENT streamEvent)
        {
            GCHandle stateHandle = GCHandle.FromIntPtr((IntPtr)context);
            if (!stateHandle.IsAllocated || stateHandle.Target is not QuicStream instance)
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
