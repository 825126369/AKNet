/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MSQuic2
{
    internal class QUIC_SEND_REQUEST:CXPLAT_POOL_Interface<QUIC_SEND_REQUEST>
    {
        public CXPLAT_POOL<QUIC_SEND_REQUEST> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<QUIC_SEND_REQUEST> POOL_ENTRY = null;

        public QUIC_SEND_REQUEST Next;
        public QUIC_BUFFER[] Buffers;
        public int BufferCount;

        public QUIC_SEND_FLAGS Flags;
        public long StreamOffset;
        public int TotalLength; //字节数
        public readonly QUIC_BUFFER InternalBuffer = new QUIC_BUFFER();
        public object ClientContext;
        
        public QUIC_SEND_REQUEST()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_SEND_REQUEST>(this);
        }

        public CXPLAT_POOL_ENTRY<QUIC_SEND_REQUEST> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void SetPool(CXPLAT_POOL<QUIC_SEND_REQUEST> mPool)
        {
            this.mPool = mPool;
        }

        public CXPLAT_POOL<QUIC_SEND_REQUEST> GetPool()
        {
            return this.mPool;
        }

        public void Reset()
        {
            this.Next = null;
            this.Buffers = null;
            this.BufferCount = 0;
            this.Flags = 0;
            this.StreamOffset = 0;
            this.TotalLength = 0;
            this.ClientContext = null;
        }
    }

    public class QUIC_STREAM_FLAGS
    {
        private ulong AllFlags;
        public bool Allocated { get { return Get(0); } set { Set(0, value); } } //// Allocated by Connection. Used for Debugging.
        public bool Initialized { get { return Get(1); } set { Set(1, value); } }    // Initialized successfully. Used for Debugging.
        public bool Started { get { return Get(2); } set { Set(2, value); } }   // The app has started the stream.
        public bool StartedIndicated { get { return Get(3); } set { Set(3, value); } }    // The app received a start complete event.
        public bool PeerStreamStartEventActive { get { return Get(4); } set { Set(4, value); } }// The app is processing QUIC_CONNECTION_EVENT_PEER_STREAM_STARTED
        public bool Unidirectional { get { return Get(5); } set { Set(5, value); } }    // Sends/receives in 1 direction only.
        public bool Opened0Rtt { get { return Get(6); } set { Set(6, value); } }    // A 0-RTT packet opened the stream.
        public bool IndicatePeerAccepted { get { return Get(7); } set { Set(7, value); } }    // The app requested the PEER_ACCEPTED event.
        public bool SendOpen { get { return Get(8); } set { Set(8, value); } }   // Send a STREAM frame immediately on start.
        public bool SendOpenAcked { get { return Get(9); } set { Set(9, value); } }    // A STREAM frame has been acknowledged.
        public bool LocalNotAllowed { get { return Get(10); } set { Set(10, value); } }    // Peer's unidirectional stream.
        public bool LocalCloseFin { get { return Get(11); } set { Set(11, value); } }    // Locally closed (graceful).
        public bool LocalCloseReset { get { return Get(12); } set { Set(12, value); } }   // Locally closed (locally aborted).
        public bool LocalCloseResetReliable { get { return Get(13); } set { Set(13, value); } }   // Indicates that we should shutdown the send path once we sent/ACK'd ReliableOffsetSend bytes.
        public bool LocalCloseResetReliableAcked { get { return Get(14); } set { Set(14, value); } } // Indicates the peer has acknowledged we will stop sending once we sent/ACK'd ReliableOffsetSend bytes.
        public bool RemoteCloseResetReliable { get { return Get(15); } set { Set(15, value); } }   // Indicates that the peer initiated a reliable reset. Keep Recv path available for RecvMaxLength bytes.
        public bool ReceivedStopSending { get { return Get(16); } set { Set(16, value); } }    // Peer sent STOP_SENDING frame.
        public bool LocalCloseAcked { get { return Get(17); } set { Set(17, value); } }    // Any close acknowledged.
        public bool FinAcked { get { return Get(18); } set { Set(18, value); } }    // Our FIN was acknowledged.
        public bool InRecovery { get { return Get(19); } set { Set(19, value); } }    // Lost data is being retransmitted and is
        public bool RemoteNotAllowed { get { return Get(20); } set { Set(20, value); } }    // Our unidirectional stream.
        public bool RemoteCloseFin { get { return Get(21); } set { Set(21, value); } }    // Remotely closed.
        public bool RemoteCloseReset { get { return Get(22); } set { Set(22, value); } }    // Remotely closed (remotely aborted).
        public bool SentStopSending { get { return Get(23); } set { Set(23, value); } }   // We sent STOP_SENDING frame.
        public bool RemoteCloseAcked { get { return Get(24); } set { Set(24, value); } }    // Any close acknowledged.
        public bool SendEnabled { get { return Get(25); } set { Set(25, value); } }    // Application is allowed to send data.
        public bool ReceiveEnabled { get { return Get(26); } set { Set(26, value); } }   // Application is ready for receive callbacks.
        public bool ReceiveMultiple { get { return Get(27); } set { Set(27, value); } }   // The app supports multiple parallel receive indications.
        public bool UseAppOwnedRecvBuffers { get { return Get(28); } set { Set(28, value); } }    // The stream is using app provided receive buffers.
        public bool ReceiveFlushQueued { get { return Get(29); } set { Set(29, value); } }   // The receive flush operation is queued.
        public bool ReceiveDataPending { get { return Get(30); } set { Set(30, value); } }   // Data (or FIN) is queued and ready for delivery.
        public bool ReceiveCallActive { get { return Get(31); } set { Set(31, value); } }    // There is an active receive to the app.
        public bool SendDelayed { get { return Get(32); } set { Set(32, value); } }    // A delayed send is currently queued.
        public bool CancelOnLoss { get { return Get(33); } set { Set(33, value); } }    // Indicates that the stream is to be canceled
                                                                                      // if loss is detected.

        public bool HandleSendShutdown { get { return Get(34); } set { Set(34, value); } }    // Send shutdown complete callback delivered.
        public bool HandleShutdown { get { return Get(35); } set { Set(35, value); } }    // Shutdown callback delivered.
        public bool HandleClosed { get { return Get(36); } set { Set(36, value); } }    // Handle closed by application layer.

        public bool ShutdownComplete { get { return Get(37); } set { Set(37, value); } }    // Both directions have been shutdown and acknowledged.
        public bool Uninitialized { get { return Get(38); } set { Set(38, value); } }    // Uninitialize started/completed. Used for Debugging.
        public bool Freed { get { return Get(39); } set { Set(39, value); } }    // Freed after last ref count released. Used for Debugging.

        public bool InStreamTable { get { return Get(40); } set { Set(40, value); } }    // The stream is currently in the connection's table.
        public bool InWaitingList { get { return Get(41); } set { Set(41, value); } }    // The stream is currently in the waiting list for stream id FC.
        public bool DelayIdFcUpdate { get { return Get(42); } set { Set(42, value); } }    // Delay stream ID FC updates to StreamClose.
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Set(int nB, bool bTrue)
        {
            MSQuicFunc.SetFlag(ref AllFlags, MSQuicFunc.BIT(nB), bTrue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Get(int nB)
        {
            return MSQuicFunc.HasFlag(AllFlags, MSQuicFunc.BIT(nB));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            AllFlags = 0;
        }
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

    internal class QUIC_STREAM : QUIC_HANDLE, CXPLAT_POOL_Interface<QUIC_STREAM>
    {
        public CXPLAT_POOL<QUIC_STREAM> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<QUIC_STREAM> POOL_ENTRY = null;

        public long RefCount;
        public int[] RefTypeCount = new int[(int)QUIC_STREAM_REF.QUIC_STREAM_REF_COUNT];
        public uint OutstandingSentMetadata;
        public readonly CXPLAT_LIST_ENTRY WaitingLink;
        public readonly CXPLAT_LIST_ENTRY ClosedLink;
        public readonly CXPLAT_LIST_ENTRY SendLink;
        public QUIC_CONNECTION Connection;
        public ulong ID;
        public readonly QUIC_STREAM_FLAGS Flags = new QUIC_STREAM_FLAGS();
        public uint SendFlags;
        public uint OutFlowBlockedReasons;
        public readonly object ApiSendRequestLock = new object();
        public QUIC_SEND_REQUEST ApiSendRequests;
        public QUIC_SEND_REQUEST SendRequests;
        public QUIC_SEND_REQUEST SendRequestsTail;
        public QUIC_SEND_REQUEST SendBookmark; //发送标签，指向下一个要发送的字节所在的请求
        public QUIC_SEND_REQUEST SendBufferBookmark; //发送Buffer标签， 指向第一个非缓冲的发送请求
        public long QueuedSendOffset;//应用层已调用 Send() 提交，但尚未被发送引擎处理的数据的结束偏移量。
        public long Queued0Rtt;
        public long Sent0Rtt;

        //流控是基于偏移量的，不是基于长度的
        //发送端的一个核心流控（Flow Control）字段，表示当前允许发送的最大数据偏移量。
        //默认 64KB
        public long MaxAllowedSendOffset;
        public long SendWindow;
        public long LastIdealSendBuffer;
        //该流上已发送过的最大数据偏移量（exclusive）。
        //它记录了“我已经发送了从偏移 0 到 MaxSentLength 的数据”，是流控、重传和发送调度的核心依据。
        public long MaxSentLength;
        public long UnAckedOffset; //已发送但尚未被确认的最小数据偏移量
        public long NextSendOffset; //下一个要发送的数据的偏移量。

        //含义：在恢复/重新发送（Recovery）阶段，下一个要重传的偏移量。
        //等价于 “retransmission queue” 的读指针。
        //用途：用于 丢包重传（PTO 或丢失检测触发）。
        //指向第一个尚未重传的已丢失数据
        public long RecoveryNextOffset;
        public long RecoveryEndOffset; //恢复阶段需要重传的数据的结束偏移量。
        public int ReliableOffsetSend; //默认是0

        public int SendShutdownErrorCode;
        public int RecvShutdownErrorCode;

        //用于存储和管理所有被 ACK 的数据包范围
        //支持 稀疏、非连续的确认，符合 QUIC 协议设计。
        //是 RTT 计算、丢包检测、拥塞控制 的基础数据源。
        public readonly QUIC_RANGE SparseAckRanges = new QUIC_RANGE();
        public ushort SendPriority;
        public long MaxAllowedRecvOffset;
        public long RecvWindowBytesDelivered;
        public long RecvWindowLastUpdate;
        public readonly QUIC_RECV_BUFFER RecvBuffer = new QUIC_RECV_BUFFER();
        public long RecvMax0RttLength;
        public long RecvMaxLength;
        public long RecvPendingLength; //已排好队、等待应用来取
        public int RecvCompletionLength;
        public QUIC_STREAM_CALLBACK ClientCallbackHandler;
        public QUIC_OPERATION ReceiveCompleteOperation;
        public readonly QUIC_OPERATION ReceiveCompleteOperationStorage = new QUIC_OPERATION();
        public readonly QUIC_API_CONTEXT ReceiveCompleteApiCtxStorage = new QUIC_API_CONTEXT();
        public BlockedTimings_DATA BlockedTimings;

        //xuke 自定义
        public readonly QUIC_BUFFER[] StackRecvBuffers = new QUIC_BUFFER[3]
        {
            new QUIC_BUFFER(),new QUIC_BUFFER(),new QUIC_BUFFER(),
        };

        public struct BlockedTimings_DATA
        {
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER StreamIdFlowControl;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER FlowControl;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER App;
            public long CachedConnSchedulingUs;
            public long CachedConnPacingUs;
            public long CachedConnAmplificationProtUs;
            public long CachedConnCongestionControlUs;
            public long CachedConnFlowControlUs;

            public void Reset()
            {

            }
        }
        
        public QUIC_STREAM()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_STREAM>(this);
            WaitingLink = new CXPLAT_LIST_ENTRY<QUIC_STREAM>(this);
            ClosedLink = new CXPLAT_LIST_ENTRY<QUIC_STREAM>(this);
            SendLink = new CXPLAT_LIST_ENTRY<QUIC_STREAM>(this);
        }

        public CXPLAT_POOL_ENTRY<QUIC_STREAM> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void Reset()
        {

            RefCount = 0;
            Array.Clear(RefTypeCount, 0, (int)QUIC_STREAM_REF.QUIC_STREAM_REF_COUNT);
            OutstandingSentMetadata = 0;
            Connection = null;
            ID = 0;
            Flags.Reset();
            SendFlags = 0;
            OutFlowBlockedReasons = 0;
            ApiSendRequests = null;
            SendRequests = null;
            SendRequestsTail = null;
            SendBookmark = null;
            SendBufferBookmark = null;
            QueuedSendOffset = 0;
            Queued0Rtt = 0;
            Sent0Rtt = 0;

            MaxAllowedSendOffset = 0;
            SendWindow = 0;
            LastIdealSendBuffer = 0;
            MaxSentLength = 0;
            UnAckedOffset = 0;
            NextSendOffset = 0;
            RecoveryNextOffset = 0;
            RecoveryEndOffset = 0;
            ReliableOffsetSend = 0;

            SendShutdownErrorCode = 0;
            RecvShutdownErrorCode = 0;
            MSQuicFunc.QuicRangeReset(SparseAckRanges);

            SendPriority = 0;
            MaxAllowedRecvOffset = 0;
            RecvWindowBytesDelivered = 0;
            RecvWindowLastUpdate = 0;
            RecvBuffer.Reset();
            RecvMax0RttLength = 0;
            RecvMaxLength = 0;
            RecvPendingLength = 0;
            RecvCompletionLength = 0;
            ClientCallbackHandler = null;
            ReceiveCompleteOperation = null;
            ReceiveCompleteOperationStorage.Reset();
            ReceiveCompleteApiCtxStorage.Reset();
            BlockedTimings = new BlockedTimings_DATA();
        }

        public void SetPool(CXPLAT_POOL<QUIC_STREAM> mPool)
        {
            this.mPool = mPool;
        }

        public CXPLAT_POOL<QUIC_STREAM> GetPool()
        {
            return this.mPool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RECOV_WINDOW_OPEN()
        {
            return RecoveryNextOffset < RecoveryEndOffset;
        }
    }

    internal static partial class MSQuicFunc
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool STREAM_ID_IS_CLIENT(ulong ID)
        {
            return (ID & 1) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool STREAM_ID_IS_SERVER(ulong ID)
        {
            return (ID & 1) == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool STREAM_ID_IS_BI_DIR(ulong ID)
        {
            return (ID & 2) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool STREAM_ID_IS_UNI_DIR(ulong ID)
        {
            return (ID & 2) == 2;
        }

        static void QuicStreamAddRef(QUIC_STREAM Stream, QUIC_STREAM_REF Ref)
        {
            NetLog.Assert(Stream.Connection != null);
            NetLog.Assert(Stream.RefCount > 0);
#if DEBUG
            Interlocked.Increment(ref Stream.RefTypeCount[(int)Ref]);
#endif
            CxPlatRefIncrement(ref Stream.RefCount);
        }

        static int QuicStreamInitialize(QUIC_CONNECTION Connection, bool OpenedRemotely, QUIC_STREAM_OPEN_FLAGS Flags, out QUIC_STREAM NewStream)
        {
            int Status;
            NewStream = null;

            QUIC_RECV_CHUNK PreallocatedRecvChunk = null;
            QUIC_STREAM Stream = Connection.Partition.StreamPool.CxPlatPoolAlloc();
            if (Stream == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_STRM_ACTIVE);

            Stream.Type = QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_STREAM;
            Stream.Connection = Connection;
            Stream.ID = ulong.MaxValue;
            Stream.Flags.Unidirectional = Flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL);
            Stream.Flags.Opened0Rtt = Flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_0_RTT);
            Stream.Flags.DelayIdFcUpdate = Flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_DELAY_ID_FC_UPDATES);
            Stream.Flags.Allocated = true;
            Stream.Flags.SendEnabled = true;
            Stream.Flags.ReceiveEnabled = true;
            Stream.Flags.ReceiveMultiple = Connection.Settings.StreamMultiReceiveEnabled && !Stream.Flags.UseAppOwnedRecvBuffers;
            Stream.RecvMaxLength = long.MaxValue;
            Stream.RefCount = 1;
            Stream.SendRequestsTail = Stream.SendRequests = null;
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
            QUIC_RECV_BUF_MODE RecvBufferMode = QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_CIRCULAR;
            if (Stream.Flags.UseAppOwnedRecvBuffers)
            {
                RecvBufferMode = QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED;
            }
            else if (Stream.Flags.ReceiveMultiple)
            {
                RecvBufferMode = QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE;
            }

            if (InitialRecvBufferLength == QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE && RecvBufferMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED)
            {
                PreallocatedRecvChunk = Connection.Partition.DefaultReceiveBufferPool.CxPlatPoolAlloc();
                if (PreallocatedRecvChunk == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    goto Exit;
                }

                QuicRecvChunkInitialize(PreallocatedRecvChunk, InitialRecvBufferLength, PreallocatedRecvChunk.Buffer, false);
            }

            int FlowControlWindowSize = Stream.Flags.Unidirectional
                ? Connection.Settings.StreamRecvWindowUnidiDefault
                : OpenedRemotely
                    ? Connection.Settings.StreamRecvWindowBidiRemoteDefault
                    : Connection.Settings.StreamRecvWindowBidiLocalDefault;
            
            Status = QuicRecvBufferInitialize(
                    Stream.RecvBuffer,
                    InitialRecvBufferLength,
                    FlowControlWindowSize,
                    RecvBufferMode,
                    PreallocatedRecvChunk);

            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            Stream.MaxAllowedRecvOffset = Stream.RecvBuffer.VirtualBufferLength;
            Stream.RecvWindowLastUpdate = CxPlatTimeUs();
            QuicConnAddRef(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_STREAM);

            Stream.Flags.Initialized = true;
            NewStream = Stream;
            Stream = null;
            PreallocatedRecvChunk = null;

        Exit:
            if (Stream != null)
            {
                QuicPerfCounterDecrement(Connection.Partition,  QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_STRM_ACTIVE);
                Stream.Flags.Freed = true;
                Stream.GetPool().CxPlatPoolFree(Stream);
            }
            
            if (PreallocatedRecvChunk != null)
            {
                PreallocatedRecvChunk.GetPool().CxPlatPoolFree(PreallocatedRecvChunk);
            }

            return Status;
        }

        static int QuicStreamStart(QUIC_STREAM Stream, QUIC_STREAM_START_FLAGS Flags, bool IsRemoteStream)
        {
            int Status;
            bool ClosedLocally = Stream.Connection.State.ClosedLocally;
            if ((ClosedLocally || Stream.Connection.State.ClosedRemotely) || Stream.Flags.Started)
            {
                Status = (ClosedLocally || Stream.Flags.Started) ? QUIC_STATUS_INVALID_STATE : QUIC_STATUS_ABORTED;
                goto Exit;
            }

            if (!IsRemoteStream)
            {
                uint Type = QuicConnIsServer(Stream.Connection) ? STREAM_ID_FLAG_IS_SERVER : STREAM_ID_FLAG_IS_CLIENT;

                if (Stream.Flags.Unidirectional)
                {
                    Type |= STREAM_ID_FLAG_IS_UNI_DIR;
                }

                Status = QuicStreamSetNewLocalStream(Stream.Connection.Streams, Type, Flags.HasFlag(QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_FAIL_BLOCKED), Stream);
                if (QUIC_FAILED(Status))
                {
                    goto Exit;
                }
            }
            else
            {
                Status = QUIC_STATUS_SUCCESS;
            }

            Stream.Flags.Started = true;
            Stream.Flags.IndicatePeerAccepted = Flags.HasFlag(QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_INDICATE_PEER_ACCEPT);

            long Now = CxPlatTimeUs();
            Stream.BlockedTimings.CachedConnSchedulingUs = Stream.Connection.BlockedTimings.Scheduling.CumulativeTimeUs +
                (Stream.Connection.BlockedTimings.Scheduling.LastStartTimeUs != 0 ?
                    CxPlatTimeDiff(Stream.Connection.BlockedTimings.Scheduling.LastStartTimeUs, Now) : 0);

            Stream.BlockedTimings.CachedConnPacingUs =
                Stream.Connection.BlockedTimings.Pacing.CumulativeTimeUs +
                (Stream.Connection.BlockedTimings.Pacing.LastStartTimeUs != 0 ?
                    CxPlatTimeDiff(Stream.Connection.BlockedTimings.Pacing.LastStartTimeUs, Now) : 0);

            Stream.BlockedTimings.CachedConnAmplificationProtUs =
                Stream.Connection.BlockedTimings.AmplificationProt.CumulativeTimeUs +
                (Stream.Connection.BlockedTimings.AmplificationProt.LastStartTimeUs != 0 ?
                    CxPlatTimeDiff(Stream.Connection.BlockedTimings.AmplificationProt.LastStartTimeUs, Now) : 0);

            Stream.BlockedTimings.CachedConnCongestionControlUs =
                Stream.Connection.BlockedTimings.CongestionControl.CumulativeTimeUs +
                (Stream.Connection.BlockedTimings.CongestionControl.LastStartTimeUs != 0 ?
                    CxPlatTimeDiff(Stream.Connection.BlockedTimings.CongestionControl.LastStartTimeUs, Now) : 0);

            Stream.BlockedTimings.CachedConnFlowControlUs =
                Stream.Connection.BlockedTimings.FlowControl.CumulativeTimeUs +
                (Stream.Connection.BlockedTimings.FlowControl.LastStartTimeUs != 0 ?
                    CxPlatTimeDiff(Stream.Connection.BlockedTimings.FlowControl.LastStartTimeUs, Now) : 0);

            if (Stream.Flags.SendEnabled)
            {
                QuicStreamAddOutFlowBlockedReason(Stream, QUIC_FLOW_BLOCKED_APP);
            }

            if (Stream.SendFlags != 0)
            {
                QuicSendQueueFlushForStream(Stream.Connection.Send, Stream, false);
            }

            Stream.Flags.SendOpen = Flags.HasFlag(QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_IMMEDIATE);
            if (Stream.Flags.SendOpen)
            {
                QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_OPEN, false);
            }

            Stream.MaxAllowedSendOffset = QuicStreamGetInitialMaxDataFromTP(Stream.ID, QuicConnIsServer(Stream.Connection), Stream.Connection.PeerTransportParams);
            if (Stream.MaxAllowedSendOffset == 0)
            {
                QuicStreamAddOutFlowBlockedReason(Stream, QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL);
            }
            Stream.SendWindow = Math.Min(Stream.MaxAllowedSendOffset, uint.MaxValue);
        Exit:

            if (!IsRemoteStream)
            {
                QuicStreamIndicateStartComplete(Stream, Status);

                if (QUIC_FAILED(Status) && Flags.HasFlag(QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL))
                {
                    QuicStreamShutdown(Stream, QUIC_STREAM_SHUTDOWN_FLAG_ABORT | QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE, 0);
                }
            }

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

                QuicStreamShutdown(Stream, QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND |
                         QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE |
                         QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE,
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

        static void QuicStreamShutdown(QUIC_STREAM Stream, uint Flags, int ErrorCode)
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

            if (BoolOk(Flags & QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE))
            {
                QuicStreamRecvShutdown(Stream, BoolOk(Flags & QUIC_STREAM_SHUTDOWN_SILENT), ErrorCode);
            }

            if (BoolOk(Flags & QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE) && !Stream.Flags.ShutdownComplete)
            {
                if (Stream.Flags.RemoteCloseResetReliable || Stream.Flags.LocalCloseResetReliable)
                {
                    return;
                }
                QuicStreamIndicateSendShutdownComplete(Stream, false);
                QuicStreamIndicateShutdownComplete(Stream);
            }
        }


        static bool QuicStreamRelease(QUIC_STREAM Stream, QUIC_STREAM_REF Ref)
        {
            NetLog.Assert(Stream.Connection != null);
            NetLog.Assert(Stream.RefCount > 0);

#if DEBUG
            NetLog.Assert(Stream.RefTypeCount[(int)Ref] > 0, Ref);
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
            NetLog.Assert(Stream.ClosedLink.Next == null);
            NetLog.Assert(Stream.SendLink.Next == null);

            Stream.Flags.Uninitialized = true;
            NetLog.Assert(Stream.ApiSendRequests == null);
            NetLog.Assert(Stream.SendRequests == null);

            QuicPerfCounterDecrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_STRM_ACTIVE);

            if (Stream.RecvBuffer.PreallocatedChunk != null)
            {
                Connection.Partition.DefaultReceiveBufferPool.CxPlatPoolFree(Stream.RecvBuffer.PreallocatedChunk);
            }

            Stream.Flags.Freed = true;
            Connection.Partition.StreamPool.CxPlatPoolFree(Stream);

            if (WasStarted)
            {
            }
            QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_STREAM);
        }

        static void QuicStreamIndicateStartComplete(QUIC_STREAM Stream, int Status)
        {
            if (Stream.Flags.StartedIndicated)
            {
                return;
            }
            Stream.Flags.StartedIndicated = true;

            QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
            Event.Type = QUIC_STREAM_EVENT_START_COMPLETE;
            Event.START_COMPLETE.Status = Status;
            Event.START_COMPLETE.ID = Stream.ID;
            Event.START_COMPLETE.PeerAccepted = QUIC_SUCCEEDED(Status) && !BoolOk(Stream.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL);
            QuicStreamIndicateEvent(Stream, ref Event);
        }

        static int QuicStreamIndicateEvent(QUIC_STREAM Stream, ref QUIC_STREAM_EVENT Event)
        {
            int Status;
            if (Stream.ClientCallbackHandler != null)
            {
                NetLog.Assert(!Stream.Connection.State.InlineApiExecution ||
                    Stream.Connection.State.HandleClosed ||
                    Stream.Flags.HandleClosed ||
                    Event.Type == QUIC_STREAM_EVENT_START_COMPLETE);

                Status = Stream.ClientCallbackHandler(Stream, Stream.ClientContext, ref Event);
            }
            else
            {
                Status = QUIC_STATUS_INVALID_STATE;
            }
            return Status;
        }

        static int QuicStreamProvideRecvBuffers(QUIC_STREAM Stream, CXPLAT_LIST_ENTRY Chunks)
        {
            int Status = QuicRecvBufferProvideChunks(Stream.RecvBuffer, Chunks);
            if (Status == QUIC_STATUS_SUCCESS)
            {
                long NewMaxAllowedRecvOffset = Stream.RecvBuffer.BaseOffset + Stream.RecvBuffer.VirtualBufferLength;
                if (Stream.MaxAllowedRecvOffset < NewMaxAllowedRecvOffset)
                {
                    Stream.MaxAllowedRecvOffset = Stream.RecvBuffer.BaseOffset + Stream.RecvBuffer.VirtualBufferLength;
                    QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_MAX_DATA, false);
                }
            }
            return Status;
        }

        static void QuicStreamSwitchToAppOwnedBuffers(QUIC_STREAM Stream)
        {
            QUIC_WORKER Worker = Stream.Connection.Worker;
            QuicRecvBufferUninitialize(Stream.RecvBuffer);
            if (Stream.RecvBuffer.PreallocatedChunk != null)
            {
                Worker.Partition.DefaultReceiveBufferPool.CxPlatPoolFree(Stream.RecvBuffer.PreallocatedChunk);
                Stream.RecvBuffer.PreallocatedChunk = null;
            }

            QuicRecvBufferInitialize(Stream.RecvBuffer, 0, 0, QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED, null);
            Stream.Flags.UseAppOwnedRecvBuffers = true;
        }

        static void QuicStreamIndicateShutdownComplete(QUIC_STREAM Stream)
        {
            if (!Stream.Flags.HandleShutdown)
            {
                Stream.Flags.HandleShutdown = true;

                QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                Event.Type = QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_SHUTDOWN_COMPLETE;
                Event.SHUTDOWN_COMPLETE.ConnectionShutdown = Stream.Connection.State.ClosedLocally || Stream.Connection.State.ClosedRemotely;
                Event.SHUTDOWN_COMPLETE.AppCloseInProgress = Stream.Flags.HandleClosed;
                Event.SHUTDOWN_COMPLETE.ConnectionShutdownByApp = Stream.Connection.State.AppClosed;
                Event.SHUTDOWN_COMPLETE.ConnectionClosedRemotely = Stream.Connection.State.ClosedRemotely;
                Event.SHUTDOWN_COMPLETE.ConnectionErrorCode = Stream.Connection.CloseErrorCode;
                Event.SHUTDOWN_COMPLETE.ConnectionCloseStatus = Stream.Connection.CloseStatus;
                QuicStreamIndicateEvent(Stream, ref Event);
                Stream.ClientCallbackHandler = null;
            }
        }

        static void QuicStreamTryCompleteShutdown(QUIC_STREAM Stream)
        {
            if (!Stream.Flags.ShutdownComplete && !Stream.Flags.ReceiveDataPending &&
                Stream.Flags.LocalCloseAcked && Stream.Flags.RemoteCloseAcked)
            {
                QuicSendClearStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAGS_ALL);
                Stream.Flags.ShutdownComplete = true;
                QuicStreamIndicateShutdownComplete(Stream);

                if (!Stream.Flags.DelayIdFcUpdate || Stream.Flags.HandleClosed)
                {
                    QuicStreamSetReleaseStream(Stream.Connection.Streams, Stream);
                }
            }
        }

        static bool QuicStreamAddOutFlowBlockedReason(QUIC_STREAM Stream, uint Reason)
        {
            NetLog.Assert((Reason & QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL) == 0);
            NetLog.Assert((Reason & (Reason - 1)) == 0, "More than one reason is not allowed");
            if (!BoolOk(Stream.OutFlowBlockedReasons & Reason))
            {
                long Now = CxPlatTimeUs();
                if (BoolOk(Reason & QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL))
                {
                    Stream.BlockedTimings.FlowControl.LastStartTimeUs = Now;
                }

                if (BoolOk(Reason & QUIC_FLOW_BLOCKED_APP))
                {
                    Stream.BlockedTimings.App.LastStartTimeUs = Now;
                }

                Stream.OutFlowBlockedReasons |= Reason;
                return true;
            }
            return false;
        }

        static long QuicStreamGetInitialMaxDataFromTP(ulong StreamID, bool IsServer, QUIC_TRANSPORT_PARAMETERS TransportParams)
        {
            if (STREAM_ID_IS_UNI_DIR(StreamID))
            {
                return TransportParams.InitialMaxStreamDataUni;
            }
            else if (IsServer)
            {
                if (STREAM_ID_IS_CLIENT(StreamID))
                {
                    return TransportParams.InitialMaxStreamDataBidiLocal;
                }
                else
                {
                    return TransportParams.InitialMaxStreamDataBidiRemote;
                }
            }
            else
            {
                if (STREAM_ID_IS_CLIENT(StreamID))
                {
                    return TransportParams.InitialMaxStreamDataBidiRemote;
                }
                else
                {
                    return TransportParams.InitialMaxStreamDataBidiLocal;
                }
            }
        }
        
        static void QuicStreamSentMetadataIncrement(QUIC_STREAM Stream)
        {
            if (++Stream.OutstandingSentMetadata == 1)
            {
                QuicStreamAddRef(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_SEND_PACKET);
            }
            NetLog.Assert(Stream.OutstandingSentMetadata != 0);
        }

        static void QuicStreamSentMetadataDecrement(QUIC_STREAM Stream)
        {
            NetLog.Assert(Stream.OutstandingSentMetadata != 0);
            if (--Stream.OutstandingSentMetadata == 0)
            {
                QuicStreamRelease(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_SEND_PACKET);
            }
        }

        static void QuicStreamNotifyReceiveBufferNeeded(QUIC_STREAM Stream, long BufferLengthNeeded)
        {
            NetLog.Assert(Stream.RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);

            QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
            Event.Type =  QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_RECEIVE_BUFFER_NEEDED;
            Event.RECEIVE_BUFFER_NEEDED.BufferLengthNeeded = BufferLengthNeeded;
            QuicStreamIndicateEvent(Stream, ref Event);
        }

    }


}
