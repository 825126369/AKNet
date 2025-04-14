using AKNet.Common;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_ACK_TRACKER
    {
        public QUIC_RANGE PacketNumbersReceived;
        public QUIC_RANGE PacketNumbersToAck;
        public QUIC_ACK_ECN_EX ReceivedECN;
        public ulong LargestPacketNumberAcknowledged;
        public ulong LargestPacketNumberRecvTime;
        public bool AckElicitingPacketsToAcknowledge;
        public bool AlreadyWrittenAckFrame;
        public bool NonZeroRecvECN;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicAckTrackerInitialize(QUIC_ACK_TRACKER Tracker)
        {
            QuicRangeInitialize(
            QUIC_MAX_RANGE_DUPLICATE_PACKETS, Tracker.PacketNumbersReceived);
            QuicRangeInitialize(
            QUIC_MAX_RANGE_ACK_PACKETS, Tracker.PacketNumbersToAck);
        }

        static void QuicAckTrackerUninitialize(QUIC_ACK_TRACKER Tracker)
        {
            QuicRangeUninitialize(Tracker.PacketNumbersToAck);
            QuicRangeUninitialize(Tracker.PacketNumbersReceived);
        }

        static void QuicAckTrackerReset(QUIC_ACK_TRACKER Tracker)
        {
            Tracker.AckElicitingPacketsToAcknowledge = false;
            Tracker.LargestPacketNumberAcknowledged = 0;
            Tracker.LargestPacketNumberRecvTime = 0;
            Tracker.AlreadyWrittenAckFrame = false;
            Tracker.NonZeroRecvECN = false;
           // CxPlatZeroMemory(Tracker.ReceivedECN, sizeof(Tracker->ReceivedECN));
            QuicRangeReset(Tracker.PacketNumbersToAck);
            QuicRangeReset(Tracker.PacketNumbersReceived);
        }

        static bool QuicAckTrackerAddPacketNumber(QUIC_ACK_TRACKER Tracker, ulong PacketNumber)
        {
            bool RangeUpdated = false;
            return QuicRangeAddRange(Tracker.PacketNumbersReceived, PacketNumber, 1, ref RangeUpdated) == null || !RangeUpdated;
        }

        static void QuicAckTrackerOnAckFrameAcked(QUIC_ACK_TRACKER Tracker, ulong LargestAckedPacketNumber)
        {
            QUIC_CONNECTION Connection = QuicAckTrackerGetPacketSpace(Tracker).Connection;

            QuicRangeSetMin(Tracker.PacketNumbersToAck, LargestAckedPacketNumber + 1);

            if (!QuicAckTrackerHasPacketsToAck(Tracker) &&
                Tracker.AckElicitingPacketsToAcknowledge)
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

            ulong Timestamp = CxPlatTime();
            ulong AckDelay = CxPlatTimeDiff64(Tracker.LargestPacketNumberRecvTime, Timestamp) >> Builder.Connection.AckDelayExponent;

            if (Builder.Connection.State.TimestampSendNegotiated && Builder.EncryptLevel == QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT)
            {
                QUIC_TIMESTAMP_EX Frame = new QUIC_TIMESTAMP_EX(){Timestamp = Timestamp - Builder.Connection.Stats.Timing.Start };
                if (!QuicTimestampFrameEncode(Frame,ref Builder.DatagramLength, Builder.Datagram.Length - Builder.EncryptionOverhead, Builder.Datagram.Buffer))
                {
                    return false;
                }
            }

            if (!QuicAckFrameEncode(Tracker.PacketNumbersToAck, AckDelay, Tracker.NonZeroRecvECN ? Tracker.ReceivedECN : null, ref Builder.DatagramLength,
                    Builder.Datagram.Length - Builder.EncryptionOverhead, Builder.Datagram.Buffer))
            {
                return false;
            }

            if (Tracker.AckElicitingPacketsToAcknowledge)
            {
                Tracker.AckElicitingPacketsToAcknowledge = false;
                QuicSendUpdateAckState(Builder.Connection.Send);
            }

            Tracker.AlreadyWrittenAckFrame = true;
            Tracker.LargestPacketNumberAcknowledged = Builder.Metadata.Frames[Builder.Metadata.FrameCount].ACK.LargestAckedPacketNumber = QuicRangeGetMax(Tracker.PacketNumbersToAck);
            QuicPacketBuilderAddFrame(Builder, QUIC_FRAME_TYPE.QUIC_FRAME_ACK, false);
            return true;
        }

    }
}
