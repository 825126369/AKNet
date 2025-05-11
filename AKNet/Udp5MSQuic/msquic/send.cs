using AKNet.Common;
using System;
using System.IO;

namespace AKNet.Udp5MSQuic.Common
{
    internal enum QUIC_SEND_FLUSH_REASON
    {
        REASON_CONNECTION_FLAGS,
        REASON_STREAM_FLAGS,
        REASON_PROBE,
        REASON_LOSS,
        REASON_ACK,
        REASON_TRANSPORT_PARAMETERS,
        REASON_CONGESTION_CONTROL,
        REASON_CONNECTION_FLOW_CONTROL,
        REASON_NEW_KEY,
        REASON_STREAM_FLOW_CONTROL,
        REASON_STREAM_ID_FLOW_CONTROL,
        REASON_AMP_PROTECTION,
        REASON_SCHEDULING,
        REASON_ROUTE_COMPLETION,
    }

    internal class QUIC_FLOW_BLOCKED_TIMING_TRACKER
    {
        public long CumulativeTimeUs;
        public long LastStartTimeUs;
    }

    internal class QUIC_SEND
    {
        public bool FlushOperationPending;
        public bool DelayedAckTimerActive;
        public bool LastFlushTimeValid;
        public bool TailLossProbeNeeded;
        public bool Uninitialized;
        public ulong NextPacketNumber;
        public long LastFlushTime;
        public int NumPacketsSentWithEct;
        public long MaxData;
        public int PeerMaxData;
        public int OrderedStreamBytesReceived;
        public int OrderedStreamBytesSent;
        public long OrderedStreamBytesDeliveredAccumulator;
        public uint SendFlags;
        public readonly CXPLAT_LIST_ENTRY SendStreams = new CXPLAT_LIST_ENTRY<QUIC_STREAM>(null);
        public QUIC_BUFFER InitialToken;
        public readonly QUIC_CONNECTION mConnection;

        public QUIC_SEND(QUIC_CONNECTION mConnection)
        {
            this.mConnection = mConnection;
        }
    }

    internal enum QUIC_SEND_RESULT
    {
        QUIC_SEND_COMPLETE,
        QUIC_SEND_INCOMPLETE,
        QUIC_SEND_DELAYED_PACING
    }

    internal static partial class MSQuicFunc
    {
        static void QuicSendInitialize(QUIC_SEND Send, QUIC_SETTINGS Settings)
        {
            CxPlatListInitializeHead(Send.SendStreams);
            Send.MaxData = Settings.ConnFlowControlWindow;
        }

        static void QuicSendUninitialize(QUIC_SEND Send)
        {
            Send.Uninitialized = true;
            Send.DelayedAckTimerActive = false;
            Send.SendFlags = 0;

            if (Send.InitialToken != null)
            {
                Send.InitialToken = null;
            }

            CXPLAT_LIST_ENTRY Entry = Send.SendStreams.Next;
            while (Entry != Send.SendStreams)
            {
                QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(Entry);
                NetLog.Assert(Stream.SendFlags != 0);
                Entry = Entry.Next;
                Stream.SendFlags = 0;
                Stream.SendLink.Next = null;
                QuicStreamRelease(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_SEND);
            }
        }

        static bool QuicSendCanSendFlagsNow(QUIC_SEND Send)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            if (Connection.Crypto.TlsState.WriteKey < QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                if (Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT] != null && CxPlatListIsEmpty(Send.SendStreams))
                {
                    return true;
                }

