using AKNet.Common;
using System.Collections.Generic;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_SEND_REQUEST
    {
        public QUIC_SEND_REQUEST Next;
        public List<QUIC_BUFFER> Buffers;
        public uint Flags;
        public long StreamOffset;
        public long TotalLength;
        public QUIC_BUFFER InternalBuffer;
    }

    public class QUIC_STREAM_FLAGS
    {
        public ulong AllFlags;

        public bool Allocated              ;    // Allocated by Connection. Used for Debugging.
        public bool Initialized            ;    // Initialized successfully. Used for Debugging.
        public bool Started                ;    // The app has started the stream.
        public bool StartedIndicated       ;    // The app received a start complete event.
        public bool PeerStreamStartEventActive; // The app is processing QUIC_CONNECTION_EVENT_PEER_STREAM_STARTED
        public bool Unidirectional         ;    // Sends/receives in 1 direction only.
        public bool Opened0Rtt             ;    // A 0-RTT packet opened the stream.
        public bool IndicatePeerAccepted   ;    // The app requested the PEER_ACCEPTED event.
        
        public bool SendOpen               ;    // Send a STREAM frame immediately on start.
        public bool SendOpenAcked          ;    // A STREAM frame has been acknowledged.

        public bool LocalNotAllowed        ;    // Peer's unidirectional stream.
        public bool LocalCloseFin          ;    // Locally closed (graceful).
        public bool LocalCloseReset        ;    // Locally closed (locally aborted).
        public bool LocalCloseResetReliable;    // Indicates that we should shutdown the send path once we sent/ACK'd ReliableOffsetSend bytes.
        public bool LocalCloseResetReliableAcked; // Indicates the peer has acknowledged we will stop sending once we sent/ACK'd ReliableOffsetSend bytes.
        public bool RemoteCloseResetReliable;   // Indicates that the peer initiated a reliable reset. Keep Recv path available for RecvMaxLength bytes.
        public bool ReceivedStopSending    ;    // Peer sent STOP_SENDING frame.
        public bool LocalCloseAcked        ;    // Any close acknowledged.
        public bool FinAcked               ;    // Our FIN was acknowledged.
        public bool InRecovery             ;    // Lost data is being retransmitted and is
                                                // unacknowledged.

        public bool RemoteNotAllowed       ;    // Our unidirectional stream.
        public bool RemoteCloseFin         ;    // Remotely closed.
        public bool RemoteCloseReset       ;    // Remotely closed (remotely aborted).
        public bool SentStopSending        ;    // We sent STOP_SENDING frame.
        public bool RemoteCloseAcked       ;    // Any close acknowledged.

        public bool SendEnabled            ;    // Application is allowed to send data.
        public bool ReceiveEnabled         ;    // Application is ready for receive callbacks.
        public bool ReceiveMultiple        ;    // The app supports multiple parallel receive indications.
        public bool UseAppOwnedRecvBuffers ;    // The stream is using app provided receive buffers.
        public bool ReceiveFlushQueued     ;    // The receive flush operation is queued.
        public bool ReceiveDataPending     ;    // Data (or FIN) is queued and ready for delivery.
        public bool ReceiveCallActive      ;    // There is an active receive to the app.
        public bool SendDelayed            ;    // A delayed send is currently queued.
        public bool CancelOnLoss           ;    // Indicates that the stream is to be canceled
                                                // if loss is detected.

        public bool HandleSendShutdown     ;    // Send shutdown complete callback delivered.
        public bool HandleShutdown         ;    // Shutdown callback delivered.
        public bool HandleClosed           ;    // Handle closed by application layer.

        public bool ShutdownComplete       ;    // Both directions have been shutdown and acknowledged.
        public bool Uninitialized          ;    // Uninitialize started/completed. Used for Debugging.
        public bool Freed                  ;    // Freed after last ref count released. Used for Debugging.

        public bool InStreamTable          ;    // The stream is currently in the connection's table.
        public bool InWaitingList          ;    // The stream is currently in the waiting list for stream id FC.
        public bool DelayIdFcUpdate        ;    // Delay stream ID FC updates to StreamClose.

    }

    internal enum QUIC_STREAM_SEND_STATE
    {
        QUIC_STREAM_SEND_DISABLED,
        QUIC_STREAM_SEND_STARTED,
        QUIC_STREAM_SEND_RESET,
        QUIC_STREAM_SEND_RESET_ACKED,
        QUIC_STREAM_SEND_FIN,
        QUIC_STREAM_SEND_FIN_ACKED,
        QUIC_STREAM_SEND_RELIABLE_RESET,
        QUIC_STREAM_SEND_RELIABLE_RESET_ACKED
    }

    internal enum QUIC_STREAM_RECV_STATE
    {
        QUIC_STREAM_RECV_DISABLED,
        QUIC_STREAM_RECV_STARTED,
        QUIC_STREAM_RECV_PAUSED,
        QUIC_STREAM_RECV_STOPPED,
        QUIC_STREAM_RECV_RESET,
        QUIC_STREAM_RECV_FIN,
        QUIC_STREAM_RECV_RELIABLE_RESET
    }

    internal enum QUIC_STREAM_REF
    {
        QUIC_STREAM_REF_APP,
        QUIC_STREAM_REF_STREAM_SET,
        QUIC_STREAM_REF_SEND,
        QUIC_STREAM_REF_SEND_PACKET,
        QUIC_STREAM_REF_LOOKUP,
        QUIC_STREAM_REF_OPERATION,
        QUIC_STREAM_REF_COUNT
    }
    internal class QUIC_STREAM:QUIC_HANDLE
    {
        public long RefCount;
        public int[] RefTypeCount = new int[(int)QUIC_STREAM_REF.QUIC_STREAM_REF_COUNT];
        public uint OutstandingSentMetadata;

        public CXPLAT_HASHTABLE_ENTRY_QUIC_STREAM TableEntry;
        public CXPLAT_LIST_ENTRY_QUIC_STREAM WaitingLink;
        public CXPLAT_LIST_ENTRY_QUIC_STREAM ClosedLink;
        public CXPLAT_LIST_ENTRY_QUIC_STREAM SendLink;
        public CXPLAT_LIST_ENTRY_QUIC_STREAM AllStreamsLink;
        public QUIC_CONNECTION Connection;

        public ulong ID;
        public QUIC_STREAM_FLAGS Flags;
        public uint SendFlags;

        public uint OutFlowBlockedReasons;
        public readonly object ApiSendRequestLock = new object();
        public QUIC_SEND_REQUEST ApiSendRequests;
        public QUIC_SEND_REQUEST SendRequests;
        public QUIC_SEND_REQUEST SendRequestsTail;

        public QUIC_SEND_REQUEST SendBookmark;
        public QUIC_SEND_REQUEST SendBufferBookmark;
        public long QueuedSendOffset;
        public long Queued0Rtt;
        public long Sent0Rtt;
        public long MaxAllowedSendOffset;
        public uint SendWindow;
        public ulong LastIdealSendBuffer;
        public long MaxSentLength;

        public ulong UnAckedOffset;
        public ulong NextSendOffset;
        public ulong RecoveryNextOffset;
        public ulong RecoveryEndOffset;
        public ulong ReliableOffsetSend;
        
        public ulong SendShutdownErrorCode;
        public QUIC_RANGE SparseAckRanges;
        public ushort SendPriority;
        public long MaxAllowedRecvOffset;
        public long RecvWindowBytesDelivered;

        public long RecvWindowLastUpdate;

        public QUIC_RECV_BUFFER RecvBuffer;

        public long RecvMax0RttLength;
        public long RecvMaxLength;
        public long RecvPendingLength;
        public long RecvCompletionLength;

        public ulong RecvShutdownErrorCode;
        public QUIC_STREAM_CALLBACK ClientCallbackHandler;
        public QUIC_OPERATION ReceiveCompleteOperation;
        public QUIC_OPERATION ReceiveCompleteOperationStorage;
        public QUIC_API_CONTEXT ReceiveCompleteApiCtxStorage;

        public class BlockedTimings_Class
        {
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER StreamIdFlowControl;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER FlowControl;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER App;
            public ulong CachedConnSchedulingUs;
            public ulong CachedConnPacingUs;
            public ulong CachedConnAmplificationProtUs;
            public ulong CachedConnCongestionControlUs;
            public ulong CachedConnFlowControlUs;
        }

        public BlockedTimings_Class BlockedTimings;
    }

    internal static partial class MSQuicFunc
    {
        static bool STREAM_ID_IS_CLIENT(ulong ID)
        {
            return (ID & 1) == 0;
        }

        static bool STREAM_ID_IS_SERVER(ulong ID)
        {
            return (ID & 1) == 1;
        }

        static bool STREAM_ID_IS_BI_DIR(ulong ID)
        {
            return (ID & 2) == 0;
        }

        static bool STREAM_ID_IS_UNI_DIR(ulong ID)
        {
            return (ID & 2) == 2;
        }

        static void QuicStreamAddRef(QUIC_STREAM Stream, QUIC_STREAM_REF Ref)
        {
            NetLog.Assert(Stream.Connection != null);
            NetLog.Assert(Stream.RefCount > 0);
#if DEBUG
            Interlocked.Increment(ref Stream.RefTypeCount[Ref]);
#else
        
#endif
            CxPlatRefIncrement(ref Stream.RefCount);
        }

        static ulong QuicStreamInitialize(QUIC_CONNECTION Connection, bool OpenedRemotely, QUIC_STREAM_OPEN_FLAGS Flags, QUIC_STREAM NewStream)
        {
            ulong Status;
            QUIC_STREAM Stream;
            QUIC_RECV_CHUNK PreallocatedRecvChunk = null;
            QUIC_WORKER Worker = Connection.Worker;

            Stream = CxPlatPoolAlloc(Worker.StreamPool);
            if (Stream == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }
#if DEBUG
            Monitor.Enter(Connection.Streams.AllStreamsLock);
            CxPlatListInsertTail(Connection.Streams.AllStreams, Stream.AllStreamsLink);
            Monitor.Exit(Connection.Streams.AllStreamsLock);
#endif
            QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_STRM_ACTIVE);

            Stream.Type = QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_STREAM;
            Stream.Connection = Connection;
            Stream.ID = ulong.MaxValue;
            Stream.Flags.Unidirectional = BoolOk((uint)(Flags & QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL));
            Stream.Flags.Opened0Rtt = BoolOk((uint)(Flags & QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_0_RTT));
            Stream.Flags.DelayIdFcUpdate = BoolOk((uint)(Flags & QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_DELAY_ID_FC_UPDATES));

            if (Stream.Flags.DelayIdFcUpdate)
            {

            }
            Stream.Flags.Allocated = true;
            Stream.Flags.SendEnabled = true;
            Stream.Flags.ReceiveEnabled = true;
            Stream.Flags.UseAppOwnedRecvBuffers = BoolOk((uint)(Flags & QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_APP_OWNED_BUFFERS));

            Stream.Flags.ReceiveMultiple = Connection.Settings.StreamMultiReceiveEnabled && !Stream.Flags.UseAppOwnedRecvBuffers;

            Stream.RecvMaxLength = ulong.MaxValue;
            Stream.RefCount = 1;
            Stream.SendRequestsTail = Stream.SendRequests;
            Stream.SendPriority = (ushort)QUIC_STREAM_PRIORITY_DEFAULT;
            CxPlatRefInitialize(ref Stream.RefCount);
            QuicRangeInitialize(QUIC_MAX_RANGE_ALLOC_SIZE, Stream.SparseAckRanges);

            Stream.ReceiveCompleteOperation = Stream.ReceiveCompleteOperationStorage;
            Stream.ReceiveCompleteOperationStorage.API_CALL.Context = Stream.ReceiveCompleteApiCtxStorage;
            Stream.ReceiveCompleteOperation.Type = QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL;
            Stream.ReceiveCompleteOperation.FreeAfterProcess = false;
            Stream.ReceiveCompleteOperation.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_STRM_RECV_COMPLETE;
            Stream.ReceiveCompleteOperation.API_CALL.Context.STRM_RECV_COMPLETE.Stream = Stream;

#if DEBUG
            Stream.RefTypeCount[(int)QUIC_STREAM_REF.QUIC_STREAM_REF_APP] = 1;
#endif

            if (Stream.Flags.Unidirectional)
            {
                if (!OpenedRemotely)
                {
                    Stream.Flags.RemoteNotAllowed = true;
                    Stream.Flags.RemoteCloseAcked = true;
                    Stream.Flags.ReceiveEnabled = false;
                }
                else
                {
                    Stream.Flags.LocalNotAllowed = true;
                    Stream.Flags.LocalCloseAcked = true;
                    Stream.Flags.SendEnabled = false;
                    Stream.Flags.HandleSendShutdown = true;
                }
            }
            
            int InitialRecvBufferLength = Connection.Settings.StreamRecvBufferDefault;
            QUIC_RECV_BUF_MODE RecvBufferMode =  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_CIRCULAR;
            if (Stream.Flags.UseAppOwnedRecvBuffers)
            {
                RecvBufferMode =  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED;
            }
            else if (Stream.Flags.ReceiveMultiple)
            {
                RecvBufferMode =  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE;
            }

            if (InitialRecvBufferLength == QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE &&
                RecvBufferMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED)
            {
                PreallocatedRecvChunk = CxPlatPoolAlloc(Worker.DefaultReceiveBufferPool);
                if (PreallocatedRecvChunk == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    goto Exit;
                }
                QuicRecvChunkInitialize(PreallocatedRecvChunk, InitialRecvBufferLength, (uint8_t*)(PreallocatedRecvChunk + 1), false);
            }

            const uint FlowControlWindowSize = Stream.Flags.Unidirectional
                ? Connection.Settings.StreamRecvWindowUnidiDefault
                : OpenedRemotely
                    ? Connection.Settings.StreamRecvWindowBidiRemoteDefault
                    : Connection.Settings.StreamRecvWindowBidiLocalDefault;

            Status =
                QuicRecvBufferInitialize(
                    Stream.RecvBuffer,
                    InitialRecvBufferLength,
                    FlowControlWindowSize,
                    RecvBufferMode,
                    Connection.Worker.AppBufferChunkPool,
                    PreallocatedRecvChunk);

            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            Stream.MaxAllowedRecvOffset = Stream.RecvBuffer.VirtualBufferLength;
            Stream.RecvWindowLastUpdate = mStopwatch.ElapsedMilliseconds;
            QuicConnAddRef(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_STREAM);

            Stream.Flags.Initialized = true;
            NewStream = Stream;
            Stream = null;
            PreallocatedRecvChunk = null;
        Exit:
            return Status;
        }


        static void QuicStreamClose(QUIC_STREAM Stream)
        {
            NetLog.Assert(!Stream.Flags.HandleClosed);
            Stream.Flags.HandleClosed = true;

            if (!Stream.Flags.ShutdownComplete)
            {
                if (Stream.Flags.Started && !Stream.Flags.HandleShutdown)
                {

                }

                QuicStreamShutdown(Stream,
                   QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND |
                         QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE |
                         QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE,
                         QUIC_ERROR_NO_ERROR);

                if (!Stream.Flags.Started)
                {
                    Stream.Flags.ShutdownComplete = true;
                    NetLog.Assert(!Stream.Flags.InStreamTable);
                }
            }

            if (Stream.Flags.DelayIdFcUpdate && Stream.Flags.ShutdownComplete)
            {
                QuicStreamSetReleaseStream(Stream.Connection.Streams, Stream);
            }

            Stream.ClientCallbackHandler = null;
            QuicStreamRelease(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_APP);
        }

        static void QuicStreamShutdown(QUIC_STREAM Stream, uint Flags, ulong ErrorCode)
        {
            NetLog.Assert(Flags != 0 && Flags != QUIC_STREAM_SHUTDOWN_SILENT);
            NetLog.Assert(!BoolOk(Flags & QUIC_STREAM_SHUTDOWN_FLAG_GRACEFUL) || !BoolOk(Flags & (QUIC_STREAM_SHUTDOWN_FLAG_ABORT | QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE)));
            NetLog.Assert(!BoolOk(Flags & QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE) ||
                Flags == (QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE |
                          QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE |
                          QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND));

            if (BoolOk(Flags & (QUIC_STREAM_SHUTDOWN_FLAG_GRACEFUL | QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND)))
            {
                QuicStreamSendShutdown(
                    Stream,
                    BoolOk(Flags & QUIC_STREAM_SHUTDOWN_FLAG_GRACEFUL),
                    BoolOk(Flags & QUIC_STREAM_SHUTDOWN_SILENT),
                    false,
                    ErrorCode);
            }

            if (!!(Flags & QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE))
            {
                QuicStreamRecvShutdown(
                    Stream,
                    !!(Flags & QUIC_STREAM_SHUTDOWN_SILENT),
                    ErrorCode);
            }

            if (!!(Flags & QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE) &&
                !Stream->Flags.ShutdownComplete)
            {
                //
                // The app has requested that we immediately give them completion
                // events so they don't have to wait. Deliver the send shutdown complete
                // and shutdown complete events now, if they haven't already been
                // delivered.
                //
                if (Stream->Flags.RemoteCloseResetReliable || Stream->Flags.LocalCloseResetReliable)
                {
                    QuicTraceLogStreamWarning(
                       ShutdownImmediatePendingReliableReset,
                       Stream,
                       "Invalid immediate shutdown request (pending reliable reset).");
                    return;
                }
                QuicStreamIndicateSendShutdownComplete(Stream, FALSE);
                QuicStreamIndicateShutdownComplete(Stream);
            }
        }


        static bool QuicStreamRelease(QUIC_STREAM Stream, QUIC_STREAM_REF Ref)
        {
            NetLog.Assert(Stream.Connection != null);
            NetLog.Assert(Stream.RefCount > 0);

#if DEBUG
            NetLog.Assert(Stream.RefTypeCount[(int)Ref] > 0);
            ushort result = (ushort)Interlocked.Decrement(ref Stream.RefTypeCount[(int)Ref]);
            NetLog.Assert(result != 0xFFFF);
#endif

            if (CxPlatRefDecrement(ref Stream.RefCount))
            {
#if DEBUG
                for (int i = 0; i < (int)QUIC_STREAM_REF.QUIC_STREAM_REF_COUNT; i++)
                {
                    NetLog.Assert(Stream.RefTypeCount[i] == 0);
                }
#endif
                QuicStreamFree(Stream);
                return true;
            }
            return false;
        }

        static void QuicStreamFree(QUIC_STREAM Stream)
        {
            bool WasStarted = Stream.Flags.Started;
            QUIC_CONNECTION Connection = Stream.Connection;
            QUIC_WORKER Worker = Connection.Worker;

            NetLog.Assert(Stream.RefCount == 0);
            NetLog.Assert(Connection.State.ClosedLocally || Stream.Flags.ShutdownComplete);
            NetLog.Assert(Connection.State.ClosedLocally || Stream.Flags.HandleClosed);
            NetLog.Assert(!Stream.Flags.InStreamTable);
            NetLog.Assert(!Stream.Flags.InWaitingList);
            NetLog.Assert(Stream.ClosedLink.Flink == null);
            NetLog.Assert(Stream.SendLink.Flink == null);

            Stream.Flags.Uninitialized = true;
            NetLog.Assert(Stream.ApiSendRequests == null);
            NetLog.Assert(Stream.SendRequests == null);

#if DEBUG
            Monitor.Enter(Connection.Streams.AllStreamsLock);
            CxPlatListEntryRemove(Stream.AllStreamsLink);
            Monitor.Exit(Connection.Streams.AllStreamsLock);
#endif
            QuicPerfCounterDecrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_STRM_ACTIVE);

            if (Stream.RecvBuffer.PreallocatedChunk)
            {
                CxPlatPoolFree(Worker.DefaultReceiveBufferPool, Stream.RecvBuffer.PreallocatedChunk);
            }

            Stream.Flags.Freed = true;
            CxPlatPoolFree(Worker.StreamPool, Stream);

            if (WasStarted)
            {
            }
            QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_STREAM);
        }

        static void QuicStreamIndicateStartComplete(QUIC_STREAM Stream, ulong Status)
        {
            if (Stream.Flags.StartedIndicated)
            {
                return;
            }
            Stream.Flags.StartedIndicated = true;

            QUIC_STREAM_EVENT Event;
            Event.Type =  QUIC_STREAM_EVENT_START_COMPLETE;
            Event.START_COMPLETE.Status = Status;
            Event.START_COMPLETE.ID = Stream.ID;
            Event.START_COMPLETE.PeerAccepted = QUIC_SUCCEEDED(Status) && !(Stream.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL);
            QuicStreamIndicateEvent(Stream, Event);
        }

        static ulong QuicStreamIndicateEvent(QUIC_STREAM Stream, QUIC_STREAM_EVENT Event)
        {
            ulong Status;
            if (Stream.ClientCallbackHandler != null)
            {
                NetLog.Assert(!Stream.Connection.State.InlineApiExecution ||
                    Stream.Connection.State.HandleClosed ||
                    Stream.Flags.HandleClosed ||
                    Event.Type == QUIC_STREAM_EVENT_START_COMPLETE);

                Status = Stream.ClientCallbackHandler((QUIC_HANDLE)Stream, Stream.ClientContext, Event);
            }
            else
            {
                Status = QUIC_STATUS_INVALID_STATE;
            }
            return Status;
        }

        static ulong QuicStreamProvideRecvBuffers(QUIC_STREAM Stream, CXPLAT_LIST_ENTRY Chunks)
        {
            ulong Status = QuicRecvBufferProvideChunks(Stream.RecvBuffer, Chunks);
            if (Status == QUIC_STATUS_SUCCESS)
            {
                Stream.MaxAllowedRecvOffset = Stream.RecvBuffer.BaseOffset + Stream.RecvBuffer.VirtualBufferLength;
                QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_MAX_DATA, false);
            }
            return Status;
        }

        static void QuicStreamSwitchToAppOwnedBuffers(QUIC_STREAM Stream)
        {
            QUIC_WORKER Worker = Stream.Connection.Worker;
            QuicRecvBufferUninitialize(Stream.RecvBuffer);
            if (Stream.RecvBuffer.PreallocatedChunk != null)
            {
                CxPlatPoolFree(Worker.DefaultReceiveBufferPool, Stream.RecvBuffer.PreallocatedChunk);
                Stream.RecvBuffer.PreallocatedChunk = null;
            }
            
            QuicRecvBufferInitialize(Stream.RecvBuffer, 0, 0, QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED, Worker.AppBufferChunkPool, null);
            Stream.Flags.UseAppOwnedRecvBuffers = true;
        }

    }
}
