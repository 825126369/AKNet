namespace AKNet.MSQuicWrapper
{
    public enum QUIC_RECEIVE_FLAGS
    {
        QUIC_RECEIVE_FLAG_NONE = 0x0000,
        QUIC_RECEIVE_FLAG_0_RTT = 0x0001,
        QUIC_RECEIVE_FLAG_FIN = 0x0002,
    }
}
