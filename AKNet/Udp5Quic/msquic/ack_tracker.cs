using System.Diagnostics;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_ACK_TRACKER
    {
        public QUIC_RANGE PacketNumbersReceived;
        public QUIC_RANGE PacketNumbersToAck;
        public QUIC_ACK_ECN_EX ReceivedECN;
        public ulong LargestPacketNumberAcknowledged;
        public ulong LargestPacketNumberRecvTime;
        public ushort AckElicitingPacketsToAcknowledge;
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
    }
}
