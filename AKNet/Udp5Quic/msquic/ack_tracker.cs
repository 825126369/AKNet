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
            Tracker.AckElicitingPacketsToAcknowledge = 0;
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
            bool RangeUpdated;
            return QuicRangeAddRange(Tracker.PacketNumbersReceived, PacketNumber, 1, RangeUpdated) == null || !RangeUpdated;
        }

        static void QuicAckTrackerOnAckFrameAcked(QUIC_ACK_TRACKER Tracker, ulong LargestAckedPacketNumber)
        {
            QUIC_CONNECTION Connection = QuicAckTrackerGetPacketSpace(Tracker).Connection;

            QuicRangeSetMin(Tracker.PacketNumbersToAck, LargestAckedPacketNumber + 1);

            if (!QuicAckTrackerHasPacketsToAck(Tracker) &&
                Tracker->AckElicitingPacketsToAcknowledge)
            {
                //
                // If we received packets out of order and ended up sending an ACK for
                // larger packet numbers before receiving the smaller ones, it's
                // possible we will remove all the ACK ranges even though we haven't
                // acknowledged the smaller one yet. In that case, we need to make sure
                // have all other state match up to the ranges.
                //
                Tracker->AckElicitingPacketsToAcknowledge = 0;
                QuicSendUpdateAckState(&Connection->Send);
            }
        }
    }
}
