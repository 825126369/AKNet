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
}
