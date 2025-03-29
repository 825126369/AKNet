namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_LOSS_DETECTION
    {
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
}
