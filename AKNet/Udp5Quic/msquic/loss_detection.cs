using System.IO;
using System;
using AKNet.Common;
using static System.Net.WebRequestMethods;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_LOSS_DETECTION
    {
        public QUIC_CONNECTION mConnection;
        public int PacketsInFlight;
        public ulong LargestAck;
        public QUIC_ENCRYPT_LEVEL LargestAckEncryptLevel;
        public long TimeOfLastPacketSent;
        public long TimeOfLastPacketAcked;
        public long TimeOfLastAckedPacketSent;
        public long AdjustedLastAckedTime;
        public ulong TotalBytesSent;
        public ulong TotalBytesAcked;
        public ulong TotalBytesSentAtLastAck;
        public ulong LargestSentPacketNumber;
        public QUIC_SENT_PACKET_METADATA SentPackets;
        public QUIC_SENT_PACKET_METADATA SentPacketsTail;
        public QUIC_SENT_PACKET_METADATA LostPackets;
        public QUIC_SENT_PACKET_METADATA LostPacketsTail;
        public ushort ProbeCount;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicLossDetectionInitialize(QUIC_LOSS_DETECTION LossDetection)
        {
            LossDetection.SentPackets = null;
            LossDetection.SentPacketsTail = LossDetection.SentPackets;
            LossDetection.LostPackets = null;
            LossDetection.LostPacketsTail = LossDetection.LostPackets;
            QuicLossDetectionInitializeInternalState(LossDetection);
        }

        static void QuicLossDetectionInitializeInternalState(QUIC_LOSS_DETECTION LossDetection)
        {
            LossDetection.PacketsInFlight = 0;
            LossDetection.TimeOfLastPacketSent = 0;
            LossDetection.TotalBytesSent = 0;
            LossDetection.TotalBytesAcked = 0;
            LossDetection.TotalBytesSentAtLastAck = 0;
            LossDetection.TimeOfLastPacketAcked = 0;
            LossDetection.TimeOfLastAckedPacketSent = 0;
            LossDetection.AdjustedLastAckedTime = 0;
            LossDetection.ProbeCount = 0;
        }

        static long QuicLossDetectionComputeProbeTimeout(QUIC_LOSS_DETECTION LossDetection, QUIC_PATH Path, int Count)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            NetLog.Assert(Path.SmoothedRtt != 0);

            long Pto = Path.SmoothedRtt + 4 * Path.RttVariance + Connection.PeerTransportParams.MaxAckDelay;
            Pto *= Count;
            return Pto;
        }

        static void QuicLossDetectionDiscardPackets(QUIC_LOSS_DETECTION LossDetection, QUIC_PACKET_KEY_TYPE KeyType)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel(KeyType);
            QUIC_SENT_PACKET_METADATA PrevPacket;
            QUIC_SENT_PACKET_METADATA Packet;
            int AckedRetransmittableBytes = 0;
            long TimeNow = CxPlatTime();

            NetLog.Assert(KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL || KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE);

            PrevPacket = null;
            Packet = LossDetection.LostPackets;
            while (Packet != null)
            {
                QUIC_SENT_PACKET_METADATA NextPacket = Packet.Next;

                if (Packet.Flags.KeyType == KeyType)
                {
                    if (PrevPacket != null)
                    {
                        PrevPacket.Next = NextPacket;
                        if (NextPacket == null)
                        {
                            LossDetection.LostPacketsTail = PrevPacket.Next;
                        }
                    }
                    else
                    {
                        LossDetection.LostPackets = NextPacket;
                        if (NextPacket == null)
                        {
                            LossDetection.LostPacketsTail = LossDetection.LostPackets;
                        }
                    }

                    QuicLossDetectionOnPacketAcknowledged(LossDetection, EncryptLevel, Packet, true, TimeNow, 0);
                    QuicSentPacketPoolReturnPacketMetadata(Packet, Connection);
                    Packet = NextPacket;
                }
                else
                {
                    PrevPacket = Packet;
                    Packet = NextPacket;
                }
            }

            QuicLossValidate(LossDetection);

            PrevPacket = NULL;
            Packet = LossDetection->SentPackets;
            while (Packet != NULL)
            {
                QUIC_SENT_PACKET_METADATA* NextPacket = Packet->Next;

                if (Packet->Flags.KeyType == KeyType)
                {
                    if (PrevPacket != NULL)
                    {
                        PrevPacket->Next = NextPacket;
                        if (NextPacket == NULL)
                        {
                            LossDetection->SentPacketsTail = &PrevPacket->Next;
                        }
                    }
                    else
                    {
                        LossDetection->SentPackets = NextPacket;
                        if (NextPacket == NULL)
                        {
                            LossDetection->SentPacketsTail = &LossDetection->SentPackets;
                        }
                    }

                    QuicTraceLogVerbose(
                        PacketTxAckedImplicit,
                        "[%c][TX][%llu] ACKed (implicit)",
                        PtkConnPre(Connection),
                        Packet->PacketNumber);
                    QuicTraceEvent(
                        ConnPacketACKed,
                        "[conn][%p][TX][%llu] %hhu ACKed",
                        Connection,
                        Packet->PacketNumber,
                        QuicPacketTraceType(Packet));

                    if (Packet->Flags.IsAckEliciting)
                    {
                        LossDetection->PacketsInFlight--;
                        AckedRetransmittableBytes += Packet->PacketLength;
                    }

                    QuicLossDetectionOnPacketAcknowledged(LossDetection, EncryptLevel, Packet, TRUE, TimeNow, 0);

                    QuicSentPacketPoolReturnPacketMetadata(Packet, Connection);

                    Packet = NextPacket;

                }
                else
                {
                    PrevPacket = Packet;
                    Packet = NextPacket;
                }
            }

            QuicLossValidate(LossDetection);

            if (AckedRetransmittableBytes > 0)
            {
                const QUIC_PATH* Path = &Connection->Paths[0]; // TODO - Correct?

                QUIC_ACK_EVENT AckEvent = {
                    .IsImplicit = TRUE,
                    .TimeNow = TimeNow,
                    .LargestAck = LossDetection->LargestAck,
                    .LargestSentPacketNumber = LossDetection->LargestSentPacketNumber,
                    .NumRetransmittableBytes = AckedRetransmittableBytes,
                    .SmoothedRtt = Path->SmoothedRtt,
                    .MinRtt = 0,
                    .OneWayDelay = Path->OneWayDelay,
                    .HasLoss = FALSE,
                    .AdjustedAckTime = 0,
                    .AckedPackets = NULL,
                    .NumTotalAckedRetransmittableBytes = 0,
                    .IsLargestAckedPacketAppLimited = FALSE,
                    .MinRttValid = FALSE
                };

                if (QuicCongestionControlOnDataAcknowledged(&Connection->CongestionControl, &AckEvent))
                {
                    //
                    // We were previously blocked and are now unblocked.
                    //
                    QuicSendQueueFlush(&Connection->Send, REASON_CONGESTION_CONTROL);
                }
            }
        }

        static void QuicLossDetectionOnPacketAcknowledged(QUIC_LOSS_DETECTION LossDetection,QUIC_ENCRYPT_LEVEL EncryptLevel, QUIC_SENT_PACKET_METADATA Packet,
            bool IsImplicit, long AckTime, long AckDelay)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            int PathIndex = -1;
            QUIC_PATH Path = QuicConnGetPathByID(Connection, Packet.PathId,ref PathIndex);

            NetLog.Assert(EncryptLevel >=  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_INITIAL && EncryptLevel <  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_COUNT);

            if (QuicConnIsClient(Connection) && !Connection.State.HandshakeConfirmed && Packet.Flags.KeyType ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                QuicCryptoHandshakeConfirmed(Connection.Crypto, true);
            }

            QUIC_PACKET_SPACE PacketSpace = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT];
            if (EncryptLevel == QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT &&
                PacketSpace.AwaitingKeyPhaseConfirmation && Packet.Flags.KeyPhase == PacketSpace.CurrentKeyPhase &&
                Packet.PacketNumber >= PacketSpace.WriteKeyPhaseStartPacketNumber)
            {
                PacketSpace.AwaitingKeyPhaseConfirmation = false;
            }

            for (int i = 0; i < Packet.FrameCount; i++)
            {
                switch (Packet.Frames[i].Type)
                {
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_ACK:
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_ACK_1:
                        QuicAckTrackerOnAckFrameAcked(Connection.Packets[EncryptLevel].AckTracker, Packet.Frames[i].ACK.LargestAckedPacketNumber);
                        break;

                    case QUIC_FRAME_RESET_STREAM:
                        QuicStreamOnResetAck(Packet->Frames[i].RESET_STREAM.Stream);
                        break;

                    case QUIC_FRAME_RELIABLE_RESET_STREAM:
                        QuicStreamOnResetReliableAck(Packet->Frames[i].RELIABLE_RESET_STREAM.Stream);
                        break;

                    case QUIC_FRAME_CRYPTO:
                        QuicCryptoOnAck(&Connection->Crypto, &Packet->Frames[i]);
                        break;

                    case QUIC_FRAME_STREAM:
                    case QUIC_FRAME_STREAM_1:
                    case QUIC_FRAME_STREAM_2:
                    case QUIC_FRAME_STREAM_3:
                    case QUIC_FRAME_STREAM_4:
                    case QUIC_FRAME_STREAM_5:
                    case QUIC_FRAME_STREAM_6:
                    case QUIC_FRAME_STREAM_7:
                        QuicStreamOnAck(
                            Packet->Frames[i].STREAM.Stream,
                            Packet->Flags,
                            &Packet->Frames[i]);
                        break;

                    case QUIC_FRAME_STREAM_DATA_BLOCKED:
                        if (Packet->Frames[i].STREAM_DATA_BLOCKED.Stream->OutFlowBlockedReasons &
                            QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL)
                        {
                            //
                            // Stream is still blocked, so queue the blocked frame up again.
                            //
                            // N.B. If this design of immediate resending after ACK ever
                            // gets too chatty, then we can reuse the existing loss
                            // detection timer to add exponential backoff.
                            //
                            QuicSendSetStreamSendFlag(
                                &Connection->Send,
                                Packet->Frames[i].STREAM_DATA_BLOCKED.Stream,
                                QUIC_STREAM_SEND_FLAG_DATA_BLOCKED,
                                FALSE);
                        }
                        break;

                    case QUIC_FRAME_NEW_CONNECTION_ID:
                        {
                            BOOLEAN IsLastCid;
                            QUIC_CID_HASH_ENTRY* SourceCid =
                                QuicConnGetSourceCidFromSeq(
                                    Connection,
                                    Packet->Frames[i].NEW_CONNECTION_ID.Sequence,
                                    FALSE,
                                    &IsLastCid);
                            if (SourceCid != NULL)
                            {
                                SourceCid->CID.Acknowledged = TRUE;
                            }
                            break;
                        }

                    case QUIC_FRAME_RETIRE_CONNECTION_ID:
                        {
                            QUIC_CID_LIST_ENTRY* DestCid =
                                QuicConnGetDestCidFromSeq(
                                    Connection,
                                    Packet->Frames[i].RETIRE_CONNECTION_ID.Sequence,
                                    TRUE);
                            if (DestCid != NULL)
                            {
#pragma prefast(suppress:6001, "TODO - Why does compiler think: Using uninitialized memory '*DestCid'")
                                CXPLAT_DBG_ASSERT(DestCid->CID.Retired);
                                CXPLAT_DBG_ASSERT(Path == NULL || Path->DestCid != DestCid);
                                QUIC_CID_VALIDATE_NULL(Connection, DestCid);
                                CXPLAT_DBG_ASSERT(Connection->RetiredDestCidCount > 0);
                                Connection->RetiredDestCidCount--;
                                CXPLAT_FREE(DestCid, QUIC_POOL_CIDLIST);
                            }
                            break;
                        }

                    case QUIC_FRAME_DATAGRAM:
                    case QUIC_FRAME_DATAGRAM_1:
                        QuicDatagramIndicateSendStateChange(
                            Connection,
                            &Packet->Frames[i].DATAGRAM.ClientContext,
                            Packet->Flags.SuspectedLost ?
                                QUIC_DATAGRAM_SEND_ACKNOWLEDGED_SPURIOUS :
                                QUIC_DATAGRAM_SEND_ACKNOWLEDGED);
                        Packet->Frames[i].DATAGRAM.ClientContext = NULL;
                        break;

                    case QUIC_FRAME_HANDSHAKE_DONE:
                        QuicCryptoHandshakeConfirmed(&Connection->Crypto, TRUE);
                        break;
                }
            }

            if (Path != NULL)
            {
                uint16_t PacketMtu =
                    PacketSizeFromUdpPayloadSize(
                        QuicAddrGetFamily(&Path->Route.RemoteAddress),
                        Packet->PacketLength);
                BOOLEAN ChangedMtu = FALSE;
                if (!Path->IsMinMtuValidated &&
                    PacketMtu >= Path->Mtu)
                {
                    Path->IsMinMtuValidated = TRUE;
                    ChangedMtu = PacketMtu > Path->Mtu;
                    QuicTraceLogConnInfo(
                        PathMinMtuValidated,
                        Connection,
                        "Path[%hhu] Minimum MTU validated",
                        Path->ID);
                }

                if (Packet->Flags.IsMtuProbe)
                {
                    CXPLAT_DBG_ASSERT(Path->IsMinMtuValidated);
                    if (QuicMtuDiscoveryOnAckedPacket(
                            &Path->MtuDiscovery,
                            PacketMtu,
                            Connection))
                    {
                        ChangedMtu = TRUE;
                    }
                }
                if (ChangedMtu)
                {
                    QuicDatagramOnSendStateChanged(&Connection->Datagram);
                }
            }

            if (!IsImplicit)
            {
                LossDetection->TotalBytesAcked += Packet->PacketLength;
                LossDetection->TotalBytesSentAtLastAck = Packet->TotalBytesSent;
                LossDetection->TimeOfLastPacketAcked = AckTime;
                LossDetection->TimeOfLastAckedPacketSent = Packet->SentTime;
                LossDetection->AdjustedLastAckedTime = AckTime - AckDelay;
            }
        }

        static void QuicLossDetectionOnPacketSent(QUIC_LOSS_DETECTION LossDetection,QUIC_PATH Path, QUIC_SENT_PACKET_METADATA TempSentPacket)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            NetLog.Assert(TempSentPacket.FrameCount != 0);
            
            QUIC_SENT_PACKET_METADATA SentPacket = QuicSentPacketPoolGetPacketMetadata(Connection.Worker.SentPacketPool, TempSentPacket.FrameCount);
            if (SentPacket == null)
            {
                QuicLossDetectionRetransmitFrames(LossDetection, TempSentPacket, false);
                QuicSentPacketMetadataReleaseFrames(TempSentPacket, Connection);
                return;
            }



            CxPlatCopyMemory(
                SentPacket,
                TempSentPacket,
                sizeof(QUIC_SENT_PACKET_METADATA) +
                sizeof(QUIC_SENT_FRAME_METADATA) * TempSentPacket->FrameCount);

            LossDetection.LargestSentPacketNumber = TempSentPacket.PacketNumber;
            SentPacket.Next = null;
            LossDetection.SentPacketsTail = SentPacket;
            LossDetection.SentPacketsTail = SentPacket.Next;

            NetLog.Assert(SentPacket.Flags.KeyType !=  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT || SentPacket.Flags.IsAckEliciting);

            Connection->Stats.Send.TotalPackets++;
            Connection->Stats.Send.TotalBytes += TempSentPacket->PacketLength;
            if (SentPacket->Flags.IsAckEliciting)
            {

                if (LossDetection->PacketsInFlight == 0)
                {
                    QuicConnResetIdleTimeout(Connection);
                }

                Connection->Stats.Send.RetransmittablePackets++;
                LossDetection->PacketsInFlight++;
                LossDetection->TimeOfLastPacketSent = SentPacket->SentTime;

                if (!Path->IsPeerValidated)
                {
                    QuicPathDecrementAllowance(
                        Connection, Path, SentPacket->PacketLength);
                }

                QuicCongestionControlOnDataSent(
                    &Connection->CongestionControl, SentPacket->PacketLength);
            }

            uint64_t SendPostedBytes = Connection->SendBuffer.PostedBytes;

            CXPLAT_LIST_ENTRY* Entry = Connection->Send.SendStreams.Flink;
            QUIC_STREAM* Stream =
                (Entry != &(Connection->Send.SendStreams)) ?
                  CXPLAT_CONTAINING_RECORD(Entry, QUIC_STREAM, SendLink) :
                  NULL;

            if (SendPostedBytes < Path->Mtu &&
                QuicCongestionControlCanSend(&Connection->CongestionControl) &&
                !QuicCryptoHasPendingCryptoFrame(&Connection->Crypto) &&
                (Stream && QuicStreamAllowedByPeer(Stream)) && !QuicStreamCanSendNow(Stream, FALSE))
            {
                QuicCongestionControlSetAppLimited(&Connection->CongestionControl);
            }

            SentPacket->Flags.IsAppLimited = QuicCongestionControlIsAppLimited(&Connection->CongestionControl);

            LossDetection->TotalBytesSent += TempSentPacket->PacketLength;

            SentPacket->TotalBytesSent = LossDetection->TotalBytesSent;

            SentPacket->Flags.HasLastAckedPacketInfo = FALSE;
            if (LossDetection->TimeOfLastPacketAcked)
            {
                SentPacket->Flags.HasLastAckedPacketInfo = TRUE;

                SentPacket->LastAckedPacketInfo.SentTime = LossDetection->TimeOfLastAckedPacketSent;
                SentPacket->LastAckedPacketInfo.AckTime = LossDetection->TimeOfLastPacketAcked;
                SentPacket->LastAckedPacketInfo.AdjustedAckTime = LossDetection->AdjustedLastAckedTime;
                SentPacket->LastAckedPacketInfo.TotalBytesSent = LossDetection->TotalBytesSentAtLastAck;
                SentPacket->LastAckedPacketInfo.TotalBytesAcked = LossDetection->TotalBytesAcked;
            }

            QuicLossValidate(LossDetection);
        }

        static bool QuicLossDetectionRetransmitFrames(QUIC_LOSS_DETECTION LossDetection, QUIC_SENT_PACKET_METADATA Packet, bool ReleasePacket)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            bool NewDataQueued = false;

            for (int i = 0; i < Packet.FrameCount; i++)
            {
                switch (Packet.Frames[i].Type)
                {
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_PING:
                        if (!Packet.Flags.IsMtuProbe)
                        {
                            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_PING);
                        }
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_RESET_STREAM:
                        NewDataQueued |= QuicSendSetStreamSendFlag(Connection.Send, Packet.Frames[i].RESET_STREAM.Stream, QUIC_STREAM_SEND_FLAG_SEND_ABORT, false);
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_RELIABLE_RESET_STREAM:
                        NewDataQueued |= QuicSendSetStreamSendFlag(Connection.Send, Packet.Frames[i].RELIABLE_RESET_STREAM.Stream, QUIC_STREAM_SEND_FLAG_RELIABLE_ABORT, false);
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_STOP_SENDING:
                        NewDataQueued |= QuicSendSetStreamSendFlag(Connection.Send, Packet.Frames[i].STOP_SENDING.Stream, QUIC_STREAM_SEND_FLAG_RECV_ABORT, false);
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_CRYPTO:
                        NewDataQueued |= QuicCryptoOnLoss(Connection.Crypto, Packet.Frames[i]);
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM:
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_1:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_2:
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_3:
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_4:
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_5:
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_6:
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_7:
                        NewDataQueued |= QuicStreamOnLoss(Packet.Frames[i].STREAM.Stream, Packet.Frames[i]);
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_MAX_DATA:
                        NewDataQueued |= QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_MAX_DATA);
                        break;
                    case QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAM_DATA:
                        NewDataQueued |= QuicSendSetStreamSendFlag(Connection.Send, Packet.Frames[i].MAX_STREAM_DATA.Stream, QUIC_STREAM_SEND_FLAG_MAX_DATA, false);
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAMS:
                        NewDataQueued |= QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_MAX_STREAMS_BIDI);
                        break;
                    case QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAMS_1:
                        NewDataQueued |= QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_MAX_STREAMS_UNI);
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_DATA_BLOCKED:
                        NewDataQueued |= QuicSendSetStreamSendFlag(Connection.Send, Packet.Frames[i].STREAM_DATA_BLOCKED.Stream, QUIC_STREAM_SEND_FLAG_DATA_BLOCKED, false);
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_NEW_CONNECTION_ID:
                        {
                            bool IsLastCid;
                            QUIC_CID_HASH_ENTRY SourceCid = QuicConnGetSourceCidFromSeq(Connection, Packet.Frames[i].NEW_CONNECTION_ID.Sequence, false, IsLastCid);
                            if (SourceCid != null && !SourceCid.CID.Acknowledged)
                            {
                                SourceCid.CID.NeedsToSend = true;
                                NewDataQueued |= QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_NEW_CONNECTION_ID);
                            }
                            break;
                        }

                    case  QUIC_FRAME_TYPE.QUIC_FRAME_RETIRE_CONNECTION_ID:
                        {
                            QUIC_CID_LIST_ENTRY DestCid = QuicConnGetDestCidFromSeq(Connection, Packet.Frames[i].RETIRE_CONNECTION_ID.Sequence, false);
                            if (DestCid != null)
                            {
                                NetLog.Assert(DestCid.CID.Retired);
                                DestCid.CID.NeedsToSend = true;
                                NewDataQueued |= QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_RETIRE_CONNECTION_ID);
                            }
                            break;
                        }

                    case  QUIC_FRAME_TYPE.QUIC_FRAME_PATH_CHALLENGE:
                        {
                            int PathIndex;
                            QUIC_PATH Path = QuicConnGetPathByID(Connection, Packet.PathId, ref PathIndex);
                            if (Path != null && !Path.IsPeerValidated)
                            {
                                long TimeNow = CxPlatTime();
                                NetLog.Assert(Connection.Configuration != null);
                                long ValidationTimeout = Math.Max(QuicLossDetectionComputeProbeTimeout(LossDetection, Path, 3), 6 * (Connection.Settings.InitialRttMs));
                                if (CxPlatTimeDiff64(Path.PathValidationStartTime, TimeNow) > ValidationTimeout)
                                {
                                    QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PATH_FAILURE);
                                    QuicPathRemove(Connection, PathIndex);
                                }
                                else
                                {
                                    Path.SendChallenge = true;
                                    QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_PATH_CHALLENGE);
                                }
                            }
                            break;
                        }

                    case  QUIC_FRAME_TYPE.QUIC_FRAME_HANDSHAKE_DONE:
                        NewDataQueued |= QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_HANDSHAKE_DONE);
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM:
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM_1:
                        if (!Packet.Flags.SuspectedLost)
                        {
                            QuicDatagramIndicateSendStateChange(Connection, Packet.Frames[i].DATAGRAM.ClientContext,  QUIC_DATAGRAM_SEND_LOST_SUSPECT);
                        }
                        break;
                    case  QUIC_FRAME_TYPE.QUIC_FRAME_ACK_FREQUENCY:
                        if (Packet.Frames[i].ACK_FREQUENCY.Sequence == Connection.SendAckFreqSeqNum)
                        {
                            NewDataQueued |= QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_ACK_FREQUENCY);
                        }
                        break;
                }
            }

            Packet.Flags.SuspectedLost = true;
            if (ReleasePacket)
            {
                QuicSentPacketPoolReturnPacketMetadata(Packet, Connection);
            }

            return NewDataQueued;
        }

    }
}
