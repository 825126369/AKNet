namespace AKNet.MSQuicWrapper
{
    public enum QUIC_STREAM_OPEN_FLAGS
    {
        QUIC_STREAM_OPEN_FLAG_NONE = 0x0000,
        QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL = 0x0001,
        QUIC_STREAM_OPEN_FLAG_0_RTT = 0x0002,
        QUIC_STREAM_OPEN_FLAG_DELAY_ID_FC_UPDATES = 0x0004,
    }
}
