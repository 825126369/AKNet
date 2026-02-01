/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:58
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace MSQuic2
{
    internal enum QUIC_ACK_TYPE
    {
        QUIC_ACK_TYPE_NON_ACK_ELICITING,//表示该数据包不会触发确认（ACK）。这种类型的数据包通常用于不需要立即确认的场景，例如某些控制帧或已确认的数据包。
        QUIC_ACK_TYPE_ACK_ELICITING,    //表示该数据包会触发确认（ACK）。发送方期望接收方在收到这种类型的数据包后发送确认信息。
        QUIC_ACK_TYPE_ACK_IMMEDIATE,    //表示该数据包需要立即确认。这种类型的数据包通常用于需要快速确认的场景，例如某些关键的控制帧或数据包。
    }

    //Acknowledge: 确认的意思
    internal class QUIC_ACK_TRACKER
    {
        public QUIC_PACKET_SPACE CXPLAT_CONTAINING_RECORD = null;

        //作用：重复包检测
        //容量上限：QUIC_MAX_RANGE_DUPLICATE_PACKETS（默认 16384 字节）
        //逻辑：任何先于此区间最小值的包都视为“已收过”，直接丢弃，不回 ACK。
        public readonly QUIC_RANGE PacketNumbersReceived = new QUIC_RANGE();

        //作用：构造下一条 ACK 帧
        //容量上限：QUIC_MAX_RANGE_ACK_PACKETS（默认 65536 字节）
        //逻辑：只把“最近收到且还没被 ACK 发送出去”的包号保留在这里；一旦 ACK 帧被发送，就把对应区间从本字段里“commit”掉（QuicAckTrackerCommitAck）。
        public readonly QUIC_RANGE PacketNumbersToAck = new QUIC_RANGE();
        public QUIC_ACK_ECN_EX ReceivedECN;
        public long LargestPacketNumberAcknowledged;
        public long LargestPacketNumberRecvTime;
        public int AckElicitingPacketsToAcknowledge; //用途：记录需要确认的触发确认（ACK-eliciting）数据包的数量。
        public bool AlreadyWrittenAckFrame;
        public bool NonZeroRecvECN;

        public void Reset()
        {
            MSQuicFunc.QuicRangeReset(PacketNumbersReceived);
            MSQuicFunc.QuicRangeReset(PacketNumbersToAck);

            ReceivedECN = default;
            LargestPacketNumberAcknowledged = 0;
            LargestPacketNumberRecvTime = 0;
            AckElicitingPacketsToAcknowledge = 0;
            AlreadyWrittenAckFrame = false;
            NonZeroRecvECN = false;
        }
    }

    internal static partial class MSQuicFunc
    {
        static void QuicAckTrackerInitialize(QUIC_ACK_TRACKER Tracker, QUIC_PACKET_SPACE Packets)
        {
            Tracker.CXPLAT_CONTAINING_RECORD = Packets;
            QuicRangeInitialize(QUIC_MAX_RANGE_DUPLICATE_PACKETS, Tracker.PacketNumbersReceived);
            QuicRangeInitialize(QUIC_MAX_RANGE_ACK_PACKETS, Tracker.PacketNumbersToAck);
        }

        static void QuicAckTrackerUninitialize(QUIC_ACK_TRACKER Tracker)
        {
            QuicRangeUninitialize(Tracker.PacketNumbersToAck);
            QuicRangeUninitialize(Tracker.PacketNumbersReceived);
        }

        static void QuicAckTrackerReset(QUIC_ACK_TRACKER Tracker)
        {
            Tracker.AckElicitingPacketsToAcknowledge = 0;
            Tracker.LargestPacketNumberAcknowledged = 0;
            Tracker.LargestPacketNumberRecvTime = 0;
            Tracker.AlreadyWrittenAckFrame = false;
            Tracker.NonZeroRecvECN = false;
            QuicRangeReset(Tracker.PacketNumbersToAck);
            QuicRangeReset(Tracker.PacketNumbersReceived);
        }
        
        //接收到数据包的时候,把 包号 存下来
        static bool QuicAckTrackerAddPacketNumber(QUIC_ACK_TRACKER Tracker, long PacketNumber)
        {
            return QuicRangeAddRange(Tracker.PacketNumbersReceived, PacketNumber, 1, out bool RangeUpdated) == null || !RangeUpdated;
        }

        //ACK确认后,向前进。 
        static void QuicAckTrackerOnAckFrameAcked(QUIC_ACK_TRACKER Tracker, long LargestAckedPacketNumber)
        {
            QUIC_CONNECTION Connection = QuicAckTrackerGetPacketSpace(Tracker).Connection;

            //NetLog.Log($"Tracker.PacketNumbersToAck 0000: {LargestAckedPacketNumber}, {Tracker.PacketNumbersToAck}");
            QuicRangeSetMin(Tracker.PacketNumbersToAck, LargestAckedPacketNumber + 1);
            //NetLog.Log($"Tracker.PacketNumbersToAck 1111:  {Tracker.PacketNumbersToAck}");
            
            if (!QuicAckTrackerHasPacketsToAck(Tracker) && BoolOk(Tracker.AckElicitingPacketsToAcknowledge))
            {
                Tracker.AckElicitingPacketsToAcknowledge = 0;
                QuicSendUpdateAckState(Connection.Send);
            }
        }

        static bool QuicAckTrackerHasPacketsToAck(QUIC_ACK_TRACKER Tracker)
        {
            return !Tracker.AlreadyWrittenAckFrame && QuicRangeSize(Tracker.PacketNumbersToAck) != 0;
        }

        static bool QuicAckTrackerAckFrameEncode(QUIC_ACK_TRACKER Tracker, QUIC_PACKET_BUILDER Builder)
        {
            NetLog.Assert(QuicAckTrackerHasPacketsToAck(Tracker));

            long Timestamp = CxPlatTimeUs();
            long AckDelay = CxPlatTimeDiff(Tracker.LargestPacketNumberRecvTime, Timestamp) >> Builder.Connection.AckDelayExponent;

            QUIC_SSBuffer mBuf = Builder.GetDatagramCanWriteSSBufer();
            if (Builder.Connection.State.TimestampSendNegotiated && Builder.EncryptLevel == QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT)
            {
                QUIC_TIMESTAMP_EX Frame = new QUIC_TIMESTAMP_EX()
                {
                    Timestamp = Timestamp - Builder.Connection.Stats.Timing.Start
                };
                
                if (!QuicTimestampFrameEncode(Frame, ref mBuf))
                {
                    return false;
                }
            }
            

            if (!QuicAckFrameEncode(Tracker.PacketNumbersToAck, AckDelay, Tracker.NonZeroRecvECN ? Tracker.ReceivedECN : default, ref mBuf))
            {
                return false;
            }
            Builder.SetDatagramOffset(mBuf);

            if (BoolOk(Tracker.AckElicitingPacketsToAcknowledge))
            {
                Tracker.AckElicitingPacketsToAcknowledge = 0;
                QuicSendUpdateAckState(Builder.Connection.Send);
            }

            Tracker.AlreadyWrittenAckFrame = true;
            Tracker.LargestPacketNumberAcknowledged = Builder.Metadata.Frames[Builder.Metadata.FrameCount].ACK.LargestAckedPacketNumber = QuicRangeGetMax(Tracker.PacketNumbersToAck);
            QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_ACK, false);
            return true;
        }

        //接收到数据包的时候,把 包号 存下来
        static void QuicAckTrackerAckPacket(QUIC_ACK_TRACKER Tracker, long PacketNumber, long RecvTimeUs, 
            CXPLAT_ECN_TYPE ECN, QUIC_ACK_TYPE AckType)
        {
            //NetLog.Log("发送确认 ACK PacketNumber: " + PacketNumber);
            QUIC_CONNECTION Connection = QuicAckTrackerGetPacketSpace(Tracker).Connection;

            NetLog.Assert(Connection != null);
            NetLog.Assert(PacketNumber <= QUIC_VAR_INT_MAX);

            if (QuicRangeGetMaxSafe(Tracker.PacketNumbersToAck, out long CurLargestPacketNumber) && CurLargestPacketNumber > PacketNumber)
            {
                Connection.Stats.Recv.ReorderedPackets++;//旧的数据包
            }

            if (!QuicRangeAddValue(Tracker.PacketNumbersToAck, PacketNumber))
            {
                QuicConnTransportError(Connection, QUIC_ERROR_INTERNAL_ERROR);
                return;
            }

            bool NewLargestPacketNumber = PacketNumber == QuicRangeGetMax(Tracker.PacketNumbersToAck);
            if (NewLargestPacketNumber)
            {
                Tracker.LargestPacketNumberRecvTime = RecvTimeUs;
            }

            switch (ECN)
            {
                case CXPLAT_ECN_TYPE.CXPLAT_ECN_ECT_1:
                    Tracker.NonZeroRecvECN = true;
                    Tracker.ReceivedECN.ECT_1_Count++;
                    break;
                case CXPLAT_ECN_TYPE.CXPLAT_ECN_ECT_0:
                    Tracker.NonZeroRecvECN = true;
                    Tracker.ReceivedECN.ECT_0_Count++;
                    break;
                case CXPLAT_ECN_TYPE.CXPLAT_ECN_CE:
                    Tracker.NonZeroRecvECN = true;
                    Tracker.ReceivedECN.CE_Count++;
                    break;
                default:
                    break;
            }

            Tracker.AlreadyWrittenAckFrame = false;

            if (AckType == QUIC_ACK_TYPE.QUIC_ACK_TYPE_NON_ACK_ELICITING)
            {
                goto Exit;
            }

            Tracker.AckElicitingPacketsToAcknowledge++;

            if (HasFlag(Connection.Send.SendFlags, QUIC_CONN_SEND_FLAG_ACK))
            {
                goto Exit;
            }

            if (AckType == QUIC_ACK_TYPE.QUIC_ACK_TYPE_ACK_IMMEDIATE || Connection.Settings.MaxAckDelayMs == 0 ||
                ((ulong)Tracker.AckElicitingPacketsToAcknowledge >= Connection.PacketTolerance) ||
                (!Connection.State.IgnoreReordering && (NewLargestPacketNumber && QuicRangeSize(Tracker.PacketNumbersToAck) > 1 &&
                QuicRangeGet(Tracker.PacketNumbersToAck, QuicRangeSize(Tracker.PacketNumbersToAck) - 1).Count == 1)))
            {
                QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_ACK);
            }
            else if (Tracker.AckElicitingPacketsToAcknowledge == 1)
            {
                QuicSendStartDelayedAckTimer(Connection.Send);
            }

        Exit:
            QuicSendValidate(Connection.Send);
        }

    }
}
