using AKNet.Common;
using System;
using System.IO;
using static System.Net.WebRequestMethods;

namespace AKNet.Udp5Quic.Common
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
        public long NumPacketsSentWithEct;
        public long MaxData;
        public long PeerMaxData;
        public long OrderedStreamBytesReceived;
        public long OrderedStreamBytesSent;
        public long OrderedStreamBytesDeliveredAccumulator;
        public uint SendFlags;
        public CXPLAT_LIST_ENTRY SendStreams;
        public byte[] InitialToken;
        public ushort InitialTokenLength;

        public QUIC_CONNECTION mConnection;
    }

    internal enum QUIC_SEND_RESULT
    {
        QUIC_SEND_COMPLETE,
        QUIC_SEND_INCOMPLETE,
        QUIC_SEND_DELAYED_PACING
    }

    internal static partial class MSQuicFunc
    {
        static void QuicSendInitialize(QUIC_SEND Send, QUIC_SETTINGS_INTERNAL Settings)
        {
            CxPlatListInitializeHead(Send.SendStreams);
            Send.MaxData = Settings.ConnFlowControlWindow;
        }

        static bool QuicSendCanSendFlagsNow(QUIC_SEND Send)
        {
            QUIC_CONNECTION Connection = QuicSendGetConnection(Send);
            if (Connection.Crypto.TlsState.WriteKey < QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                if (Connection.Crypto.TlsState.WriteKeys[QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT] != null && CxPlatListIsEmpty(Send.SendStreams))
                {
                    return true;
                }

                if ((!Connection.State.Started && QuicConnIsClient(Connection)) || !(Send.SendFlags &  QUIC_CONN_SEND_FLAG_ALLOWED_HANDSHAKE))
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

            if (CanSetFlag && (Send->SendFlags & SendFlags) != SendFlags)
            {
                QuicTraceLogConnVerbose(
                    ScheduleSendFlags,
                    Connection,
                    "Scheduling flags 0x%x to 0x%x",
                    SendFlags,
                    Send->SendFlags);
                Send->SendFlags |= SendFlags;
                QuicSendQueueFlush(Send, REASON_CONNECTION_FLAGS);
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
            if (Stream.SendLink.Flink == null)
            {
                CXPLAT_LIST_ENTRY Entry = Send.SendStreams.Blink;
                while (Entry != Send.SendStreams)
                {
                    if (Stream.SendPriority <= ((CXPLAT_LIST_ENTRY_QUIC_STREAM)Entry).mContain.SendPriority)
                    {
                        break;
                    }
                    Entry = Entry.Blink;
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

                if (Stream.SendFlags == 0 && Stream.SendLink.Flink != null)
                {
                    CxPlatListEntryRemove(Stream.SendLink);
                    Stream.SendLink.Flink = null;
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
                if (Connection.Packets[i] != null && Connection.Packets[i].AckTracker.AckElicitingPacketsToAcknowledge)
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

            QUIC_SEND_RESULT Result =  QUIC_SEND_RESULT.QUIC_SEND_INCOMPLETE;
            QUIC_STREAM Stream = null;
            int StreamPacketCount = 0;

            if (BoolOk(Send.SendFlags & QUIC_CONN_SEND_FLAG_PATH_CHALLENGE))
            {
                Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_PATH_CHALLENGE;
                QuicSendPathChallenges(Send);
            }

            QUIC_PACKET_BUILDER Builder = { 0 };
            if (!QuicPacketBuilderInitialize(&Builder, Connection, Path))
            {
                //
                // If this fails, the connection is in a bad (likely partially
                // uninitialized) state, so just ignore the send flush call. This can
                // happen if a loss detection fires right after shutdown.
                //
                return TRUE;
            }
            _Analysis_assume_(Builder.Metadata != NULL);

            if (Builder.Path->EcnValidationState == ECN_VALIDATION_CAPABLE)
            {
                Builder.EcnEctSet = TRUE;
            }
            else if (Builder.Path->EcnValidationState == ECN_VALIDATION_TESTING)
            {
                if (Builder.Path->EcnTestingEndingTime != 0)
                {
                    if (!CxPlatTimeAtOrBefore64(TimeNow, Builder.Path->EcnTestingEndingTime))
                    {
                        Builder.Path->EcnValidationState = ECN_VALIDATION_UNKNOWN;
                        QuicTraceLogConnInfo(
                            EcnValidationUnknown,
                            Connection,
                            "ECN unknown.");
                    }
                }
                else
                {
                    uint64_t ThreePtosInUs =
                        QuicLossDetectionComputeProbeTimeout(
                            &Connection->LossDetection,
                            &Connection->Paths[0],
                            QUIC_CLOSE_PTO_COUNT);
                    Builder.Path->EcnTestingEndingTime = TimeNow + ThreePtosInUs;
                }
                Builder.EcnEctSet = TRUE;
            }

            QuicTraceEvent(
                ConnFlushSend,
                "[conn][%p] Flushing Send. Allowance=%u bytes",
                Connection,
                Builder.SendAllowance);

#if DEBUG
            uint32_t DeadlockDetection = 0;
            uint32_t PrevSendFlags = UINT32_MAX;        // N-1
            uint32_t PrevPrevSendFlags = UINT32_MAX;    // N-2
#endif

            do
            {

                if (Path->Allowance < QUIC_MIN_SEND_ALLOWANCE)
                {
                    QuicTraceLogConnVerbose(
                        AmplificationProtectionBlocked,
                        Connection,
                        "Cannot send any more because of amplification protection");
                    Result = QUIC_SEND_COMPLETE;
                    break;
                }

                uint32_t SendFlags = Send->SendFlags;
                if (Connection->Crypto.TlsState.WriteKey < QUIC_PACKET_KEY_1_RTT)
                {
                    SendFlags &= QUIC_CONN_SEND_FLAG_ALLOWED_HANDSHAKE;
                }
                if (Path->Allowance != UINT32_MAX)
                {
                    //
                    // Don't try to send datagrams until the peer's source address has
                    // been validated because they might not fit in the limited space.
                    //
                    SendFlags &= ~QUIC_CONN_SEND_FLAG_DATAGRAM;
                }

                if (!QuicPacketBuilderHasAllowance(&Builder))
                {
                    //
                    // While we are CC blocked, very few things are still allowed to
                    // be sent. If those are queued then we can still send.
                    //
                    SendFlags &= QUIC_CONN_SEND_FLAGS_BYPASS_CC;
                    if (!SendFlags)
                    {
                        if (QuicCongestionControlCanSend(&Connection->CongestionControl))
                        {
                            QuicConnAddOutFlowBlockedReason(
                                Connection, QUIC_FLOW_BLOCKED_PACING);
                            QuicConnTimerSet(
                                Connection,
                                QUIC_CONN_TIMER_PACING,
                                QUIC_SEND_PACING_INTERVAL);
                            Result = QUIC_SEND_DELAYED_PACING;
                        }
                        else
                        {
                            //
                            // No pure ACKs to send right now. All done sending for now.
                            //
                            Result = QUIC_SEND_COMPLETE;
                        }
                        break;
                    }
                }

                //
                // We write data to packets in the following order:
                //
                //   1. Connection wide control data.
                //   2. Path MTU discovery packets.
                //   3. Stream (control and application) data.
                //

                BOOLEAN WrotePacketFrames;
                BOOLEAN FlushBatchedDatagrams = FALSE;
                if ((SendFlags & ~QUIC_CONN_SEND_FLAG_DPLPMTUD) != 0)
                {
                    CXPLAT_DBG_ASSERT(QuicSendCanSendFlagsNow(Send));
                    if (!QuicPacketBuilderPrepareForControlFrames(
                            &Builder,
                            Send->TailLossProbeNeeded,
                            SendFlags & ~QUIC_CONN_SEND_FLAG_DPLPMTUD))
                    {
                        break;
                    }
                    WrotePacketFrames = QuicSendWriteFrames(Send, &Builder);
                }
                else if ((SendFlags & QUIC_CONN_SEND_FLAG_DPLPMTUD) != 0)
                {
                    if (!QuicPacketBuilderPrepareForPathMtuDiscovery(&Builder))
                    {
                        break;
                    }
                    FlushBatchedDatagrams = TRUE;
                    Send->SendFlags &= ~QUIC_CONN_SEND_FLAG_DPLPMTUD;
                    if (Builder.Metadata->FrameCount < QUIC_MAX_FRAMES_PER_PACKET &&
                        Builder.DatagramLength < Builder.Datagram->Length - Builder.EncryptionOverhead)
                    {
                        //
                        // We are doing DPLPMTUD, so make sure there is a PING frame in there, if
                        // we have room, just to make sure we get an ACK.
                        //
                        Builder.Datagram->Buffer[Builder.DatagramLength++] = QUIC_FRAME_PING;
                        Builder.Metadata->Frames[Builder.Metadata->FrameCount++].Type = QUIC_FRAME_PING;
                        WrotePacketFrames = TRUE;
                    }
                    else
                    {
                        WrotePacketFrames = FALSE;
                    }
                }
                else if (Stream != NULL ||
                    (Stream = QuicSendGetNextStream(Send, &StreamPacketCount)) != NULL)
                {
                    if (!QuicPacketBuilderPrepareForStreamFrames(
                            &Builder,
                            Send->TailLossProbeNeeded))
                    {
                        break;
                    }

                    //
                    // Write any ACK frames if we have them.
                    //
                    QUIC_PACKET_SPACE* Packets = Connection->Packets[Builder.EncryptLevel];
                    uint8_t ZeroRttPacketType =
                        Connection->Stats.QuicVersion == QUIC_VERSION_2 ?
                            QUIC_0_RTT_PROTECTED_V2 : QUIC_0_RTT_PROTECTED_V1;
                    WrotePacketFrames =
                        Builder.PacketType != ZeroRttPacketType &&
                        QuicAckTrackerHasPacketsToAck(&Packets->AckTracker) &&
                        QuicAckTrackerAckFrameEncode(&Packets->AckTracker, &Builder);

                    //
                    // Write the stream frames.
                    //
                    WrotePacketFrames |= QuicStreamSendWrite(Stream, &Builder);

                    if (Stream->SendFlags == 0 && Stream->SendLink.Flink != NULL)
                    {
                        //
                        // If the stream no longer has anything to send, remove it from the
                        // list and release Send's reference on it.
                        //
                        CxPlatListEntryRemove(&Stream->SendLink);
                        Stream->SendLink.Flink = NULL;
                        QuicStreamRelease(Stream, QUIC_STREAM_REF_SEND);
                        Stream = NULL;

                    }
                    else if ((WrotePacketFrames && --StreamPacketCount == 0) ||
                        !QuicSendCanSendStreamNow(Stream))
                    {
                        //
                        // Try a new stream next loop iteration.
                        //
                        Stream = NULL;
                    }

                }
                else
                {
                    //
                    // Nothing else left to send right now.
                    //
                    Result = QUIC_SEND_COMPLETE;
                    break;
                }

                Send->TailLossProbeNeeded = FALSE;

                if (!WrotePacketFrames ||
                    Builder.Metadata->FrameCount == QUIC_MAX_FRAMES_PER_PACKET ||
                    Builder.Datagram->Length - Builder.DatagramLength < QUIC_MIN_PACKET_SPARE_SPACE)
                {

                    //
                    // We now have enough data in the current packet that we should
                    // finalize it.
                    //
                    if (!QuicPacketBuilderFinalize(&Builder, !WrotePacketFrames || FlushBatchedDatagrams))
                    {
                        //
                        // Don't have any more space to send.
                        //
                        break;
                    }
                }

#if DEBUG
                CXPLAT_DBG_ASSERT(++DeadlockDetection < 1000);
                UNREFERENCED_PARAMETER(PrevPrevSendFlags); // Used in debugging only
                PrevPrevSendFlags = PrevSendFlags;
                PrevSendFlags = SendFlags;
#endif

            } while (Builder.SendData != NULL ||
                Builder.TotalCountDatagrams < QUIC_MAX_DATAGRAMS_PER_SEND);

            if (Builder.SendData != NULL)
            {
                //
                // Final send, if there is anything left over.
                //
                QuicPacketBuilderFinalize(&Builder, TRUE);
                CXPLAT_DBG_ASSERT(Builder.SendData == NULL);
            }

            QuicPacketBuilderCleanup(&Builder);
            if (Result == QUIC_SEND_INCOMPLETE)
            {
                QuicConnAddOutFlowBlockedReason(Connection, QUIC_FLOW_BLOCKED_SCHEDULING);
                QuicSendQueueFlush(&Connection->Send, REASON_SCHEDULING);
                if (Builder.TotalCountDatagrams + 1 > Connection->PeerPacketTolerance)
                {
                    QuicConnUpdatePeerPacketTolerance(Connection, Builder.TotalCountDatagrams + 1);
                }
            }

            return Result != QUIC_SEND_INCOMPLETE;
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

                QUIC_PACKET_BUILDER Builder = { 0 };
                if (!QuicPacketBuilderInitialize(&Builder, Connection, Path))
                {
                    continue;
                }
                _Analysis_assume_(Builder.Metadata != NULL);

                if (!QuicPacketBuilderPrepareForControlFrames(
                        &Builder, FALSE, QUIC_CONN_SEND_FLAG_PATH_CHALLENGE))
                {
                    continue;
                }

                if (!Path->IsMinMtuValidated)
                {
                    //
                    // Path challenges need to be padded to at least the same as initial
                    // packets to validate min MTU.
                    //
                    Builder.MinimumDatagramLength =
                        MaxUdpPayloadSizeForFamily(
                            QuicAddrGetFamily(&Builder.Path->Route.RemoteAddress),
                            Builder.Path->Mtu);

                    if ((uint32_t)Builder.MinimumDatagramLength > Builder.Datagram->Length)
                    {
                        //
                        // If we're limited by amplification protection, just pad up to
                        // that limit instead.
                        //
                        Builder.MinimumDatagramLength = (uint16_t)Builder.Datagram->Length;
                    }
                }

                uint16_t AvailableBufferLength =
                    (uint16_t)Builder.Datagram->Length - Builder.EncryptionOverhead;

                QUIC_PATH_CHALLENGE_EX Frame;
                CxPlatCopyMemory(Frame.Data, Path->Challenge, sizeof(Frame.Data));

                BOOLEAN Result =
                    QuicPathChallengeFrameEncode(
                        QUIC_FRAME_PATH_CHALLENGE,
                        &Frame,
                        &Builder.DatagramLength,
                        AvailableBufferLength,
                        Builder.Datagram->Buffer);

                CXPLAT_DBG_ASSERT(Result);
                if (Result)
                {
                    CxPlatCopyMemory(
                        Builder.Metadata->Frames[0].PATH_CHALLENGE.Data,
                        Frame.Data,
                        sizeof(Frame.Data));

                    Result = QuicPacketBuilderAddFrame(&Builder, QUIC_FRAME_PATH_CHALLENGE, TRUE);
                    CXPLAT_DBG_ASSERT(!Result);
                    UNREFERENCED_PARAMETER(Result);

                    Path->SendChallenge = FALSE;
                }

                QuicPacketBuilderFinalize(&Builder, TRUE);
                QuicPacketBuilderCleanup(&Builder);
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
                    if (Connection.Packets[i].AckTracker.AckElicitingPacketsToAcknowledge)
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
                if (Connection.Packets[i] != null && Connection.Packets[i].AckTracker.AckElicitingPacketsToAcknowledge)
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

    }
}
