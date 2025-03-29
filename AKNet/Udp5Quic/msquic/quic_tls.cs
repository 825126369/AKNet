namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_TLS_PROCESS_STATE
    {
        public bool HandshakeComplete;
        public bool SessionResumed;
        public CXPLAT_TLS_EARLY_DATA_STATE EarlyDataState;
        public QUIC_PACKET_KEY_TYPE ReadKey;
        public QUIC_PACKET_KEY_TYPE WriteKey;
        public ushort AlertCode;
        public int BufferLength;
        public int BufferAllocLength;
        public int BufferTotalLength;

        //
        // The absolute offset of the start of handshake data. A value of 0
        // indicates 'unset'.
        //
        uint32_t BufferOffsetHandshake;

        //
        // The absolute offset of the start of 1-RTT data. A value of 0 indicates
        // 'unset'.
        //
        uint32_t BufferOffset1Rtt;

        //
        // Holds the TLS data to be sent. Use CXPLAT_ALLOC_NONPAGED and CXPLAT_FREE
        // to allocate and free the memory.
        //
        uint8_t* Buffer;

        //
        // A small buffer to hold the final negotiated ALPN of the connection,
        // assuming it fits in TLS_SMALL_ALPN_BUFFER_SIZE bytes. NegotiatedAlpn
        // with either point to this, or point to allocated memory.
        //
        uint8_t SmallAlpnBuffer[TLS_SMALL_ALPN_BUFFER_SIZE];

        //
        // The final negotiated ALPN of the connection. The first byte is the length
        // followed by that many bytes for actual ALPN.
        //
        const uint8_t* NegotiatedAlpn;

        //
        // All the keys available for decrypting packets with.
        //
        QUIC_PACKET_KEY* ReadKeys[QUIC_PACKET_KEY_COUNT];

        //
        // All the keys available for encrypting packets with.
        //
        QUIC_PACKET_KEY* WriteKeys[QUIC_PACKET_KEY_COUNT];

        //
        // (Server-Connection-Only) ClientAlpnList cache params (in TLS format)
        //
        const uint8_t* ClientAlpnList;
        uint16_t ClientAlpnListLength;

    }
    CXPLAT_TLS_PROCESS_STATE;
}