                if ((!Connection.State.Started && QuicConnIsClient(Connection)) || !BoolOk(Send.SendFlags &  QUIC_CONN_SEND_FLAG_ALLOWED_HANDSHAKE))
                {
                    return false;
                }
            }
            return true;
        }

        static void QuicSendQueueFlush(QUIC_SEND Send, QUIC_SEND_FLUSH_REASON Reason)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            if (!Send.FlushOperationPending && QuicSendCanSendFlagsNow(Send))
            {
                QUIC_OPERATION Oper;
                if ((Oper = QuicOperationAlloc(Connection.Worker,  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_SEND)) != null)
                {
                    Send.FlushOperationPending = true;
                    QuicConnQueueOper(Connection, Oper);
                }
            }
        }

        static bool QuicSendSetSendFlag(QUIC_SEND Send, uint SendFlags)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            bool IsCloseFrame = BoolOk(SendFlags & (QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE | QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE));
            bool CanSetFlag = !QuicConnIsClosed(Connection) || IsCloseFrame;

            if (BoolOk(SendFlags & QUIC_CONN_SEND_FLAG_ACK) && Send.DelayedAckTimerActive)
            {
                QuicConnTimerCancel(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_ACK_DELAY);
                Send.DelayedAckTimerActive = false;
            }

            if (CanSetFlag && (Send.SendFlags & SendFlags) != SendFlags)
            {
                Send.SendFlags |= SendFlags;
                QuicSendQueueFlush(Send, QUIC_SEND_FLUSH_REASON.REASON_CONNECTION_FLAGS);
            }

            if (IsCloseFrame)
            {
                QuicSendClear(Send);
            }

            QuicSendValidate(Send);

            return CanSetFlag;
        }

        static void QuicSendQueueFlushForStream(QUIC_SEND Send,QUIC_STREAM Stream, bool DelaySend)
        {
            if (Stream.SendLink.Next == null)
            {
                CXPLAT_LIST_ENTRY Entry = Send.SendStreams.Prev;
                while (Entry != Send.SendStreams)
                {
                    if (Stream.SendPriority <= CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(Entry).SendPriority)
                    {
                        break;
                    }
                    Entry = Entry.Prev;
                }
                CxPlatListInsertHead(Entry, Stream.SendLink);
                QuicStreamAddRef(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_SEND);
            }

            if (DelaySend)
            {
                Stream.Flags.SendDelayed = true;

            }
            else if (Stream.Connection.State.Started)
            {
                Stream.Flags.SendDelayed = false;
                QuicSendQueueFlush(Send, QUIC_SEND_FLUSH_REASON.REASON_STREAM_FLAGS);
            }
        }

        static bool QuicSendSetStreamSendFlag(QUIC_SEND Send,QUIC_STREAM Stream, uint SendFlags, bool DelaySend)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            if (QuicConnIsClosed(Connection))
            {
                return false;
            }

            if (Stream.Flags.LocalCloseAcked)
            {
                SendFlags &=
                    ~(QUIC_STREAM_SEND_FLAG_SEND_ABORT |
                      QUIC_STREAM_SEND_FLAG_RELIABLE_ABORT |
                      QUIC_STREAM_SEND_FLAG_DATA_BLOCKED |
                      QUIC_STREAM_SEND_FLAG_DATA |
                      QUIC_STREAM_SEND_FLAG_OPEN |
                      QUIC_STREAM_SEND_FLAG_FIN);
            }
            else if (Stream.Flags.LocalCloseReset)
            {
                SendFlags &=
                    ~(QUIC_STREAM_SEND_FLAG_DATA_BLOCKED |
                      QUIC_STREAM_SEND_FLAG_DATA |
                      QUIC_STREAM_SEND_FLAG_OPEN |
                      QUIC_STREAM_SEND_FLAG_FIN);
            }
            if (Stream.Flags.RemoteCloseAcked)
            {
                SendFlags &= ~(QUIC_STREAM_SEND_FLAG_RECV_ABORT | QUIC_STREAM_SEND_FLAG_MAX_DATA);
            }
            else if (Stream.Flags.RemoteCloseFin || Stream.Flags.RemoteCloseReset)
            {
                SendFlags &= ~QUIC_STREAM_SEND_FLAG_MAX_DATA;
            }

            if ((Stream.SendFlags | SendFlags) != Stream.SendFlags || (Stream.Flags.SendDelayed && BoolOk(SendFlags & QUIC_STREAM_SEND_FLAG_DATA)))
            {
                if (Stream.Flags.Started)
                {
                    QuicSendQueueFlushForStream(Send, Stream, DelaySend);
                }
                Stream.SendFlags |= SendFlags;
            }

            return SendFlags != 0;
        }

        static void QuicSendClearStreamSendFlag(QUIC_SEND Send,QUIC_STREAM Stream, uint SendFlags)
        {
            if (BoolOk(Stream.SendFlags & SendFlags))
            {
                Stream.SendFlags &= ~SendFlags;

                if (Stream.SendFlags == 0 && Stream.SendLink.Next != null)
                {
                    CxPlatListEntryRemove(Stream.SendLink);
                    Stream.SendLink.Next = null;
                    QuicStreamRelease(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_SEND);
                }
            }
        }

        static void QuicSendClearSendFlag(QUIC_SEND Send, uint SendFlags)
        {
            if (BoolOk(Send.SendFlags & SendFlags))
            {
                Send.SendFlags &= ~SendFlags;
            }

            QuicSendValidate(Send);
        }

        static void QuicSendProcessDelayedAckTimer(QUIC_SEND Send)
        {
            NetLog.Assert(Send.DelayedAckTimerActive);
            NetLog.Assert(!BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_ACK));
            Send.DelayedAckTimerActive = false;

            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            bool AckElicitingPacketsToAcknowledge = false;
            for (int i = 0; i < (int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_COUNT; ++i)
            {
                if (Connection.Packets[i] != null && BoolOk(Connection.Packets[i].AckTracker.AckElicitingPacketsToAcknowledge))
                {
                    AckElicitingPacketsToAcknowledge = true;
                    break;
                }
            }

            NetLog.Assert(AckElicitingPacketsToAcknowledge);
            if (AckElicitingPacketsToAcknowledge)
            {
                Send.SendFlags |= QUIC_CONN_SEND_FLAG_ACK;
            }

            QuicSendValidate(Send);
        }

        static bool CxPlatIsRouteReady(QUIC_CONNECTION Connection, QUIC_PATH Path)
        {
            if (Path.Route.State == CXPLAT_ROUTE_STATE.RouteResolved)
            {
                return true;
            }

            if (Path.Route.State == CXPLAT_ROUTE_STATE.RouteUnresolved || Path.Route.State == CXPLAT_ROUTE_STATE.RouteSuspected)
            {
                QuicConnAddRef(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_ROUTE);
                ulong Status = CxPlatResolveRoute(Path.Route);
                if (Status == QUIC_STATUS_SUCCESS)
                {
                    QuicConnRelease(Connection,  QUIC_CONNECTION_REF.QUIC_CONN_REF_ROUTE);
                    return true;
                }
                NetLog.Assert(Status == QUIC_STATUS_PENDING || QUIC_FAILED(Status));
            }
            return false;
        }

        static bool QuicSendFlush(QUIC_SEND Send)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            QUIC_PATH Path = Connection.Paths[0];

            NetLog.Assert(!Connection.State.HandleClosed);
            if (!CxPlatIsRouteReady(Connection, Path))
            {
                return true;
            }

            QuicConnTimerCancel(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_PACING);
            QuicConnRemoveOutFlowBlockedReason(Connection, QUIC_FLOW_BLOCKED_SCHEDULING | QUIC_FLOW_BLOCKED_PACING);

            if (Path.DestCid == null)
            {
                return true;
            }

            long TimeNow = CxPlatTime();
            QuicMtuDiscoveryCheckSearchCompleteTimeout(Connection, TimeNow);

            if (!Path.IsPeerValidated)
            {
                Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_DPLPMTUD;
            }

            if (Send.SendFlags == 0 && CxPlatListIsEmpty(Send.SendStreams))
            {
                return true;
            }

            if (Connection.Settings.DestCidUpdateIdleTimeoutMs != 0 && Send.LastFlushTimeValid &&
                CxPlatTimeDiff64(Send.LastFlushTime, TimeNow) >= Connection.Settings.DestCidUpdateIdleTimeoutMs)
            {
                QuicConnRetireCurrentDestCid(Connection, Path);
            }

            QUIC_SEND_RESULT Result = QUIC_SEND_RESULT.QUIC_SEND_INCOMPLETE;
            QUIC_STREAM Stream = null;
            int StreamPacketCount = 0;

            if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_PATH_CHALLENGE))
            {
                Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_PATH_CHALLENGE;
                QuicSendPathChallenges(Send);
            }

            QUIC_PACKET_BUILDER Builder = new QUIC_PACKET_BUILDER();
            if (!QuicPacketBuilderInitialize(Builder, Connection, Path))
            {
                return true;
            }
            NetLog.Assert(Builder.Metadata != null);

            if (Builder.Path.EcnValidationState == ECN_VALIDATION_STATE.ECN_VALIDATION_CAPABLE)
            {
                Builder.EcnEctSet = true;
            }
            else if (Builder.Path.EcnValidationState == ECN_VALIDATION_STATE.ECN_VALIDATION_TESTING)
            {
                if (Builder.Path.EcnTestingEndingTime != 0)
                {
                    if (!CxPlatTimeAtOrBefore64(TimeNow, Builder.Path.EcnTestingEndingTime))
                    {
                        Builder.Path.EcnValidationState = ECN_VALIDATION_STATE.ECN_VALIDATION_UNKNOWN;
                    }
                }
                else
                {
                    long ThreePtosInUs = QuicLossDetectionComputeProbeTimeout(
                            Connection.LossDetection,
                            Connection.Paths[0],
                            QUIC_CLOSE_PTO_COUNT);
                    Builder.Path.EcnTestingEndingTime = TimeNow + ThreePtosInUs;
                }
                Builder.EcnEctSet = true;
            }

            do
            {

                if (Path.Allowance < QUIC_MIN_SEND_ALLOWANCE)
                {
                    Result = QUIC_SEND_RESULT.QUIC_SEND_COMPLETE;
                    break;
                }

                uint SendFlags = Send.SendFlags;
                if (Connection.Crypto.TlsState.WriteKey < QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
                {
                    SendFlags &= QUIC_CONN_SEND_FLAG_ALLOWED_HANDSHAKE;
                }
                if (Path.Allowance != uint.MaxValue)
                {
                    SendFlags &= ~QUIC_CONN_SEND_FLAG_DATAGRAM;
                }

                if (!QuicPacketBuilderHasAllowance(Builder))
                {
                    SendFlags &= QUIC_CONN_SEND_FLAGS_BYPASS_CC;
                    if (SendFlags == 0)
                    {
                        if (QuicCongestionControlCanSend(Connection.CongestionControl))
                        {
                            QuicConnAddOutFlowBlockedReason(Connection, QUIC_FLOW_BLOCKED_PACING);
                            QuicConnTimerSet(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_PACING, QUIC_SEND_PACING_INTERVAL);
                            Result = QUIC_SEND_RESULT.QUIC_SEND_DELAYED_PACING;
                        }
                        else
                        {
                            Result = QUIC_SEND_RESULT.QUIC_SEND_COMPLETE;
                        }
                        break;
                    }
                }

                bool WrotePacketFrames;
                bool FlushBatchedDatagrams = false;
                if ((SendFlags & ~QUIC_CONN_SEND_FLAG_DPLPMTUD) != 0)
                {
                    NetLog.Assert(QuicSendCanSendFlagsNow(Send));
                    if (!QuicPacketBuilderPrepareForControlFrames(Builder, Send.TailLossProbeNeeded,
                            SendFlags & ~QUIC_CONN_SEND_FLAG_DPLPMTUD))
                    {
                        break;
                    }
                    WrotePacketFrames = QuicSendWriteFrames(Send, Builder);
                }
                else if ((SendFlags & QUIC_CONN_SEND_FLAG_DPLPMTUD) != 0)
                {
                    if (!QuicPacketBuilderPrepareForPathMtuDiscovery(Builder))
                    {
                        break;
                    }
                    FlushBatchedDatagrams = true;
                    Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_DPLPMTUD;
                    if (Builder.Metadata.FrameCount < QUIC_MAX_FRAMES_PER_PACKET &&
                        Builder.Datagram.Length < Builder.Datagram.Length - Builder.EncryptionOverhead)
                    {
                        Builder.Datagram.Buffer[Builder.Datagram.Length++] = (byte)QUIC_FRAME_TYPE.QUIC_FRAME_PING;
                        Builder.Metadata.Frames[Builder.Metadata.FrameCount++].Type = QUIC_FRAME_TYPE.QUIC_FRAME_PING;
                        WrotePacketFrames = true;
                    }
                    else
                    {
                        WrotePacketFrames = false;
                    }
                }
                else if (Stream != null ||
                    (Stream = QuicSendGetNextStream(Send, StreamPacketCount)) != null)
                {
                    if (!QuicPacketBuilderPrepareForStreamFrames(Builder, Send.TailLossProbeNeeded))
                    {
                        break;
                    }

                    QUIC_PACKET_SPACE Packets = Connection.Packets[(int)Builder.EncryptLevel];
                    byte ZeroRttPacketType = Connection.Stats.QuicVersion == QUIC_VERSION_2 ? (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_0_RTT_PROTECTED_V2 : (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_0_RTT_PROTECTED_V1;
                    WrotePacketFrames = Builder.PacketType != ZeroRttPacketType && QuicAckTrackerHasPacketsToAck(Packets.AckTracker) &&
                        QuicAckTrackerAckFrameEncode(Packets.AckTracker, Builder);

                    WrotePacketFrames |= QuicStreamSendWrite(Stream, Builder);
                    if (Stream.SendFlags == 0 && Stream.SendLink.Next != null)
                    {
                        CxPlatListEntryRemove(Stream.SendLink);
                        Stream.SendLink.Next = null;
                        QuicStreamRelease(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_SEND);
                        Stream = null;
                    }
                    else if ((WrotePacketFrames && --StreamPacketCount == 0) || !QuicSendCanSendStreamNow(Stream))
                    {
                        Stream = null;
                    }
                }
                else
                {
                    Result = QUIC_SEND_RESULT.QUIC_SEND_COMPLETE;
                    break;
                }

                Send.TailLossProbeNeeded = false;

                if (!WrotePacketFrames || Builder.Metadata.FrameCount == QUIC_MAX_FRAMES_PER_PACKET ||
                    Builder.Datagram.Length - Builder.Datagram.Length < QUIC_MIN_PACKET_SPARE_SPACE)
                {
                    if (!QuicPacketBuilderFinalize(Builder, !WrotePacketFrames || FlushBatchedDatagrams))
                    {
                        break;
                    }
                }

            } while (Builder.SendData != null ||
                Builder.TotalCountDatagrams < QUIC_MAX_DATAGRAMS_PER_SEND);

            if (Builder.SendData != null)
            {
                QuicPacketBuilderFinalize(Builder, true);
                NetLog.Assert(Builder.SendData == null);
            }

            QuicPacketBuilderCleanup(Builder);
            if (Result == QUIC_SEND_RESULT.QUIC_SEND_INCOMPLETE)
            {
                QuicConnAddOutFlowBlockedReason(Connection, QUIC_FLOW_BLOCKED_SCHEDULING);
                QuicSendQueueFlush(Connection.Send, QUIC_SEND_FLUSH_REASON.REASON_SCHEDULING);
                if (Builder.TotalCountDatagrams + 1 > Connection.PeerPacketTolerance)
                {
                    QuicConnUpdatePeerPacketTolerance(Connection, (byte)(Builder.TotalCountDatagrams + 1));
                }
            }

            return Result != QUIC_SEND_RESULT.QUIC_SEND_INCOMPLETE;
        }

        static void QuicSendPathChallenges(QUIC_SEND Send)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            NetLog.Assert(Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT] != null);

            for (int i = 0; i < Connection.PathsCount; ++i)
            {
                QUIC_PATH Path = Connection.Paths[i];
                if (!Connection.Paths[i].SendChallenge || Connection.Paths[i].Allowance < QUIC_MIN_SEND_ALLOWANCE)
                {
                    continue;
                }

                if (!CxPlatIsRouteReady(Connection, Path))
                {
                    Send.SendFlags |= QUIC_CONN_SEND_FLAG_PATH_CHALLENGE;
                    continue;
                }

                QUIC_PACKET_BUILDER Builder = new QUIC_PACKET_BUILDER();
                if (!QuicPacketBuilderInitialize(Builder, Connection, Path))
                {
                    continue;
                }
                NetLog.Assert(Builder.Metadata != null);

                if (!QuicPacketBuilderPrepareForControlFrames(Builder, false, QUIC_CONN_SEND_FLAG_PATH_CHALLENGE))
                {
                    continue;
                }

                if (!Path.IsMinMtuValidated)
                {
                    Builder.MinimumDatagramLength = MaxUdpPayloadSizeForFamily(QuicAddrGetFamily(Builder.Path.Route.RemoteAddress), Builder.Path.Mtu);

                    if (Builder.MinimumDatagramLength > Builder.Datagram.Length)
                    {
                        Builder.MinimumDatagramLength = Builder.Datagram.Length;
                    }
                }

                int AvailableBufferLength = Builder.Datagram.Length - Builder.EncryptionOverhead;

                QUIC_PATH_CHALLENGE_EX Frame = new QUIC_PATH_CHALLENGE_EX();
                Array.Copy(Path.Challenge, Frame.Data, Frame.Data.Length);

                QUIC_SSBuffer mBuf = new QUIC_SSBuffer(Builder.Datagram.Buffer, Builder.Datagram.Length, AvailableBufferLength);
                bool Result = QuicPathChallengeFrameEncode(QUIC_FRAME_TYPE.QUIC_FRAME_PATH_CHALLENGE, Frame, ref mBuf);
                NetLog.Assert(Result);
                if (Result)
                {
                    Array.Copy(Frame.Data, Builder.Metadata.Frames[0].PATH_CHALLENGE.Data, Frame.Data.Length);
                    Result = QuicPacketBuilderAddFrame(Builder,  QUIC_FRAME_TYPE.QUIC_FRAME_PATH_CHALLENGE, true);
                    NetLog.Assert(!Result);
                    Path.SendChallenge = false;
                }

                QuicPacketBuilderFinalize(Builder, true);
                QuicPacketBuilderCleanup(Builder);
            }
        }


        static void QuicSendValidate(QUIC_SEND Send)
        {
            if (Send.Uninitialized)
            {
                return;
            }

            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            bool HasAckElicitingPacketsToAcknowledge = false;
            for (int i = 0; i < (int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_COUNT; ++i)
            {
                if (Connection.Packets[i] != null)
                {
                    if (Connection.Packets[i].AckTracker.AckElicitingPacketsToAcknowledge > 0)
                    {
                        HasAckElicitingPacketsToAcknowledge = true;
                        break;
                    }
                }
            }

            if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_ACK))
            {
                NetLog.Assert(!Send.DelayedAckTimerActive);
                NetLog.Assert(HasAckElicitingPacketsToAcknowledge);
            }
            else if (Send.DelayedAckTimerActive)
            {
                NetLog.Assert(HasAckElicitingPacketsToAcknowledge);
            }
            else if (!Connection.State.ClosedLocally && !Connection.State.ClosedRemotely)
            {
                NetLog.Assert(!HasAckElicitingPacketsToAcknowledge);
            }
        }

        static void QuicSendUpdateAckState(QUIC_SEND Send)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            bool HasAckElicitingPacketsToAcknowledge = false;
            for (int i = 0; i < (int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_COUNT; ++i)
            {
                if (Connection.Packets[i] != null && BoolOk(Connection.Packets[i].AckTracker.AckElicitingPacketsToAcknowledge))
                {
                    HasAckElicitingPacketsToAcknowledge = true;
                    break;
                }
            }

            if (!HasAckElicitingPacketsToAcknowledge)
            {
                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_ACK))
                {
                    NetLog.Assert(!Send.DelayedAckTimerActive);
                    Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_ACK;
                }
                else if (Send.DelayedAckTimerActive)
                {
                    QuicConnTimerCancel(Connection,  QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_ACK_DELAY);
                    Send.DelayedAckTimerActive = false;
                }
            }

            QuicSendValidate(Send);
        }

        static byte QuicKeyTypeToPacketTypeV1(QUIC_PACKET_KEY_TYPE KeyType)
        {
            switch (KeyType)
            {
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL:
                    return (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1;
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT:
                    return (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_0_RTT_PROTECTED_V1;
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE:
                    return (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_HANDSHAKE_V1;
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT:
                default:
                    return SEND_PACKET_SHORT_HEADER_TYPE;
            }
        }

        static byte QuicKeyTypeToPacketTypeV2(QUIC_PACKET_KEY_TYPE KeyType)
        {
            switch (KeyType)
            {
                case  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL: 
                    return (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2;
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT: 
                    return (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_0_RTT_PROTECTED_V2;
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE: 
                    return (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_HANDSHAKE_V2;
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT:
                default: 
                    return SEND_PACKET_SHORT_HEADER_TYPE;
            }
        }

        static byte QuicEncryptLevelToPacketTypeV1(QUIC_ENCRYPT_LEVEL Level)
        {
            switch (Level)
            {
                case QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_INITIAL: 
                    return (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1;
                case QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_HANDSHAKE: 
                    return (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_HANDSHAKE_V1;
                case QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT:
                default: 
                    return SEND_PACKET_SHORT_HEADER_TYPE;
            }
        }

        static byte QuicEncryptLevelToPacketTypeV2(QUIC_ENCRYPT_LEVEL Level)
        {
            switch (Level)
            {
                case QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_INITIAL:
                    return (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1;
                case QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_HANDSHAKE:
                    return (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_HANDSHAKE_V1;
                case QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT:
                default:
                    return SEND_PACKET_SHORT_HEADER_TYPE;
            }
        }

        static bool QuicSendWriteFrames(QUIC_SEND Send, QUIC_PACKET_BUILDER Builder)
        {
            NetLog.Assert(Builder.Metadata.FrameCount < QUIC_MAX_FRAMES_PER_PACKET);
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            int AvailableBufferLength = Builder.Datagram.Length - Builder.EncryptionOverhead;
            int PrevFrameCount = Builder.Metadata.FrameCount;
            bool RanOutOfRoom = false;

            QUIC_PACKET_SPACE Packets = Connection.Packets[(int)Builder.EncryptLevel];
            NetLog.Assert(Packets != null);

            bool IsCongestionControlBlocked = !QuicPacketBuilderHasAllowance(Builder);
            bool Is1RttEncryptionLevel =
                Builder.Metadata.Flags.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT ||
                Builder.Metadata.Flags.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT;

            byte ZeroRttPacketType = Connection.Stats.QuicVersion == QUIC_VERSION_2 ?
                   (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_0_RTT_PROTECTED_V2 : (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_0_RTT_PROTECTED_V1;

            if (Builder.PacketType != ZeroRttPacketType && QuicAckTrackerHasPacketsToAck(Packets.AckTracker))
            {
                if (!QuicAckTrackerAckFrameEncode(Packets.AckTracker, Builder))
                {
                    RanOutOfRoom = true;
                    goto Exit;
                }
            }

            if (!IsCongestionControlBlocked && BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_CRYPTO))
            {
                if (QuicCryptoWriteFrames(Connection.Crypto, Builder))
                {
                    if (Builder.Metadata.FrameCount == QUIC_MAX_FRAMES_PER_PACKET)
                    {
                        return true;
                    }
                }
                else
                {
                    RanOutOfRoom = true;
                }
            }

            if (BoolOk(Send.SendFlags & (QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE | QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE)))
            {
                bool IsApplicationClose = BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE);
                if (Connection.State.ClosedRemotely)
                {
                    IsApplicationClose = false;
                }

                ulong CloseErrorCode = Connection.CloseErrorCode;
                string CloseReasonPhrase = Connection.CloseReasonPhrase;

                if (IsApplicationClose && !Is1RttEncryptionLevel)
                {
                    CloseErrorCode = QUIC_ERROR_APPLICATION_ERROR;
                    CloseReasonPhrase = null;
                    IsApplicationClose = false;
                }

                QUIC_CONNECTION_CLOSE_EX Frame = new QUIC_CONNECTION_CLOSE_EX()
                {
                    ApplicationClosed = IsApplicationClose,
                    ErrorCode = CloseErrorCode,
                    FrameType = 0,
                    ReasonPhrase = CloseReasonPhrase
                };

                if (QuicConnCloseFrameEncode(Frame, ref Builder.Datagram.Length, AvailableBufferLength, Builder.Datagram.Buffer))
                {
                    Builder.WrittenConnectionCloseFrame = true;
                    if (Builder.Key.Type == Connection.Crypto.TlsState.WriteKey)
                    {
                        Send.SendFlags &= ~(QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE | QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE);
                    }

                    QuicPacketBuilderAddFrame(Builder, IsApplicationClose ? QUIC_FRAME_TYPE.QUIC_FRAME_CONNECTION_CLOSE_1 : QUIC_FRAME_TYPE.QUIC_FRAME_CONNECTION_CLOSE, false);
                }
                else
                {
                    return false;
                }

                return true;
            }

            if (IsCongestionControlBlocked)
            {
                RanOutOfRoom = true;
                goto Exit;
            }

            if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_PATH_RESPONSE))
            {
                int i;
                for (i = 0; i < Connection.PathsCount; ++i)
                {
                    QUIC_PATH TempPath = Connection.Paths[i];
                    if (!TempPath.SendResponse)
                    {
                        continue;
                    }

                    QUIC_PATH_CHALLENGE_EX Frame = new QUIC_PATH_CHALLENGE_EX();
                    TempPath.Response.CopyTo(Frame.Data, Frame.Data.Length);

                    QUIC_SSBuffer mBuf = new QUIC_SSBuffer(Builder.Datagram.Buffer, Builder.Datagram.Length, AvailableBufferLength);
                    if (QuicPathChallengeFrameEncode(QUIC_FRAME_TYPE.QUIC_FRAME_PATH_RESPONSE, Frame, ref mBuf))
                    {
                        TempPath.SendResponse = false;
                        Frame.Data.CopyTo(Builder.Metadata.Frames[Builder.Metadata.FrameCount].PATH_RESPONSE.Data, 0);
                        if (QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_PATH_RESPONSE, true))
                        {
                            break;
                        }
                    }
                    else
                    {
                        RanOutOfRoom = true;
                        break;
                    }
                }

                if (i == Connection.PathsCount)
                {
                    Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_PATH_RESPONSE;
                }

                if (Builder.Metadata.FrameCount == QUIC_MAX_FRAMES_PER_PACKET)
                {
                    return true;
                }
            }

            if (Is1RttEncryptionLevel)
            {
                if (Builder.Metadata.Flags.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT && BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_HANDSHAKE_DONE))
                {
                    if (Builder.Datagram.Length < AvailableBufferLength)
                    {
                        Builder.Datagram.Buffer[Builder.Datagram.Length++] = (byte)QUIC_FRAME_TYPE.QUIC_FRAME_HANDSHAKE_DONE;
                        Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_HANDSHAKE_DONE;
                        Builder.MinimumDatagramLength = Builder.Datagram.Length;
                        if (QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_HANDSHAKE_DONE, true))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        RanOutOfRoom = true;
                    }
                }

                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_DATA_BLOCKED))
                {
                    QUIC_DATA_BLOCKED_EX Frame = new QUIC_DATA_BLOCKED_EX()
                    {
                        DataLimit = (ulong)Send.OrderedStreamBytesSent
                    };

                    if (QuicDataBlockedFrameEncode(Frame, ref Builder.Datagram.Length, AvailableBufferLength, Builder.Datagram.Buffer))
                    {
                        Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_DATA_BLOCKED;
                        if (QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_DATA_BLOCKED, true))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        RanOutOfRoom = true;
                    }
                }

                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_MAX_DATA))
                {

                    QUIC_MAX_DATA_EX Frame = new QUIC_MAX_DATA_EX()
                    {
                        MaximumData = (int)Send.MaxData
                    };

                    if (QuicMaxDataFrameEncode(Frame, ref Builder.Datagram.Length, AvailableBufferLength, Builder.Datagram.Buffer))
                    {
                        Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_MAX_DATA;
                        if (QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_MAX_DATA, true))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        RanOutOfRoom = true;
                    }
                }

                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_MAX_STREAMS_BIDI))
                {
                    QUIC_MAX_STREAMS_EX Frame = new QUIC_MAX_STREAMS_EX()
                    {
                        BidirectionalStreams = true,
                        MaximumStreams = 0
                    };

                    Frame.MaximumStreams = QuicConnIsServer(Connection) ?
                            Connection.Streams.Types[STREAM_ID_FLAG_IS_CLIENT | STREAM_ID_FLAG_IS_BI_DIR].MaxTotalStreamCount :
                            Connection.Streams.Types[STREAM_ID_FLAG_IS_SERVER | STREAM_ID_FLAG_IS_BI_DIR].MaxTotalStreamCount;

                    if (QuicMaxStreamsFrameEncode(Frame, ref Builder.Datagram.Length, AvailableBufferLength, Builder.Datagram.Buffer))
                    {
                        Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_MAX_STREAMS_BIDI;
                        if (QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAMS, true))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        RanOutOfRoom = true;
                    }
                }

                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_BIDI_STREAMS_BLOCKED))
                {
                    ulong Mask = QuicConnIsServer(Connection) ? (1 | STREAM_ID_FLAG_IS_BI_DIR) : STREAM_ID_FLAG_IS_BI_DIR;
                    QUIC_STREAMS_BLOCKED_EX Frame = new QUIC_STREAMS_BLOCKED_EX()
                    {
                        BidirectionalStreams = true,
                        StreamLimit = Connection.Streams.Types[Mask].MaxTotalStreamCount
                    };

                    if (QuicStreamsBlockedFrameEncode(Frame, ref Builder.Datagram.Length, AvailableBufferLength, Builder.Datagram.Buffer))
                    {
                        Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_BIDI_STREAMS_BLOCKED;
                        if (QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_STREAMS_BLOCKED, true))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        RanOutOfRoom = true;
                    }
                }

                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_UNI_STREAMS_BLOCKED))
                {
                    ulong Mask = QuicConnIsServer(Connection) ? (1 | STREAM_ID_FLAG_IS_UNI_DIR) : STREAM_ID_FLAG_IS_UNI_DIR;
                    QUIC_STREAMS_BLOCKED_EX Frame = new QUIC_STREAMS_BLOCKED_EX()
                    {
                        BidirectionalStreams = false,
                        StreamLimit = Connection.Streams.Types[Mask].MaxTotalStreamCount
                    };

                    if (QuicStreamsBlockedFrameEncode(Frame, ref Builder.Datagram.Length, AvailableBufferLength, Builder.Datagram.Buffer))
                    {
                        Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_UNI_STREAMS_BLOCKED;
                        if (QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_STREAMS_BLOCKED_1, true))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        RanOutOfRoom = true;
                    }
                }

                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_MAX_STREAMS_UNI))
                {
                    QUIC_MAX_STREAMS_EX Frame = new QUIC_MAX_STREAMS_EX()
                    {
                        BidirectionalStreams = false
                    };

                    Frame.MaximumStreams = QuicConnIsServer(Connection) ?
                            Connection.Streams.Types[STREAM_ID_FLAG_IS_CLIENT | STREAM_ID_FLAG_IS_UNI_DIR].MaxTotalStreamCount :
                            Connection.Streams.Types[STREAM_ID_FLAG_IS_SERVER | STREAM_ID_FLAG_IS_UNI_DIR].MaxTotalStreamCount;

                    if (QuicMaxStreamsFrameEncode(Frame, ref Builder.Datagram.Length, AvailableBufferLength, Builder.Datagram.Buffer))
                    {
                        Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_MAX_STREAMS_UNI;
                        if (QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAMS_1, true))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        RanOutOfRoom = true;
                    }
                }

                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_NEW_CONNECTION_ID))
                {
                    bool HasMoreCidsToSend = false;
                    bool MaxFrameLimitHit = false;
                    for (CXPLAT_LIST_ENTRY Entry = Connection.SourceCids.Next; Entry != null; Entry = Entry.Next)
                    {
                        QUIC_CID SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                        if (!SourceCid.NeedsToSend)
                        {
                            continue;
                        }
                        if (MaxFrameLimitHit)
                        {
                            HasMoreCidsToSend = true;
                            break;
                        }

                        QUIC_NEW_CONNECTION_ID_EX Frame = new QUIC_NEW_CONNECTION_ID_EX()
                        {
                            Sequence = SourceCid.SequenceNumber,
                            RetirePriorTo = 0,
                        };
                        NetLog.Assert(Connection.SourceCidLimit >= QUIC_TP_ACTIVE_CONNECTION_ID_LIMIT_MIN);
                        if (Frame.Sequence >= Connection.SourceCidLimit)
                        {
                            Frame.RetirePriorTo = Frame.Sequence + 1 - Connection.SourceCidLimit;
                        }
                        SourceCid.Data.CopyTo(Frame.Buffer);

                        NetLog.Assert(SourceCid.Data.Length == MsQuicLib.CidTotalLength);
                        QUIC_SSBuffer mBuf = Frame.Buffer;
                        QuicLibraryGenerateStatelessResetToken(SourceCid.Data, mBuf + SourceCid.Data.Length);

                        QUIC_SSBuffer Datagram = Builder.Datagram.Slice(0, AvailableBufferLength);
                        if (QuicNewConnectionIDFrameEncode(Frame, ref Datagram))
                        {
                            SourceCid.NeedsToSend = false;
                            Builder.Metadata.Frames[Builder.Metadata.FrameCount].NEW_CONNECTION_ID.Sequence = SourceCid.SequenceNumber;
                            MaxFrameLimitHit = QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_NEW_CONNECTION_ID, true);
                        }
                        else
                        {
                            RanOutOfRoom = true;
                            HasMoreCidsToSend = true;
                            break;
                        }
                    }
                    if (!HasMoreCidsToSend)
                    {
                        Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_NEW_CONNECTION_ID;
                    }
                    if (MaxFrameLimitHit)
                    {
                        return true;
                    }
                }

                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_RETIRE_CONNECTION_ID))
                {
                    bool HasMoreCidsToSend = false;
                    bool MaxFrameLimitHit = false;
                    for (CXPLAT_LIST_ENTRY Entry = Connection.DestCids.Next; Entry != Connection.DestCids; Entry = Entry.Next)
                    {
                        QUIC_CID DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                        if (!DestCid.NeedsToSend)
                        {
                            continue;
                        }
                        NetLog.Assert(DestCid.Retired);
                        if (MaxFrameLimitHit)
                        {
                            HasMoreCidsToSend = true;
                            break;
                        }

                        QUIC_RETIRE_CONNECTION_ID_EX Frame = new QUIC_RETIRE_CONNECTION_ID_EX()
                        {
                            Sequence = DestCid.SequenceNumber
                        };

                        if (QuicRetireConnectionIDFrameEncode(Frame, ref Builder.Datagram.Length, AvailableBufferLength, Builder.Datagram.Buffer))
                        {
                            DestCid.NeedsToSend = false;
                            Builder.Metadata.Frames[Builder.Metadata.FrameCount].RETIRE_CONNECTION_ID.Sequence = DestCid.SequenceNumber;
                            MaxFrameLimitHit = QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_RETIRE_CONNECTION_ID, true);
                        }
                        else
                        {
                            RanOutOfRoom = true;
                            HasMoreCidsToSend = true;
                            break;
                        }
                    }
                    if (!HasMoreCidsToSend)
                    {
                        Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_RETIRE_CONNECTION_ID;
                    }
                    if (MaxFrameLimitHit)
                    {
                        return true;
                    }
                }

                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_ACK_FREQUENCY))
                {

                    QUIC_ACK_FREQUENCY_EX Frame = new QUIC_ACK_FREQUENCY_EX();
                    Frame.SequenceNumber = Connection.SendAckFreqSeqNum;
                    Frame.PacketTolerance = (ulong)Connection.PeerPacketTolerance;
                    Frame.UpdateMaxAckDelay = QuicConnGetAckDelay(Connection);
                    Frame.IgnoreOrder = false;
                    Frame.IgnoreCE = false;

                    if (QuicAckFrequencyFrameEncode(Frame, ref Builder.Datagram.Length, AvailableBufferLength, Builder.Datagram.Buffer))
                    {
                        Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_ACK_FREQUENCY;
                        Builder.Metadata.Frames[Builder.Metadata.FrameCount].ACK_FREQUENCY.Sequence = Frame.SequenceNumber;
                        if (QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_ACK_FREQUENCY, true))

                        {
                            return true;
                        }
                    }
                    else
                    {
                        RanOutOfRoom = true;
                    }
                }

                if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_DATAGRAM))
                {
                    RanOutOfRoom = QuicDatagramWriteFrame(Connection.Datagram, Builder);
                    if (Builder.Metadata.FrameCount == QUIC_MAX_FRAMES_PER_PACKET)
                    {
                        return true;
                    }
                }
            }

            if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_PING))
            {

                if (Builder.Datagram.Length < AvailableBufferLength)
                {
                    Builder.Datagram.Buffer[Builder.Datagram.Length++] = (byte)QUIC_FRAME_TYPE.QUIC_FRAME_PING;
                    Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_PING;
                    if (Connection.KeepAlivePadding > 0)
                    {
                        Builder.MinimumDatagramLength =
                            Builder.Datagram.Length + Connection.KeepAlivePadding + Builder.EncryptionOverhead;
                        if (Builder.MinimumDatagramLength > Builder.Datagram.Length)
                        {
                            Builder.MinimumDatagramLength = Builder.Datagram.Length;
                        }
                    }
                    else
                    {
                        Builder.MinimumDatagramLength = Builder.Datagram.Length;
                    }
                    if (QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_PING, true))
                    {
                        return true;
                    }
                }
                else
                {
                    RanOutOfRoom = true;
                }
            }

        Exit:
            NetLog.Assert(Builder.Metadata.FrameCount > PrevFrameCount || RanOutOfRoom);
            return Builder.Metadata.FrameCount > PrevFrameCount;
        }

        static bool HasStreamControlFrames(uint Flags)
        {
            return BoolOk(Flags &
                (QUIC_STREAM_SEND_FLAG_DATA_BLOCKED |
                 QUIC_STREAM_SEND_FLAG_MAX_DATA |
                 QUIC_STREAM_SEND_FLAG_SEND_ABORT |
                 QUIC_STREAM_SEND_FLAG_RECV_ABORT |
                 QUIC_STREAM_SEND_FLAG_RELIABLE_ABORT));
        }

        static bool HasStreamDataFrames(uint Flags)
        {
            return BoolOk(Flags &
                (QUIC_STREAM_SEND_FLAG_DATA |
                 QUIC_STREAM_SEND_FLAG_OPEN |
                 QUIC_STREAM_SEND_FLAG_FIN));
        }

        static void QuicSendClear(QUIC_SEND Send)
        {
            Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_CONN_CLOSED_MASK;
            while (!CxPlatListIsEmpty(Send.SendStreams))
            {
                QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(CxPlatListRemoveHead(Send.SendStreams));
                NetLog.Assert(Stream.SendFlags != 0);
                Stream.SendFlags = 0;
                Stream.SendLink.Next = null;
                QuicStreamRelease(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_SEND);
            }
        }

        static void QuicSendReset(QUIC_SEND Send)
        {
            Send.SendFlags = 0;
            Send.LastFlushTime = 0;
            if (Send.DelayedAckTimerActive)
            {
                QuicConnTimerCancel(QuicSendGetConnection(Send), QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_ACK_DELAY);
                Send.DelayedAckTimerActive = false;
            }
            QuicConnTimerCancel(QuicSendGetConnection(Send), QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_PACING);
        }

        static QUIC_PACKET_KEY_TYPE QuicPacketTypeToKeyTypeV1(QUIC_LONG_HEADER_TYPE_V1 PacketType)
        {
            switch (PacketType)
            {
                case QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1:
                case QUIC_LONG_HEADER_TYPE_V1.QUIC_RETRY_V1: 
                    return QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL;
                case QUIC_LONG_HEADER_TYPE_V1.QUIC_HANDSHAKE_V1: 
                    return QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE;
                case QUIC_LONG_HEADER_TYPE_V1.QUIC_0_RTT_PROTECTED_V1: 
                    return QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT;
                default: 
                    return QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT;
            }
        }

        static QUIC_PACKET_KEY_TYPE QuicPacketTypeToKeyTypeV2(QUIC_LONG_HEADER_TYPE_V2 PacketType)
        {
            switch (PacketType)
            {
                case QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2:
                case QUIC_LONG_HEADER_TYPE_V2.QUIC_RETRY_V2: 
                    return QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL;
                case QUIC_LONG_HEADER_TYPE_V2.QUIC_HANDSHAKE_V2: 
                    return QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE;
                case QUIC_LONG_HEADER_TYPE_V2.QUIC_0_RTT_PROTECTED_V2: 
                    return QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT;
                default: 
                    return QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT;
            }
        }

        static QUIC_ENCRYPT_LEVEL QuicPacketTypeToEncryptLevelV2(QUIC_LONG_HEADER_TYPE_V2 PacketType)
        {
            switch (PacketType)
            {
                case QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2: return  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_INITIAL;
                case QUIC_LONG_HEADER_TYPE_V2.QUIC_HANDSHAKE_V2: return  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_HANDSHAKE;
                default: return  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT;
            }
        }

        static QUIC_ENCRYPT_LEVEL QuicPacketTypeToEncryptLevelV1(QUIC_LONG_HEADER_TYPE_V1 PacketType)
        {
            switch (PacketType)
            {
                case  QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1: return  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_INITIAL;
                case  QUIC_LONG_HEADER_TYPE_V1.QUIC_HANDSHAKE_V1: return  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_HANDSHAKE;
                default: return  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT;
            }
        }


        static void QuicSendStartDelayedAckTimer(QUIC_SEND Send)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);

            NetLog.Assert(Connection.Settings.MaxAckDelayMs != 0);
            if (!Send.DelayedAckTimerActive && !BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_ACK) &&
                !Connection.State.ClosedLocally &&
                !Connection.State.ClosedRemotely)
            {
                QuicConnTimerSet(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_ACK_DELAY, Connection.Settings.MaxAckDelayMs);
                Send.DelayedAckTimerActive = true;
            }
        }

        static void QuicSendApplyNewSettings(QUIC_SEND Send, QUIC_SETTINGS Settings)
        {
            Send.MaxData = Settings.ConnFlowControlWindow;
        }

        static QUIC_STREAM QuicSendGetNextStream(QUIC_SEND Send, int PacketCount)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            NetLog.Assert(!QuicConnIsClosed(Connection) || CxPlatListIsEmpty(Send.SendStreams));

            CXPLAT_LIST_ENTRY Entry = Send.SendStreams.Next;
            while (Entry != Send.SendStreams)
            {
                QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(Entry);
                if (QuicSendCanSendStreamNow(Stream))
                {
                    if (Connection.State.UseRoundRobinStreamScheduling)
                    {
                        CXPLAT_LIST_ENTRY LastEntry = Stream.SendLink.Next;
                        while (LastEntry != Send.SendStreams)
                        {
                            if (Stream.SendPriority > CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(LastEntry).SendPriority)
                            {
                                break;
                            }
                            LastEntry = LastEntry.Next;
                        }
                        if (LastEntry.Prev != Stream.SendLink)
                        {
                            CxPlatListEntryRemove(Stream.SendLink);
                            CxPlatListInsertTail(LastEntry, Stream.SendLink);
                        }

                        PacketCount = QUIC_STREAM_SEND_BATCH_COUNT;
                    }
                    else
                    {
                        PacketCount = int.MaxValue;
                    }

                    return Stream;
                }

                Entry = Entry.Next;
            }

            return null;
        }

        static bool QuicSendCanSendStreamNow(QUIC_STREAM Stream)
        {
            NetLog.Assert(Stream.SendFlags != 0);
            QUIC_CONNECTION Connection = Stream.Connection;
            if (Connection.Crypto.TlsState.WriteKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                return QuicStreamCanSendNow(Stream, false);
            }

            if (Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT] != null)
            {
                return QuicStreamCanSendNow(Stream, true);
            }

            return false;
        }


    }
}
