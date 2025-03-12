namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_PACKET_KEY_TYPE
    {
        QUIC_PACKET_KEY_INITIAL,
        QUIC_PACKET_KEY_0_RTT,
        QUIC_PACKET_KEY_HANDSHAKE,
        QUIC_PACKET_KEY_1_RTT,
        QUIC_PACKET_KEY_1_RTT_OLD,
        QUIC_PACKET_KEY_1_RTT_NEW,
        QUIC_PACKET_KEY_COUNT
    }
}
