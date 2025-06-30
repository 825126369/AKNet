using AKNet.Common;
using System;

namespace AKNet.Udp5MSQuic.Common
{
    internal enum QUIC_LOSS_TIMER_TYPE
    {
        //含义：初始重传定时器。
        //用途：用于在连接建立初期（如发送 Initial 包或 Handshake 包）时，等待对端的响应
        LOSS_TIMER_INITIAL,
        //含义：基于 RACK-TLP 算法的定时器。
        //用途：根据最近一次收到的 ACK 时间来判断是否某些数据包可能已经丢失。
        //特点：
        //使用 RACK（Recent ACKnowledgment）算法来检测丢包。
        //不依赖于 RTT 测量次数，适用于乱序网络环境。
        //比传统的基于 RTT 的定时器更灵敏、更准确。
        //当某个数据包在其“预期时间内”未被确认，则可能触发重传。
        LOSS_TIMER_RACK,
        //用于主动发送探测包（probe packet），以推动协议状态前进，尤其是在无足够 ACK 反馈时。
        LOSS_TIMER_PROBE
    }

    internal class QUIC_LOSS_DETECTION
    {
        public QUIC_CONNECTION mConnection;
        public int PacketsInFlight; //当前在网络中飞行中的（即已发送但尚未被确认）可重传数据包数量。
        public ulong LargestAck;//接收到的最大确认号（packet number），即对端最近一次 ACK 中最大的 packet number。
        public QUIC_ENCRYPT_LEVEL LargestAckEncryptLevel; //收到最大 ACK 所属的加密级别（如 Initial、Handshake、0-RTT、1-RTT 等）。
        public long TimeOfLastPacketSent;//最后一个发送数据包的时间戳。
        public long TimeOfLastPacketAcked;//最后一个被确认的数据包的接收时间（即本地收到 ACK 的时间）。
        public long TimeOfLastAckedPacketSent;//被确认的那个数据包最初发送的时间。
        public long AdjustedLastAckedTime; //经过调整后的最后确认时间（通常减去了 ACK 延迟）。
        public ulong TotalBytesSent; //总共已发送的字节数。
        public long TotalBytesAcked; //总共已被确认的字节数。
        public ulong TotalBytesSentAtLastAck; //上次收到确认时已发送的总字节数。
        public ulong LargestSentPacketNumber; //已发送的最大的 packet number。
        public QUIC_SENT_PACKET_METADATA SentPackets;
        public QUIC_SENT_PACKET_METADATA SentPacketsTail;
        public QUIC_SENT_PACKET_METADATA LostPackets;
        public QUIC_SENT_PACKET_METADATA LostPacketsTail;
        public ushort ProbeCount; //探测数据包的发送次数。当没有足够的 ACK 来触发定时器时，系统会主动发送探测包以维持连接活跃并检测丢包。
    }

    internal static partial class MSQuicFunc
    {
        static void QuicLossDetectionInitialize(QUIC_LOSS_DETECTION LossDetection, QUIC_CONNECTION Connection)
        {
            LossDetection.mConnection = Connection;
            LossDetection.SentPackets = LossDetection.SentPacketsTail = null;
            LossDetection.LostPackets = LossDetection.LostPacketsTail = null;
            QuicLossDetectionInitializeInternalState(LossDetection);
        }

