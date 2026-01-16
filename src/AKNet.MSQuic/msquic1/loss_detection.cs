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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MSQuic1
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
        public long TotalBytesSent; //总共已发送的字节数。
        public long TotalBytesAcked; //总共已被确认的字节数。
        public long TotalBytesSentAtLastAck; //上次收到确认时已发送的总字节数。
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
            long PtoUs = Path.SmoothedRtt + 4 * Path.RttVariance + MS_TO_US(Connection.PeerTransportParams.MaxAckDelay);
            PtoUs *= Count;
            return PtoUs;
        }

        //丢弃包
        static void QuicLossDetectionDiscardPackets(QUIC_LOSS_DETECTION LossDetection, QUIC_PACKET_KEY_TYPE KeyType)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel(KeyType);
            int AckedRetransmittableBytes = 0;
            long TimeNow = CxPlatTimeUs();

            NetLog.Assert(KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL || KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE);

            QUIC_SENT_PACKET_METADATA PrevPacket = null;
            QUIC_SENT_PACKET_METADATA Packet = LossDetection.LostPackets;
            while (Packet != null)
            {
                QUIC_SENT_PACKET_METADATA NextPacket = Packet.Next;
                if (Packet.Flags.KeyType == KeyType) //这里的作用就是把这些废弃的KeyType，从链表中移除
                {
                    if (PrevPacket != null)
                    {
                        PrevPacket.Next = NextPacket;
                        if (NextPacket == null)
                        {
                            LossDetection.LostPacketsTail = PrevPacket;
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
                            LossDetection.SentPacketsTail = PrevPacket;
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
                QUIC_ACK_EVENT AckEvent = new QUIC_ACK_EVENT() 
                {
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

        //当包确认的时候
        static void QuicLossDetectionOnPacketAcknowledged(QUIC_LOSS_DETECTION LossDetection,QUIC_ENCRYPT_LEVEL EncryptLevel, QUIC_SENT_PACKET_METADATA Packet,
            bool IsImplicit, long AckTime, long AckDelay)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            int PathIndex = -1;
            QUIC_PATH Path = QuicConnGetPathByID(Connection, Packet.PathId, ref PathIndex);

            NetLog.Assert(EncryptLevel >=  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_INITIAL && EncryptLevel <  QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_COUNT);
            if (QuicConnIsClient(Connection) && !Connection.State.HandshakeConfirmed && Packet.Flags.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
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
                        //发送端告诉对端：“我想继续发数据，但你的 MAX_STREAM_DATA 窗口已经顶到上限，无法再发一个字节。”
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
                                QUIC_CID_VALIDATE_NULL(Connection, DestCid);
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
            
            QUIC_SENT_PACKET_METADATA SentPacket = QuicSentPacketPoolGetPacketMetadata(Connection.Partition.SentPacketPool, TempSentPacket.FrameCount);
            if (SentPacket == null)
            {
                //内存不足的时候
                QuicLossDetectionRetransmitFrames(LossDetection, TempSentPacket, false);
                QuicSentPacketMetadataReleaseFrames(TempSentPacket, Connection);
                return;
            }

            SentPacket.CopyFrom(TempSentPacket); //拷贝一份数据，用作重传使用 
            LossDetection.LargestSentPacketNumber = TempSentPacket.PacketNumber; //最大的发送包号
            SentPacket.Next = null;

            //加到重传发送队列里
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
            Connection.Stats.Send.TotalBytes += TempSentPacket.PacketLength;
            if (SentPacket.Flags.IsAckEliciting)
            {
                if (LossDetection.PacketsInFlight == 0)
                {
                    QuicConnResetIdleTimeout(Connection); //重置待机超时时间
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
            QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(Entry);

            if (SendPostedBytes < Path.Mtu &&
                QuicCongestionControlCanSend(Connection.CongestionControl) &&
                !QuicCryptoHasPendingCryptoFrame(Connection.Crypto) &&
                (Stream != null && QuicStreamAllowedByPeer(Stream)) && !QuicStreamCanSendNow(Stream, false))
            {
                QuicCongestionControlSetAppLimited(Connection.CongestionControl);
            }

            SentPacket.Flags.IsAppLimited = QuicCongestionControlIsAppLimited(Connection.CongestionControl);
            LossDetection.TotalBytesSent += TempSentPacket.PacketLength;
            SentPacket.TotalBytesSent = LossDetection.TotalBytesSent;

            SentPacket.Flags.HasLastAckedPacketInfo = false;
            if (LossDetection.TimeOfLastPacketAcked > 0)
            {
                SentPacket.Flags.HasLastAckedPacketInfo = true;

                SentPacket.LastAckedPacketInfo.SentTime = LossDetection.TimeOfLastAckedPacketSent;
                SentPacket.LastAckedPacketInfo.AckTime = LossDetection.TimeOfLastPacketAcked;
                SentPacket.LastAckedPacketInfo.AdjustedAckTime = LossDetection.AdjustedLastAckedTime;
                SentPacket.LastAckedPacketInfo.TotalBytesSent = LossDetection.TotalBytesSentAtLastAck;
                SentPacket.LastAckedPacketInfo.TotalBytesAcked = LossDetection.TotalBytesAcked;
            }
            
            QuicLossValidate(LossDetection);
            //NetLog.Log($"等待确认 PacketNumber: {LossDetection.LargestSentPacketNumber}, {LossDetection.PacketsInFlight}");
        }

        //这里 重传 包
        //这个函数的主要任务是：
        //遍历一个已丢失的数据包中携带的所有可重传帧，将它们重新提交给发送引擎（Send Buffer），并可选地释放原包内存。
        //它不直接构造新数据包，而是“触发重传动作”，由上层（如 QuicSendGeneratePacket）负责实际打包。
        static bool QuicLossDetectionRetransmitFrames(QUIC_LOSS_DETECTION LossDetection, QUIC_SENT_PACKET_METADATA Packet, bool ReleasePacket)
        {
            NET_ADD_STATS(LossDetection.mConnection.Partition, UDP_STATISTIC_TYPE.ReSendCount);

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
                                QUIC_CID_VALIDATE_NULL(Connection, DestCid);
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
                                long TimeNow = CxPlatTimeUs();
                                NetLog.Assert(Connection.Configuration != null);
                                long ValidationTimeout = Math.Max(QuicLossDetectionComputeProbeTimeout(LossDetection, Path, 3), 6 * MS_TO_US(Connection.Settings.InitialRttMs));
                                if (CxPlatTimeDiff(Path.PathValidationStartTime, TimeNow) > ValidationTimeout)
                                {
                                    QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PATH_FAILURE);
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

        //更新计时器
        static void QuicLossDetectionUpdateTimer(QUIC_LOSS_DETECTION LossDetection, bool ExecuteImmediatelyIfNecessary)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            if (Connection.State.ClosedLocally || Connection.State.ClosedRemotely)
            {
                QuicConnTimerCancel(Connection,  QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_LOSS_DETECTION);
                return;
            }

            QUIC_SENT_PACKET_METADATA OldestPacket = QuicLossDetectionOldestOutstandingPacket(LossDetection);
            if (OldestPacket == null && (QuicConnIsServer(Connection) || Connection.Crypto.TlsState.WriteKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT))
            {
                //NetLog.Log("QuicLossDetectionUpdateTimer 取消计时器");
                // ACK 已经确认了所有包，那么我们就停止运行计时器
                QuicConnTimerCancel(Connection,  QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_LOSS_DETECTION);
                return;
            }

            QUIC_PATH Path = Connection.Paths[0];
            if (!Path.IsPeerValidated && Path.Allowance < QUIC_MIN_SEND_ALLOWANCE)
            {
                //当连接处于“反放大限制”（anti-amplification limit）状态，
                //且没有足够的 Allowance 来发送任何数据时，主动禁用发送相关的定时器（如 PTO、Probe Timer）。
                QuicConnTimerCancel(Connection,  QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_LOSS_DETECTION);
                return;
            }

            long TimeNow = CxPlatTimeUs();
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

            long Delay; //微妙 us
            if (CxPlatTimeAtOrBefore64(TimeFires, TimeNow))
            {
                Delay = 0;
            }
            else
            {
                Delay = CxPlatTimeDiff(TimeNow, TimeFires);

                if (OldestPacket != null)
                {
                    long DisconnectTime = OldestPacket.SentTime + MS_TO_US(Connection.Settings.DisconnectTimeoutMs);
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

            NET_ADD_AVERAGE_STATS(Connection.Partition, UDP_STATISTIC_TYPE.LOSS_DETECTION_TIME_AVERAGE, US_TO_S(Delay));

            if (Delay == 0 && ExecuteImmediatelyIfNecessary)
            {
                QuicLossDetectionProcessTimerOperation(LossDetection);
            }
            else
            {
                QuicConnTimerSetEx(Connection,  QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_LOSS_DETECTION, Delay, TimeNow);
            }
        }

        //这个是 定时器 触发的 方法
        static void QuicLossDetectionProcessTimerOperation(QUIC_LOSS_DETECTION LossDetection)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            QUIC_SENT_PACKET_METADATA OldestPacket = QuicLossDetectionOldestOutstandingPacket(LossDetection);
            if (OldestPacket == null && (QuicConnIsServer(Connection) || Connection.Crypto.TlsState.WriteKey ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT))
            {
                return;
            }

            long TimeNow = CxPlatTimeUs();
            if (OldestPacket != null && CxPlatTimeDiff(OldestPacket.SentTime, TimeNow) >= MS_TO_US(Connection.Settings.DisconnectTimeoutMs))
            {
                QuicLossPrintStateInfo(LossDetection, "超时爆断");
                //超时爆断
                QuicConnCloseLocally(Connection, 
                    QUIC_CLOSE_INTERNAL_SILENT | QUIC_CLOSE_QUIC_STATUS, QUIC_STATUS_CONNECTION_TIMEOUT,
                    $"LossDetection DisconnectTimeout: {OldestPacket} " +
                    $"超时时间: {US_TO_S(TimeNow - OldestPacket.SentTime)} " +
                    $"当前最大ACK: {LossDetection.LargestAck}");
            }
            else
            {
                //这里的话就是 处理丢包
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

        //1:去掉 丢包队列 里大于2倍RTO时间的包
        //2:把 发送队列里的 可疑丢包，加到 丢包队列里
        static bool QuicLossDetectionDetectAndHandleLostPackets(QUIC_LOSS_DETECTION LossDetection, long TimeNow)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            int LostRetransmittableBytes = 0;
            QUIC_SENT_PACKET_METADATA Packet;

            if (LossDetection.LostPackets != null)
            {
                //当一个数据包发送后超过 2*PTO 时间仍未确认，且已被标记为“丢失”，就可以认为它非常“陈旧”，可以被安全地清理。
                long TwoPto = QuicLossDetectionComputeProbeTimeout(LossDetection, Connection.Paths[0], 2);
                while ((Packet = LossDetection.LostPackets) != null && Packet.PacketNumber < LossDetection.LargestAck && CxPlatTimeDiff(Packet.SentTime, TimeNow) > TwoPto)
                {
                    LossDetection.LostPackets = Packet.Next;
                    QuicLossDetectionOnPacketDiscarded(LossDetection, Packet, true);
                }

                if (LossDetection.LostPackets == null)
                {
                    LossDetection.LostPacketsTail = LossDetection.LostPackets = null;
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
                        //怀疑丢包1
                        if (!NonretransmittableHandshakePacket)
                        {
                           
                        }
                    }
                    else if (Packet.PacketNumber < LossDetection.LargestAck && CxPlatTimeAtOrBefore64(Packet.SentTime + TimeReorderThreshold, TimeNow))
                    {
                        //怀疑丢包2
                        if (!NonretransmittableHandshakePacket)
                        {
                            
                        }
                    }
                    else
                    {
                        //不怀疑丢包
                        break;
                    }

                    //这里处理可疑的丢包
                    Connection.Stats.Send.SuspectedLostPackets++;
                    QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_SUSPECTED_LOST);
                    if (Packet.Flags.IsAckEliciting)
                    {
                        LossDetection.PacketsInFlight--;
                        LostRetransmittableBytes += Packet.PacketLength;
                        QuicLossDetectionRetransmitFrames(LossDetection, Packet, false);
                    }

                    LargestLostPacketNumber = Packet.PacketNumber;

                    //这里的话，就是把Packet 从发送队列里移除，加入到丢包队列里
                    if (PrevPacket == null)
                    {
                        LossDetection.SentPackets = Packet.Next;
                        if (Packet.Next == null)
                        {
                            LossDetection.SentPacketsTail = LossDetection.SentPackets = null;
                        }
                    }
                    else
                    {
                        PrevPacket.Next = Packet.Next;
                        if (Packet.Next == null)
                        {
                            LossDetection.SentPacketsTail = PrevPacket;
                        }
                    }

                    //把Packet 加入到 丢包队列里
                    if (LossDetection.LostPacketsTail == null)
                    {
                        LossDetection.LostPackets = LossDetection.LostPacketsTail = Packet;
                    }
                    else
                    {
                        LossDetection.LostPacketsTail.Next = Packet;
                        LossDetection.LostPacketsTail = Packet;
                    }
                    
                    Packet = Packet.Next;
                    LossDetection.LostPacketsTail.Next = null;
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

        //这个结构体包含了丢包检测机制所需的所有状态信息，其中 SentPackets 是一个链表，按发送时间顺序（从旧到新）链接了所有尚未被确认的数据包元数据。
        static QUIC_SENT_PACKET_METADATA QuicLossDetectionOldestOutstandingPacket(QUIC_LOSS_DETECTION LossDetection)
        {
            QUIC_SENT_PACKET_METADATA Packet = LossDetection.SentPackets;
            while (Packet != null && !Packet.Flags.IsAckEliciting)
            {
                //循环条件：如果当前数据包存在 (Packet != NULL) 并且 它不需要ACK确认 (!IsAckEliciting)，则继续循环。
                Packet = Packet.Next;
            }
            return Packet;
        }

        //QuicLossDetectionScheduleProbe 是 MSQuic 内部用来“立即安排一次探测包（PTO probe）发送” 的函数，核心目的只有一句话：
        //当丢检定时器（PTO）到期，而发送端仍然没有收到任何 ACK 时，调用它把一个探测包塞进发送队列，强制让对端回 ACK，从而打破“死沉默”。
        static void QuicLossDetectionScheduleProbe(QUIC_LOSS_DETECTION LossDetection)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            LossDetection.ProbeCount++;

            int NumPackets = 2;
            QuicCongestionControlSetExemption(Connection.CongestionControl, NumPackets); //设置紧急包豁免数量
            QuicSendQueueFlush(Connection.Send,  QUIC_SEND_FLUSH_REASON.REASON_PROBE);
            Connection.Send.TailLossProbeNeeded = true;

            //如果流中还有剩余数据,就发剩余数据,来探测
            if (Connection.Crypto.TlsState.WriteKey == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
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
            
            //没有足够的数据存在，那就发送先前的数据
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

            //最后发送Ping包
            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_PING);
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

            long AckDelay = 0; //微秒
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
                if (!QuicRangeGetMaxSafe(Connection.DecodedAckRanges, out ulong Largest) || LossDetection.LargestSentPacketNumber < Largest) 
                {
                    InvalidFrame = true;
                    Result = false;
                }
                else
                {
                    //NetLog.Log("处理ACK帧 Connection.DecodedAckRanges: " + Connection.DecodedAckRanges);
                    AckDelay <<= (int)Connection.PeerTransportParams.AckDelayExponent;
                    QuicLossDetectionProcessAckBlocks(
                        LossDetection,
                        Path,
                        Packet,
                        EncryptLevel,
                        AckDelay,
                        Connection.DecodedAckRanges,
                        ref InvalidFrame,
                        (FrameType ==  QUIC_FRAME_TYPE.QUIC_FRAME_ACK_1 ? Ecn : default));
                }
            }
            else
            {
                NetLog.LogError("QuicLossDetectionProcessAckFrame 解析ACK帧失败");
            }

            QuicRangeReset(Connection.DecodedAckRanges);
            return Result;
        }

        //收到返回的ACK 确认包后，去处理掉重传队列
        static void QuicLossDetectionProcessAckBlocks(QUIC_LOSS_DETECTION LossDetection, QUIC_PATH Path, QUIC_RX_PACKET Packet, QUIC_ENCRYPT_LEVEL EncryptLevel,
            long AckDelay, QUIC_RANGE AckBlocks, ref bool InvalidAckBlock, QUIC_ACK_ECN_EX Ecn)
        {
            Debug.Assert(LossDetection.mConnection.WorkerThreadID == Thread.CurrentThread.ManagedThreadId, "多线程错误");

            QUIC_SENT_PACKET_METADATA AckedPackets = null;//ACK确认包的队头
            QUIC_SENT_PACKET_METADATA AckedPacketsTail = AckedPackets;

            int AckedRetransmittableBytes = 0;
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            long TimeNow = CxPlatTimeUs();
            long MinRtt = long.MaxValue;
            bool NewLargestAck = false;
            bool NewLargestAckRetransmittable = false;
            bool NewLargestAckDifferentPath = false;
            long NewLargestAckTimestamp = 0;

            InvalidAckBlock = false;
            QUIC_SENT_PACKET_METADATA LargestAckedPacket = null;
            QUIC_SENT_PACKET_METADATA LostPacketsStart = LossDetection.LostPackets;
            QUIC_SENT_PACKET_METADATA SentPacketsStart = LossDetection.SentPackets;
            QUIC_SENT_PACKET_METADATA SentPacketsStart_Prev = null;
            QUIC_SENT_PACKET_METADATA LostPacketsStart_Prev = null;

            int i = 0;
            QUIC_SUBRANGE AckBlock;
            while ((AckBlock = QuicRangeGetSafe(AckBlocks, i++)) != null)
            {
                //在收到一个新的 ACK 帧后，检查之前标记为“已丢失”的数据包是否其实已经被接收方成功接收了。
                //如果确实被接收了，则将这些数据包标记为“虚假丢包”（spurious loss），并从“待重传队列”中移除它们。
                if (LostPacketsStart != null)
                {
                    QUIC_SENT_PACKET_METADATA LastLostPacket = LossDetection.LostPacketsTail;
                    if (LastLostPacket.PacketNumber < AckBlock.Low)
                    {
                        goto CheckSentPackets;
                    }

                    QUIC_SENT_PACKET_METADATA BeginRemovePre = LostPacketsStart_Prev;
                    QUIC_SENT_PACKET_METADATA BeginRemove = null;
                    QUIC_SENT_PACKET_METADATA EndRemove = null;
                    QUIC_SENT_PACKET_METADATA EndRemoveNext = null;
                    while (LostPacketsStart != null)
                    {
                        if (LostPacketsStart.PacketNumber < AckBlock.Low)
                        {
                            BeginRemovePre = LostPacketsStart;
                            LostPacketsStart = LostPacketsStart.Next;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (LostPacketsStart != null && LostPacketsStart.PacketNumber >= AckBlock.Low)
                    {
                        while (LostPacketsStart != null && LostPacketsStart.PacketNumber <= AckBlock.High)
                        {
                            Connection.Stats.Send.SpuriousLostPackets++;//被怀疑丢失，但又确认的包
                            QuicPerfCounterDecrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_SUSPECTED_LOST);

                            if (BeginRemove == null)
                            {
                                BeginRemove = LostPacketsStart;
                            }

                            EndRemove = LostPacketsStart;
                            EndRemoveNext = LostPacketsStart.Next;
                            LostPacketsStart = LostPacketsStart.Next;
                        }
                    }

                    if (BeginRemove != null)
                    {
                        //确认的包，加到ACK确认队列里
                        if (AckedPacketsTail == null)
                        {
                            AckedPackets = BeginRemove;
                            AckedPacketsTail = EndRemove;
                        }
                        else
                        {
                            AckedPacketsTail.Next = BeginRemove;
                            AckedPacketsTail = EndRemove;
                        }

                        EndRemove.Next = null;

                        //移除/拼接 剩余块
                        if (BeginRemove == LossDetection.LostPackets && EndRemove == LossDetection.LostPacketsTail)
                        {
                            LossDetection.LostPackets = LossDetection.LostPacketsTail = null;
                        }
                        else if (BeginRemove == LossDetection.LostPackets)
                        {
                            LossDetection.LostPackets = EndRemoveNext;
                        }
                        else if (EndRemove == LossDetection.LostPacketsTail)
                        {
                            LossDetection.LostPacketsTail = BeginRemovePre;
                        }

                        if (BeginRemovePre != null)
                        {
                            BeginRemovePre.Next = EndRemoveNext;
                        }
                        NetLog.Assert(LostPacketsStart == EndRemoveNext);
                        QuicLossValidate(LossDetection);
                    }

                    LostPacketsStart_Prev = BeginRemovePre;

                    //如果所有之前认为丢失的包都恢复了，则通知拥塞控制模块进行相应调整。
                    if (LossDetection.LostPackets == null)
                    {
                        if (QuicCongestionControlOnSpuriousCongestionEvent(Connection.CongestionControl))
                        {
                            QuicSendQueueFlush(Connection.Send, QUIC_SEND_FLUSH_REASON.REASON_CONGESTION_CONTROL);
                        }
                    }
                }

            CheckSentPackets:
                //NetLog.Log("AckBlock: " + AckBlock);

                //等待重传的发送队列，接收到ACK后，从队列中删除这些无用包
                if (SentPacketsStart != null)
                {
                    //它们还没被确认，保持原位
                    //如果多个ACK,这个 BeginRemovePre 值有可能不为null啊
                    QUIC_SENT_PACKET_METADATA BeginRemovePre = SentPacketsStart_Prev;
                    QUIC_SENT_PACKET_METADATA BeginRemove = null;
                    QUIC_SENT_PACKET_METADATA EndRemove = null;
                    QUIC_SENT_PACKET_METADATA EndRemoveNext = null;
                    while (SentPacketsStart != null)
                    {
                        if (SentPacketsStart.PacketNumber < AckBlock.Low)
                        {
                            BeginRemovePre = SentPacketsStart;
                            SentPacketsStart = SentPacketsStart.Next;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (SentPacketsStart != null && SentPacketsStart.PacketNumber >= AckBlock.Low)
                    {
                        //如果接收到的 ACK 块，大于等于发送包的 包号，那么刚好是正要等待确认的
                        while (SentPacketsStart != null && SentPacketsStart.PacketNumber <= AckBlock.High)
                        {
                            if (SentPacketsStart.Flags.IsAckEliciting) //需要确认，刚好有这个确认包了
                            {
                                LossDetection.PacketsInFlight--;
                                AckedRetransmittableBytes += SentPacketsStart.PacketLength;
                            }

                            LargestAckedPacket = SentPacketsStart;

                            if (BeginRemove == null)
                            {
                                BeginRemove = SentPacketsStart;
                            }

                            EndRemove = SentPacketsStart;
                            EndRemoveNext = SentPacketsStart.Next;
                            SentPacketsStart = SentPacketsStart.Next;
                        }
                    }

                    if (BeginRemove != null)
                    {
                        //确认的包，加到ACK确认队列里
                        if (AckedPacketsTail == null)
                        {
                            AckedPackets = BeginRemove;
                            AckedPacketsTail = EndRemove;
                        }
                        else
                        {
                            AckedPacketsTail.Next = BeginRemove;
                            AckedPacketsTail = EndRemove;
                        }

                        EndRemove.Next = null;

                        //移除/拼接 剩余块
                        if (BeginRemove == LossDetection.SentPackets && EndRemove == LossDetection.SentPacketsTail)
                        {
                            LossDetection.SentPackets = LossDetection.SentPacketsTail = null;
                        }
                        else if (BeginRemove == LossDetection.SentPackets)
                        {
                            LossDetection.SentPackets = EndRemoveNext;
                        }
                        else if (EndRemove == LossDetection.SentPacketsTail)
                        {
                            LossDetection.SentPacketsTail = BeginRemovePre;
                        }

                        if(BeginRemovePre != null)
                        {
                            BeginRemovePre.Next = EndRemoveNext;
                        }
                        NetLog.Assert(SentPacketsStart == EndRemoveNext);

                        //QuicLossPrintStateInfo(LossDetection, "2222222");
                        QuicLossValidate(LossDetection);
                    }

                    SentPacketsStart_Prev = BeginRemovePre;
                }
                
                if (LargestAckedPacket != null && LossDetection.LargestAck <= LargestAckedPacket.PacketNumber)
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

                NET_ADD_AVERAGE_STATS(Connection.Partition, UDP_STATISTIC_TYPE.SmoothedRtt, US_TO_S(Path.SmoothedRtt));
                NET_ADD_AVERAGE_STATS(Connection.Partition, UDP_STATISTIC_TYPE.MinRtt, US_TO_S(Path.MinRtt));
                NET_ADD_AVERAGE_STATS(Connection.Partition, UDP_STATISTIC_TYPE.MaxRtt, US_TO_S(Path.MaxRtt));
            }

            if (NewLargestAck)
            {
                if (Path.EcnValidationState != ECN_VALIDATION_STATE.ECN_VALIDATION_FAILED)
                {
                    QUIC_PACKET_SPACE Packets = Connection.Packets[(int)EncryptLevel];
                    bool EcnValidated = true;
                    ulong EctCeDeltaSum = 0;
                    if (!Ecn.IsEmpty)
                    {
                        EctCeDeltaSum += Ecn.CE_Count - Packets.EcnCeCounter;
                        EctCeDeltaSum += Ecn.ECT_0_Count - Packets.EcnEctCounter;

                        if (EctCeDeltaSum < 0 || EctCeDeltaSum < EcnEctCounter || Ecn.ECT_1_Count != 0 ||
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
                                QUIC_ECN_EVENT EcnEvent = new QUIC_ECN_EVENT() 
                                {
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
            AckedPacketsIterator = AckedPackets; //已确认的包
            while (AckedPacketsIterator != null)
            {
                QUIC_SENT_PACKET_METADATA PacketMeta = AckedPacketsIterator;
                AckedPacketsIterator = AckedPacketsIterator.Next;
                QuicSentPacketPoolReturnPacketMetadata(PacketMeta, Connection);
            }
            QuicLossDetectionUpdateTimer(LossDetection, false);
        }


        //------------------------------------------诊断信息----------------------------------------------
        [Conditional("DEBUG")]
        static void QuicLossPrintStateInfo(QUIC_LOSS_DETECTION LossDetection, string Tag)
        {
            if (true)
            {
                int nLostCount = 0;
                int nAllSendCount = 0;
                int ackNeedSendCount = 0;
                List<ulong> mNumberList = new List<ulong>();
                QUIC_SENT_PACKET_METADATA mPackage = LossDetection.SentPackets;
                while (mPackage != null)
                {
                    if (mPackage.Flags.IsAckEliciting)
                    {
                        ackNeedSendCount++;
                        mNumberList.Add(mPackage.PacketNumber);
                    }

                    mPackage = mPackage.Next;
                    nAllSendCount++;
                }

                mPackage = LossDetection.LostPackets;
                while (mPackage != null)
                {
                    mPackage = mPackage.Next;
                    nLostCount++;
                }

                List<string> mLogList = new List<string>();
                mLogList.Add($"-----------{Tag}---------丢包/重传 统计-------线程ID: {Thread.CurrentThread.ManagedThreadId}-----------");
                mLogList.Add($"飞行中的包数量: {LossDetection.PacketsInFlight}");
                mLogList.Add($"需要ACK确认的包数量: {mNumberList.Count}");
                mLogList.Add($"发送包的最大包号: {LossDetection.LargestSentPacketNumber}");
                mLogList.Add($"本次收到ACK确认的最大包号: {LossDetection.LargestAck}");
                mLogList.Add($"没有收到ACK时,下一步探测数量: {LossDetection.ProbeCount}");
                mLogList.Add($"SentPackets: {nAllSendCount}, {mNumberList.Count}, {nAllSendCount - mNumberList.Count}  {string.Join('-', mNumberList)}");
                mLogList.Add($"LostPackets: {nLostCount}");
                NetLog.Log(string.Join("\n", mLogList));
            }
        }

        [Conditional("DEBUG")]
        static void QuicLossValidate(QUIC_LOSS_DETECTION LossDetection)
        {
#if DEBUG
            int AckElicitingPackets = 0;
            QUIC_SENT_PACKET_METADATA Tail = LossDetection.SentPackets;
            while (Tail != null)
            {
                if (Tail.Flags.IsAckEliciting)
                {
                    AckElicitingPackets++;
                }

                if (Tail.Next != null)
                {
                    Tail = Tail.Next;
                }
                else
                {
                    break;
                }
            }

            NetLog.Assert(LossDetection.SentPacketsTail == Tail);
            if(LossDetection.PacketsInFlight != AckElicitingPackets)
            {
                List<string> mLogList = new List<string>();
                mLogList.Add($"--------------------断言失败-------线程ID: {Thread.CurrentThread.ManagedThreadId}-----------");
                mLogList.Add($"飞行中的包数量: {LossDetection.PacketsInFlight}");
                mLogList.Add($"需要ACK确认的包数量: {AckElicitingPackets}");
                //NetLog.LogError(string.Join("\n", mLogList));
                throw new Exception(string.Join("\n", mLogList));
            }

            if (LossDetection.SentPacketsTail == null)
            {
                NetLog.Assert(LossDetection.SentPackets == null);
            }

            Tail = LossDetection.LostPackets;
            while (Tail != null && Tail.Next != null)
            {
                Tail = Tail.Next;
            }
            NetLog.Assert(LossDetection.LostPacketsTail == Tail);
            if (LossDetection.LostPacketsTail == null)
            {
                NetLog.Assert(LossDetection.LostPackets == null);
            }
#endif
        }
    }
}