        static void QuicLossDetectionUninitialize(QUIC_LOSS_DETECTION LossDetection)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            while (LossDetection.SentPackets != null)
            {
                QUIC_SENT_PACKET_METADATA Packet = LossDetection.SentPackets;
                LossDetection.SentPackets = LossDetection.SentPackets.Next;
                QuicLossDetectionOnPacketDiscarded(LossDetection, Packet, false);
            }
            while (LossDetection.LostPackets != null)
            {
                QUIC_SENT_PACKET_METADATA Packet = LossDetection.LostPackets;
                LossDetection.LostPackets = LossDetection.LostPackets.Next;
                QuicLossDetectionOnPacketDiscarded(LossDetection, Packet, false);
            }
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
            PrevPacket = null;
            Packet = LossDetection.SentPackets;
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
                            LossDetection.SentPacketsTail = PrevPacket.Next;
                        }
                    }
                    else
                    {
                        LossDetection.SentPackets = NextPacket;
                        if (NextPacket == null)
                        {
                            LossDetection.SentPacketsTail = LossDetection.SentPackets;
                        }
                    }

                    if (Packet.Flags.IsAckEliciting)
                    {
                        LossDetection.PacketsInFlight--;
                        AckedRetransmittableBytes += Packet.PacketLength;
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

            if (AckedRetransmittableBytes > 0)
            {
                QUIC_PATH Path = Connection.Paths[0];

                QUIC_ACK_EVENT AckEvent = new QUIC_ACK_EVENT() {
                    IsImplicit = true,
                    TimeNow = TimeNow,
                    LargestAck = LossDetection.LargestAck,
                    LargestSentPacketNumber = LossDetection.LargestSentPacketNumber,
                    NumRetransmittableBytes = AckedRetransmittableBytes,
                    SmoothedRtt = Path.SmoothedRtt,
                    MinRtt = 0,
                    OneWayDelay = Path.OneWayDelay,
                    HasLoss = false,
                    AdjustedAckTime = 0,
                    AckedPackets = null,
                    NumTotalAckedRetransmittableBytes = 0,
                    IsLargestAckedPacketAppLimited = false,
                    MinRttValid = false
                };

                if (QuicCongestionControlOnDataAcknowledged(Connection.CongestionControl, AckEvent))
                {
                    QuicSendQueueFlush(Connection.Send,  QUIC_SEND_FLUSH_REASON.REASON_CONGESTION_CONTROL);
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
                    case QUIC_FRAME_TYPE.QUIC_FRAME_ACK:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_ACK_1:
                        QuicAckTrackerOnAckFrameAcked(Connection.Packets[(int)EncryptLevel].AckTracker, Packet.Frames[i].ACK.LargestAckedPacketNumber);
                        break;

                    case QUIC_FRAME_TYPE.QUIC_FRAME_RESET_STREAM:
                        QuicStreamOnResetAck(Packet.Frames[i].RESET_STREAM.Stream);
                        break;

                    case QUIC_FRAME_TYPE.QUIC_FRAME_RELIABLE_RESET_STREAM:
                        QuicStreamOnResetReliableAck(Packet.Frames[i].RELIABLE_RESET_STREAM.Stream);
                        break;

                    case QUIC_FRAME_TYPE.QUIC_FRAME_CRYPTO:
                        QuicCryptoOnAck(Connection.Crypto, Packet.Frames[i]);
                        break;

                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_1:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_2:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_3:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_4:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_5:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_6:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_7:
                        QuicStreamOnAck(Packet.Frames[i].STREAM.Stream, Packet.Flags, Packet.Frames[i]);
                        break;

                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_DATA_BLOCKED:
                        if (BoolOk(Packet.Frames[i].STREAM_DATA_BLOCKED.Stream.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL))
                        {
                            QuicSendSetStreamSendFlag(Connection.Send, Packet.Frames[i].STREAM_DATA_BLOCKED.Stream, QUIC_STREAM_SEND_FLAG_DATA_BLOCKED, false);
                        }
                        break;

                    case QUIC_FRAME_TYPE.QUIC_FRAME_NEW_CONNECTION_ID:
                        {
                            bool IsLastCid = false;
                            QUIC_CID SourceCid = QuicConnGetSourceCidFromSeq(
                                    Connection,
                                    Packet.Frames[i].NEW_CONNECTION_ID.Sequence,
                                    false,
                                    ref IsLastCid);
                            if (SourceCid != null)
                            {
                                SourceCid.Acknowledged = true;
                            }
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_RETIRE_CONNECTION_ID:
                        {
                            QUIC_CID DestCid = QuicConnGetDestCidFromSeq(Connection, Packet.Frames[i].RETIRE_CONNECTION_ID.Sequence, true);
                            if (DestCid != null)
                            {
                                NetLog.Assert(DestCid.Retired);
                                NetLog.Assert(Path == null || Path.DestCid != DestCid);
                                NetLog.Assert(Connection.RetiredDestCidCount > 0);
                                Connection.RetiredDestCidCount--;
                            }
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM_1:
                        QuicDatagramIndicateSendStateChange(
                            Connection,
                            ref Packet.Frames[i].DATAGRAM.ClientContext,
                            Packet.Flags.SuspectedLost ?
                                 QUIC_DATAGRAM_SEND_STATE.QUIC_DATAGRAM_SEND_ACKNOWLEDGED_SPURIOUS :
                                 QUIC_DATAGRAM_SEND_STATE.QUIC_DATAGRAM_SEND_ACKNOWLEDGED);
                        Packet.Frames[i].DATAGRAM.ClientContext = null;
                        break;

                    case QUIC_FRAME_TYPE.QUIC_FRAME_HANDSHAKE_DONE:
                        QuicCryptoHandshakeConfirmed(Connection.Crypto, true);
                        break;
                }
            }

            if (Path != null)
            {
                int PacketMtu = PacketSizeFromUdpPayloadSize(QuicAddrGetFamily(Path.Route.RemoteAddress), Packet.PacketLength);
                bool ChangedMtu = false;
                if (!Path.IsMinMtuValidated && PacketMtu >= Path.Mtu)
                {
                    Path.IsMinMtuValidated = true;
                    ChangedMtu = PacketMtu > Path.Mtu;
                }

                if (Packet.Flags.IsMtuProbe)
                {
                    NetLog.Assert(Path.IsMinMtuValidated);
                    if (QuicMtuDiscoveryOnAckedPacket(Path.MtuDiscovery, PacketMtu, Connection))
                    {
                        ChangedMtu = true;
                    }
                }
                if (ChangedMtu)
                {
                    QuicDatagramOnSendStateChanged(Connection.Datagram);
                }
            }

            if (!IsImplicit)
            {
                LossDetection.TotalBytesAcked += Packet.PacketLength;
                LossDetection.TotalBytesSentAtLastAck = Packet.TotalBytesSent;
                LossDetection.TimeOfLastPacketAcked = AckTime;
                LossDetection.TimeOfLastAckedPacketSent = Packet.SentTime;
                LossDetection.AdjustedLastAckedTime = AckTime - AckDelay;
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

            SentPacket.CopyFrom(TempSentPacket);
            LossDetection.LargestSentPacketNumber = TempSentPacket.PacketNumber;
            SentPacket.Next = null;

            if (LossDetection.SentPacketsTail == null)
            {
                LossDetection.SentPackets = LossDetection.SentPacketsTail = SentPacket;
            }
            else
            {
                LossDetection.SentPacketsTail.Next = SentPacket;
                LossDetection.SentPacketsTail = SentPacket;
            }
            NetLog.Assert(SentPacket.Flags.KeyType !=  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT || SentPacket.Flags.IsAckEliciting);

            Connection.Stats.Send.TotalPackets++;
            Connection.Stats.Send.TotalBytes += (ulong)TempSentPacket.PacketLength;
            if (SentPacket.Flags.IsAckEliciting)
            {
                if (LossDetection.PacketsInFlight == 0)
                {
                    QuicConnResetIdleTimeout(Connection);
                }

                Connection.Stats.Send.RetransmittablePackets++;
                LossDetection.PacketsInFlight++;
                LossDetection.TimeOfLastPacketSent = SentPacket.SentTime;

                if (!Path.IsPeerValidated)
                {
                    QuicPathDecrementAllowance(Connection, Path, SentPacket.PacketLength);
                }

                QuicCongestionControlOnDataSent(Connection.CongestionControl, SentPacket.PacketLength);
            }

            long SendPostedBytes = Connection.SendBuffer.PostedBytes;
            CXPLAT_LIST_ENTRY Entry = Connection.Send.SendStreams.Next;
            QUIC_STREAM Stream = (Entry != Connection.Send.SendStreams) ? CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(Entry) :null;

            if (SendPostedBytes < Path.Mtu &&
                QuicCongestionControlCanSend(Connection.CongestionControl) &&
                !QuicCryptoHasPendingCryptoFrame(Connection.Crypto) &&
                (Stream != null && QuicStreamAllowedByPeer(Stream)) && !QuicStreamCanSendNow(Stream, false))
            {
                QuicCongestionControlSetAppLimited(Connection.CongestionControl);
            }

            SentPacket.Flags.IsAppLimited = QuicCongestionControlIsAppLimited(Connection.CongestionControl);
            LossDetection.TotalBytesSent += (ulong)TempSentPacket.PacketLength;
            SentPacket.TotalBytesSent = LossDetection.TotalBytesSent;

            SentPacket.Flags.HasLastAckedPacketInfo = false;
            if (LossDetection.TimeOfLastPacketAcked > 0)
            {
                SentPacket.Flags.HasLastAckedPacketInfo = true;

                SentPacket.LastAckedPacketInfo.SentTime = LossDetection.TimeOfLastAckedPacketSent;
                SentPacket.LastAckedPacketInfo.AckTime = LossDetection.TimeOfLastPacketAcked;
                SentPacket.LastAckedPacketInfo.AdjustedAckTime = LossDetection.AdjustedLastAckedTime;
                SentPacket.LastAckedPacketInfo.TotalBytesSent = LossDetection.TotalBytesSentAtLastAck;
                SentPacket.LastAckedPacketInfo.TotalBytesAcked = (ulong)LossDetection.TotalBytesAcked;
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
                            bool IsLastCid =false;
                            QUIC_CID SourceCid = QuicConnGetSourceCidFromSeq(Connection, Packet.Frames[i].NEW_CONNECTION_ID.Sequence, false, ref IsLastCid);
                            if (SourceCid != null && !SourceCid.Acknowledged)
                            {
                                SourceCid.NeedsToSend = true;
                                NewDataQueued |= QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_NEW_CONNECTION_ID);
                            }
                            break;
                        }

                    case  QUIC_FRAME_TYPE.QUIC_FRAME_RETIRE_CONNECTION_ID:
                        {
                            QUIC_CID DestCid = QuicConnGetDestCidFromSeq(Connection, Packet.Frames[i].RETIRE_CONNECTION_ID.Sequence, false);
                            if (DestCid != null)
                            {
                                NetLog.Assert(DestCid.Retired);
                                DestCid.NeedsToSend = true;
                                NewDataQueued |= QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_RETIRE_CONNECTION_ID);
                            }
                            break;
                        }

                    case  QUIC_FRAME_TYPE.QUIC_FRAME_PATH_CHALLENGE:
                        {
                            int PathIndex = -1;
                            QUIC_PATH Path = QuicConnGetPathByID(Connection, Packet.PathId, ref PathIndex);
                            if (Path != null && !Path.IsPeerValidated)
                            {
                                long TimeNow = CxPlatTime();
                                NetLog.Assert(Connection.Configuration != null);
                                long ValidationTimeout = Math.Max(QuicLossDetectionComputeProbeTimeout(LossDetection, Path, 3), 6 * (Connection.Settings.InitialRttMs));
                                if (CxPlatTimeDiff(Path.PathValidationStartTime, TimeNow) > ValidationTimeout)
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
                            QuicDatagramIndicateSendStateChange(Connection, ref Packet.Frames[i].DATAGRAM.ClientContext,   QUIC_DATAGRAM_SEND_STATE.QUIC_DATAGRAM_SEND_LOST_SUSPECT);
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

        static void QuicLossDetectionUpdateTimer(QUIC_LOSS_DETECTION LossDetection, bool ExecuteImmediatelyIfNecessary)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);

            if (Connection.State.ClosedLocally || Connection.State.ClosedRemotely)
            {
                QuicConnTimerCancel(Connection,  QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_LOSS_DETECTION);
                return;
            }

            QUIC_SENT_PACKET_METADATA OldestPacket = QuicLossDetectionOldestOutstandingPacket(LossDetection);
            if (OldestPacket == null && (QuicConnIsServer(Connection) || Connection.Crypto.TlsState.WriteKey ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT))
            {
                QuicConnTimerCancel(Connection,  QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_LOSS_DETECTION);
                return;
            }

            QUIC_PATH Path = Connection.Paths[0];
            if (!Path.IsPeerValidated && Path.Allowance < QUIC_MIN_SEND_ALLOWANCE)
            {
                QuicConnTimerCancel(Connection,  QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_LOSS_DETECTION);
                return;
            }

            long TimeNow = CxPlatTime();
            NetLog.Assert(Path.SmoothedRtt != 0);

            long TimeFires;
            QUIC_LOSS_TIMER_TYPE TimeoutType;
            if (OldestPacket != null && OldestPacket.PacketNumber < LossDetection.LargestAck &&
                QuicKeyTypeToEncryptLevel(OldestPacket.Flags.KeyType) <= LossDetection.LargestAckEncryptLevel)
            {
                TimeoutType =  QUIC_LOSS_TIMER_TYPE.LOSS_TIMER_RACK;
                long RttUs = Math.Max(Path.SmoothedRtt, Path.LatestRttSample);
                TimeFires = OldestPacket.SentTime + QUIC_TIME_REORDER_THRESHOLD(RttUs);

            }
            else if (!Path.GotFirstRttSample)
            {
                TimeoutType =  QUIC_LOSS_TIMER_TYPE.LOSS_TIMER_INITIAL;
                TimeFires = LossDetection.TimeOfLastPacketSent + ((Path.SmoothedRtt + 4 * Path.RttVariance) << LossDetection.ProbeCount);
            }
            else
            {
                TimeoutType =  QUIC_LOSS_TIMER_TYPE.LOSS_TIMER_PROBE;
                TimeFires = LossDetection.TimeOfLastPacketSent + QuicLossDetectionComputeProbeTimeout(LossDetection, Path, 1 << LossDetection.ProbeCount);
            }

            long Delay;
            if (CxPlatTimeAtOrBefore64(TimeFires, TimeNow))
            {
                Delay = 0;
            }
            else
            {
                Delay = CxPlatTimeDiff(TimeNow, TimeFires);

                if (OldestPacket != null)
                {
                    long DisconnectTime = OldestPacket.SentTime + Connection.Settings.DisconnectTimeoutMs;
                    if (CxPlatTimeAtOrBefore64(DisconnectTime, TimeNow))
                    {
                        Delay = 0;
                    }
                    else
                    {
                        long MaxDelay = CxPlatTimeDiff(TimeNow, DisconnectTime);
                        if (Delay > MaxDelay)
                        {
                            Delay = MaxDelay;
                        }
                    }
                }
            }

            if (Delay == 0 && ExecuteImmediatelyIfNecessary)
            {
                QuicLossDetectionProcessTimerOperation(LossDetection);
            }
            else
            {
                QuicConnTimerSetEx(Connection,  QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_LOSS_DETECTION, Delay, TimeNow);
            }
        }

        static void QuicLossDetectionProcessTimerOperation(QUIC_LOSS_DETECTION LossDetection)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            QUIC_SENT_PACKET_METADATA OldestPacket = QuicLossDetectionOldestOutstandingPacket(LossDetection);
            if (OldestPacket == null && (QuicConnIsServer(Connection) || Connection.Crypto.TlsState.WriteKey ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT))
            {
                return;
            }

            long TimeNow = CxPlatTime();
            if (OldestPacket != null && CxPlatTimeDiff(OldestPacket.SentTime, TimeNow) >= Connection.Settings.DisconnectTimeoutMs)
            {
                QuicConnCloseLocally(Connection, QUIC_CLOSE_INTERNAL_SILENT | QUIC_CLOSE_QUIC_STATUS, QUIC_STATUS_CONNECTION_TIMEOUT, null);
            }
            else
            {
                if (!QuicLossDetectionDetectAndHandleLostPackets(LossDetection, TimeNow))
                {
                    QuicLossDetectionScheduleProbe(LossDetection);
                }

                QuicLossDetectionUpdateTimer(LossDetection, false);
            }
        }

        static void QuicLossDetectionOnPacketDiscarded(QUIC_LOSS_DETECTION LossDetection,QUIC_SENT_PACKET_METADATA Packet, bool DiscardedForLoss)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            if (Packet.Flags.IsMtuProbe && DiscardedForLoss)
            {
                int PathIndex = 0;
                QUIC_PATH Path = QuicConnGetPathByID(Connection, Packet.PathId, ref PathIndex);
                if (Path != null)
                {
                    ushort PacketMtu = PacketSizeFromUdpPayloadSize(QuicAddrGetFamily(Path.Route.RemoteAddress), Packet.PacketLength);
                    QuicMtuDiscoveryProbePacketDiscarded(Path.MtuDiscovery, Connection, PacketMtu);
                }
            }

            QuicSentPacketPoolReturnPacketMetadata(Packet, Connection);
        }

        static bool QuicLossDetectionDetectAndHandleLostPackets(QUIC_LOSS_DETECTION LossDetection, long TimeNow)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            int LostRetransmittableBytes = 0;
            QUIC_SENT_PACKET_METADATA Packet;

            if (LossDetection.LostPackets != null)
            {
                long TwoPto = QuicLossDetectionComputeProbeTimeout(LossDetection,Connection.Paths[0], 2);
                while ((Packet = LossDetection.LostPackets) != null && Packet.PacketNumber < LossDetection.LargestAck && CxPlatTimeDiff(Packet.SentTime, TimeNow) > TwoPto)
                {
                    LossDetection.LostPackets = Packet.Next;
                    QuicLossDetectionOnPacketDiscarded(LossDetection, Packet, true);
                }
                if (LossDetection.LostPackets == null)
                {
                    LossDetection.LostPacketsTail = LossDetection.LostPackets;
                }

                QuicLossValidate(LossDetection);
            }

            if (LossDetection.SentPackets != null)
            {
                QUIC_PATH Path = Connection.Paths[0]; // TODO - Correct?
                long Rtt = Math.Max(Path.SmoothedRtt, Path.LatestRttSample);
                long TimeReorderThreshold = QUIC_TIME_REORDER_THRESHOLD(Rtt);
                ulong LargestLostPacketNumber = 0;
                QUIC_SENT_PACKET_METADATA PrevPacket = null;
                Packet = LossDetection.SentPackets;
                while (Packet != null)
                {
                    bool NonretransmittableHandshakePacket =!Packet.Flags.IsAckEliciting && Packet.Flags.KeyType <  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT;
                    QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel(Packet.Flags.KeyType);
                    if (EncryptLevel > LossDetection.LargestAckEncryptLevel)
                    {
                        PrevPacket = Packet;
                        Packet = Packet.Next;
                        continue;
                    }

                    if (Packet.PacketNumber + QUIC_PACKET_REORDER_THRESHOLD < LossDetection.LargestAck)
                    {
                        if (!NonretransmittableHandshakePacket)
                        {
                           
                        }
                    }
                    else if (Packet.PacketNumber < LossDetection.LargestAck && CxPlatTimeAtOrBefore64(Packet.SentTime + TimeReorderThreshold, TimeNow))
                    {
                        if (!NonretransmittableHandshakePacket)
                        {
                            
                        }
                    }
                    else
                    {
                        break;
                    }

                    Connection.Stats.Send.SuspectedLostPackets++;
                    QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_SUSPECTED_LOST);
                    if (Packet.Flags.IsAckEliciting)
                    {
                        LossDetection.PacketsInFlight--;
                        LostRetransmittableBytes += Packet.PacketLength;
                        QuicLossDetectionRetransmitFrames(LossDetection, Packet, false);
                    }

                    LargestLostPacketNumber = Packet.PacketNumber;
                    if (PrevPacket == null)
                    {
                        LossDetection.SentPackets = Packet.Next;
                        if (Packet.Next == null)
                        {
                            LossDetection.SentPacketsTail = LossDetection.SentPackets;
                        }
                    }
                    else
                    {
                        PrevPacket.Next = Packet.Next;
                        if (Packet.Next == null)
                        {
                            LossDetection.SentPacketsTail = PrevPacket.Next;
                        }
                    }

                    LossDetection.LostPacketsTail = Packet;
                    LossDetection.LostPacketsTail = Packet.Next;
                    Packet = Packet.Next;
                    LossDetection.LostPacketsTail = null;
                }

                QuicLossValidate(LossDetection);

                if (LostRetransmittableBytes > 0)
                {
                    if (LossDetection.ProbeCount > QUIC_PERSISTENT_CONGESTION_THRESHOLD)
                    {
                        QuicConnUpdatePeerPacketTolerance(Connection, QUIC_MIN_ACK_SEND_NUMBER);
                    }

                    QUIC_LOSS_EVENT LossEvent = new QUIC_LOSS_EVENT()
                    {
                        LargestPacketNumberLost = LargestLostPacketNumber,
                        LargestSentPacketNumber = LossDetection.LargestSentPacketNumber,
                        NumRetransmittableBytes = LostRetransmittableBytes,
                        PersistentCongestion = LossDetection.ProbeCount >  QUIC_PERSISTENT_CONGESTION_THRESHOLD
                    };

                    QuicCongestionControlOnDataLost(Connection.CongestionControl, LossEvent);
                    QuicSendQueueFlush(Connection.Send,  QUIC_SEND_FLUSH_REASON.REASON_LOSS);
                }
            }

            QuicLossValidate(LossDetection);
            return LostRetransmittableBytes > 0;
        }

        static QUIC_SENT_PACKET_METADATA QuicLossDetectionOldestOutstandingPacket(QUIC_LOSS_DETECTION LossDetection)
        {
            QUIC_SENT_PACKET_METADATA Packet = LossDetection.SentPackets;
            while (Packet != null && !Packet.Flags.IsAckEliciting)
            {
                Packet = Packet.Next;
            }
            return Packet;
        }

        static void QuicLossDetectionScheduleProbe(QUIC_LOSS_DETECTION LossDetection)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            LossDetection.ProbeCount++;

            int NumPackets = 2;
            QuicCongestionControlSetExemption(Connection.CongestionControl, NumPackets);
            QuicSendQueueFlush(Connection.Send,  QUIC_SEND_FLUSH_REASON.REASON_PROBE);
            Connection.Send.TailLossProbeNeeded = true;

            if (Connection.Crypto.TlsState.WriteKey ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                for (CXPLAT_LIST_ENTRY Entry = Connection.Send.SendStreams.Next; Entry != Connection.Send.SendStreams; Entry = Entry.Next)
                {
                    QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(Entry);
                    if (QuicStreamCanSendNow(Stream, false))
                    {
                        if (--NumPackets == 0)
                        {
                            return;
                        }
                    }
                }
            }
            
            QUIC_SENT_PACKET_METADATA Packet = LossDetection.SentPackets;
            while (Packet != null)
            {
                if (Packet.Flags.IsAckEliciting)
                {
                    if (QuicLossDetectionRetransmitFrames(LossDetection, Packet, false) &&  --NumPackets == 0)
                    {
                        return;
                    }
                }
                Packet = Packet.Next;
            }

            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_PING);
        }

        static void QuicLossValidate(QUIC_LOSS_DETECTION LossDetection)
        {
            uint AckElicitingPackets = 0;
            QUIC_SENT_PACKET_METADATA Tail = LossDetection.SentPackets;
            QUIC_SENT_PACKET_METADATA LastTail = null;
            while (Tail != null)
            {
                if (Tail.Flags.IsAckEliciting)
                {
                    AckElicitingPackets++;
                }
                LastTail = Tail;
                Tail = Tail.Next;
            }
            NetLog.Assert(LossDetection.SentPacketsTail == LastTail);
            NetLog.Assert(LossDetection.PacketsInFlight == AckElicitingPackets, LossDetection.PacketsInFlight + ", " + AckElicitingPackets);

            Tail = LossDetection.LostPackets;
            LastTail = null;
            while (Tail != null)
            {
                LastTail = Tail;
                Tail = Tail.Next;
            }
            NetLog.Assert(LossDetection.LostPacketsTail == LastTail);
        }

        static void QuicLossDetectionOnZeroRttRejected(QUIC_LOSS_DETECTION LossDetection)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            QUIC_SENT_PACKET_METADATA PrevPacket;
            QUIC_SENT_PACKET_METADATA Packet;
            int CountRetransmittableBytes = 0;

            PrevPacket = null;
            Packet = LossDetection.SentPackets;
            while (Packet != null)
            {
                QUIC_SENT_PACKET_METADATA NextPacket = Packet.Next;
                if (Packet.Flags.KeyType ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT)
                {
                    if (PrevPacket != null)
                    {
                        PrevPacket.Next = NextPacket;
                        if (NextPacket == null)
                        {
                            LossDetection.SentPacketsTail = PrevPacket.Next;
                        }
                    }
                    else
                    {
                        LossDetection.SentPackets = NextPacket;
                        if (NextPacket == null)
                        {
                            LossDetection.SentPacketsTail = LossDetection.SentPackets;
                        }
                    }

                    NetLog.Assert(Packet.Flags.IsAckEliciting);
                    LossDetection.PacketsInFlight--;
                    CountRetransmittableBytes += Packet.PacketLength;
                    QuicLossDetectionRetransmitFrames(LossDetection, Packet, true);
                    Packet = NextPacket;

                }
                else
                {
                    PrevPacket = Packet;
                    Packet = NextPacket;
                }
            }

            QuicLossValidate(LossDetection);

            if (CountRetransmittableBytes > 0)
            {
                if (QuicCongestionControlOnDataInvalidated(Connection.CongestionControl, CountRetransmittableBytes))
                {
                    QuicSendQueueFlush(Connection.Send,  QUIC_SEND_FLUSH_REASON.REASON_CONGESTION_CONTROL);
                }
            }
        }

        static void QuicLossDetectionReset(QUIC_LOSS_DETECTION LossDetection)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            QuicConnTimerCancel(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_LOSS_DETECTION);
            QuicLossDetectionInitializeInternalState(LossDetection);
            while (LossDetection.SentPackets != null)
            {
                QUIC_SENT_PACKET_METADATA Packet = LossDetection.SentPackets;
                LossDetection.SentPackets = LossDetection.SentPackets.Next;
                QuicLossDetectionRetransmitFrames(LossDetection, Packet, true);
            }
            LossDetection.SentPacketsTail = LossDetection.SentPackets = null;

            while (LossDetection.LostPackets != null)
            {
                QUIC_SENT_PACKET_METADATA Packet = LossDetection.LostPackets;
                LossDetection.LostPackets = LossDetection.LostPackets.Next;
                QuicLossDetectionRetransmitFrames(LossDetection, Packet, true);
            }
            LossDetection.LostPacketsTail = LossDetection.LostPackets = null;

            QuicLossValidate(LossDetection);
        }

        static bool QuicLossDetectionProcessAckFrame(QUIC_LOSS_DETECTION LossDetection, QUIC_PATH Path, QUIC_RX_PACKET Packet,
            QUIC_ENCRYPT_LEVEL EncryptLevel, QUIC_FRAME_TYPE FrameType, ref QUIC_SSBuffer Buffer, ref bool InvalidFrame)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);

            long AckDelay = 0; // 我用微秒
            QUIC_ACK_ECN_EX Ecn = default;
            bool Result = QuicAckFrameDecode(
                    FrameType,
                    ref Buffer,
                    ref InvalidFrame,
                    Connection.DecodedAckRanges,
                    ref Ecn,
                    ref AckDelay);

            if (Result)
            {
                ulong Largest = 0;
                if (!QuicRangeGetMaxSafe(Connection.DecodedAckRanges, ref Largest) || LossDetection.LargestSentPacketNumber < Largest) 
                {
                    InvalidFrame = true;
                    Result = false;
                }
                else
                {
                    AckDelay <<= (int)Connection.PeerTransportParams.AckDelayExponent;
                    QuicLossDetectionProcessAckBlocks(
                        LossDetection,
                        Path,
                        Packet,
                        EncryptLevel,
                        AckDelay,
                        Connection.DecodedAckRanges,
                        ref InvalidFrame,
                        FrameType ==  QUIC_FRAME_TYPE.QUIC_FRAME_ACK_1 ? Ecn : default);
                }
            }

            QuicRangeReset(Connection.DecodedAckRanges);
            return Result;
        }

        static void QuicLossDetectionProcessAckBlocks(QUIC_LOSS_DETECTION LossDetection, QUIC_PATH Path, QUIC_RX_PACKET Packet, QUIC_ENCRYPT_LEVEL EncryptLevel,
            long AckDelay, QUIC_RANGE AckBlocks, ref bool InvalidAckBlock, QUIC_ACK_ECN_EX Ecn)
        {
            QUIC_SENT_PACKET_METADATA AckedPackets = null;//ACK确认包的队头
            QUIC_SENT_PACKET_METADATA AckedPacketsTail = AckedPackets;

            int AckedRetransmittableBytes = 0;
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            long TimeNow = CxPlatTime();
            long MinRtt = long.MaxValue;
            bool NewLargestAck = false;
            bool NewLargestAckRetransmittable = false;
            bool NewLargestAckDifferentPath = false;
            long NewLargestAckTimestamp = 0;

            InvalidAckBlock = false;

            QUIC_SENT_PACKET_METADATA LostPacketsStart = LossDetection.LostPackets;
            QUIC_SENT_PACKET_METADATA SentPacketsStart = LossDetection.SentPackets;
            QUIC_SENT_PACKET_METADATA LargestAckedPacket = null;

            int i = 0;
            QUIC_SUBRANGE AckBlock;
            while (!(AckBlock = QuicRangeGetSafe(AckBlocks, i++)).IsEmpty)
            {
                //在收到一个新的 ACK 帧后，检查之前标记为“已丢失”的数据包是否其实已经被接收方成功接收了。
                //如果确实被接收了，则将这些数据包标记为“虚假丢包”（spurious loss），并从“待重传队列”中移除它们。
                if (LostPacketsStart != null)
                {
                    while (LostPacketsStart != null && LostPacketsStart.PacketNumber < AckBlock.Low)
                    {
                        LostPacketsStart = LostPacketsStart.Next;
                    }
                    
                    QUIC_SENT_PACKET_METADATA End = LostPacketsStart;
                    QUIC_SENT_PACKET_METADATA LastEnd = null; 
                    while (End != null && End.PacketNumber <= QuicRangeGetHigh(AckBlock))
                    {
                        LastEnd = End;
                        Connection.Stats.Send.SpuriousLostPackets++;//被怀疑丢失，但又确认的包
                        QuicPerfCounterDecrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_SUSPECTED_LOST);
                        End = End.Next;
                    }

                    if (LostPacketsStart != End)
                    {
                        if(AckedPacketsTail == null)
                        {
                            AckedPackets = AckedPacketsTail = LostPacketsStart;
                        }
                        else
                        {
                            AckedPacketsTail.Next = LostPacketsStart;
                        }
                        AckedPacketsTail = LastEnd;

                        LostPacketsStart.Next = End;
                        if (End == LossDetection.LostPacketsTail)
                        {
                            LossDetection.LostPacketsTail = LostPacketsStart;
                        }

                        QuicLossValidate(LossDetection);
                    }

                    if (LossDetection.LostPackets == null)
                    {
                        if (QuicCongestionControlOnSpuriousCongestionEvent(Connection.CongestionControl))
                        {
                            QuicSendQueueFlush(Connection.Send, QUIC_SEND_FLUSH_REASON.REASON_CONGESTION_CONTROL);
                        }
                    }
                }

                if (SentPacketsStart != null)
                {
                    while (SentPacketsStart != null && SentPacketsStart.PacketNumber < AckBlock.Low)
                    {
                        SentPacketsStart = SentPacketsStart.Next;
                    }

                    QUIC_SENT_PACKET_METADATA End = SentPacketsStart;
                    while (End != null && End.PacketNumber <= QuicRangeGetHigh(AckBlock)) //如果接收到的 ACK 块，大于等于发送包的 包号，那么表示已经确认过了
                    {
                        if (End.Flags.IsAckEliciting)
                        {
                            LossDetection.PacketsInFlight--;
                            AckedRetransmittableBytes += End.PacketLength;
                        }
                        LargestAckedPacket = End;
                        End = End.Next;
                    }

                    if (SentPacketsStart != End)
                    {
                        if (AckedPacketsTail == null)
                        {
                            AckedPackets = AckedPacketsTail = SentPacketsStart;
                        }
                        else
                        {
                            AckedPacketsTail.Next = SentPacketsStart;
                        }

                        AckedPacketsTail = End;
                        SentPacketsStart = End;
                        if (End == LossDetection.SentPacketsTail)
                        {
                            LossDetection.SentPacketsTail = SentPacketsStart;
                        }

                        QuicLossValidate(LossDetection);
                    }
                }

                if (LargestAckedPacket != null &&
                    LossDetection.LargestAck <= LargestAckedPacket.PacketNumber)
                {
                    LossDetection.LargestAck = LargestAckedPacket.PacketNumber;
                    if (EncryptLevel > LossDetection.LargestAckEncryptLevel)
                    {
                        LossDetection.LargestAckEncryptLevel = EncryptLevel;
                    }
                    NewLargestAck = true;
                    NewLargestAckRetransmittable = LargestAckedPacket.Flags.IsAckEliciting;
                    NewLargestAckDifferentPath = Path.ID != LargestAckedPacket.PathId;
                    NewLargestAckTimestamp = LargestAckedPacket.SentTime;
                }
            }

            if (AckedPackets == null)
            {
                return;
            }

            ulong LargestAckedPacketNum = 0;
            bool IsLargestAckedPacketAppLimited = false;
            ulong EcnEctCounter = 0;
            QUIC_SENT_PACKET_METADATA AckedPacketsIterator = AckedPackets;

            while (AckedPacketsIterator != null)
            {
                QUIC_SENT_PACKET_METADATA PacketMeta = AckedPacketsIterator;
                AckedPacketsIterator = AckedPacketsIterator.Next;

                if (QuicKeyTypeToEncryptLevel(PacketMeta.Flags.KeyType) != EncryptLevel)
                {
                    InvalidAckBlock = true;
                    return;
                }

                long PacketRtt = CxPlatTimeDiff(PacketMeta.SentTime, TimeNow);
                MinRtt = Math.Min(MinRtt, PacketRtt);

                if (LargestAckedPacketNum < PacketMeta.PacketNumber)
                {
                    LargestAckedPacketNum = PacketMeta.PacketNumber;
                    IsLargestAckedPacketAppLimited = PacketMeta.Flags.IsAppLimited;
                }

                EcnEctCounter += (ulong)(PacketMeta.Flags.EcnEctSet ? 1 : 0);
                QuicLossDetectionOnPacketAcknowledged(LossDetection, EncryptLevel, PacketMeta, false, TimeNow, AckDelay);
            }

            QuicLossValidate(LossDetection);

            if (NewLargestAckRetransmittable && !NewLargestAckDifferentPath)
            {
                NetLog.Assert(MinRtt != long.MaxValue);
                if (MinRtt >= AckDelay)
                {
                    MinRtt -= AckDelay;
                }

                NetLog.Assert(NewLargestAckTimestamp != 0);
                QuicConnUpdateRtt(
                    Connection,
                    Path,
                    MinRtt,
                    NewLargestAckTimestamp - Connection.Stats.Timing.Start,
                    Packet.SendTimestamp);
            }

            if (NewLargestAck)
            {
                if (Path.EcnValidationState != ECN_VALIDATION_STATE.ECN_VALIDATION_FAILED)
                {
                    //
                    // Per RFC 9000, we validate ECN counts from received ACK frames
                    // when the largest acked packet number increases.
                    //
                    QUIC_PACKET_SPACE Packets = Connection.Packets[(int)EncryptLevel];
                    bool EcnValidated = true;
                    ulong EctCeDeltaSum = 0;
                    if (!Ecn.IsEmpty)
                    {
                        EctCeDeltaSum += Ecn.CE_Count - Packets.EcnCeCounter;
                        EctCeDeltaSum += Ecn.ECT_0_Count - Packets.EcnEctCounter;

                        if (EctCeDeltaSum < 0 ||
                            EctCeDeltaSum < EcnEctCounter ||
                            Ecn.ECT_1_Count != 0 ||
                            (ulong)Connection.Send.NumPacketsSentWithEct < Ecn.ECT_0_Count)
                        {
                            EcnValidated = false;
                        }
                        else
                        {
                            bool NewCE = Ecn.CE_Count > Packets.EcnCeCounter;
                            Packets.EcnCeCounter = Ecn.CE_Count;
                            Packets.EcnEctCounter = Ecn.ECT_0_Count;
                            if (Path.EcnValidationState <= ECN_VALIDATION_STATE.ECN_VALIDATION_UNKNOWN)
                            {
                                Path.EcnValidationState = ECN_VALIDATION_STATE.ECN_VALIDATION_CAPABLE;
                            }

                            if (Path.EcnValidationState == ECN_VALIDATION_STATE.ECN_VALIDATION_CAPABLE && NewCE)
                            {
                                QUIC_ECN_EVENT EcnEvent = new QUIC_ECN_EVENT() {
                                    LargestPacketNumberAcked = LargestAckedPacketNum,
                                    LargestSentPacketNumber = LossDetection.LargestSentPacketNumber,
                                };
                                QuicCongestionControlOnEcn(Connection.CongestionControl, EcnEvent);
                            }
                        }
                    }
                    else
                    {
                        if (EcnEctCounter != 0)
                        {
                            EcnValidated = false;
                        }
                    }

                    if (!EcnValidated)
                    {
                        Path.EcnValidationState = ECN_VALIDATION_STATE.ECN_VALIDATION_FAILED;
                    }
                }

                QuicLossDetectionDetectAndHandleLostPackets(LossDetection, TimeNow);
            }

            if (NewLargestAck || AckedRetransmittableBytes > 0)
            {
                QUIC_ACK_EVENT AckEvent = new QUIC_ACK_EVENT()
                {
                    IsImplicit = false,
                    TimeNow = TimeNow,
                    LargestAck = LossDetection.LargestAck,
                    LargestSentPacketNumber = LossDetection.LargestSentPacketNumber,
                    NumRetransmittableBytes = AckedRetransmittableBytes,
                    SmoothedRtt = Path.SmoothedRtt,
                    MinRtt = MinRtt,
                    OneWayDelay = Path.OneWayDelay,
                    HasLoss = (LossDetection.LostPackets != null),
                    AdjustedAckTime = TimeNow - AckDelay,
                    AckedPackets = AckedPackets,
                    NumTotalAckedRetransmittableBytes = LossDetection.TotalBytesAcked,
                    IsLargestAckedPacketAppLimited = IsLargestAckedPacketAppLimited,
                    MinRttValid = true,
                };

                if (QuicCongestionControlOnDataAcknowledged(Connection.CongestionControl, AckEvent))
                {
                    QuicSendQueueFlush(Connection.Send, QUIC_SEND_FLUSH_REASON.REASON_CONGESTION_CONTROL);
                }
            }

            LossDetection.ProbeCount = 0;

            AckedPacketsIterator = AckedPackets;
            while (AckedPacketsIterator != null)
            {
                QUIC_SENT_PACKET_METADATA PacketMeta = AckedPacketsIterator;
                AckedPacketsIterator = AckedPacketsIterator.Next;
                QuicSentPacketPoolReturnPacketMetadata(PacketMeta, Connection);
            }
            QuicLossDetectionUpdateTimer(LossDetection, false);
        }


    }
}
