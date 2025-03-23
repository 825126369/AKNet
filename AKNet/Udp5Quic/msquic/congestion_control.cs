namespace AKNet.Udp5Quic.Common
{
    internal struct QUIC_ACK_EVENT
    {
        public long TimeNow;
        public long LargestAck;
        public long LargestSentPacketNumber;
        public long NumTotalAckedRetransmittableBytes;
        public uint NumRetransmittableBytes;
        public QUIC_SENT_PACKET_METADATA AckedPackets;
        public long SmoothedRtt;
        public long MinRtt;
        public long OneWayDelay;
        public long AdjustedAckTime;
        public bool IsImplicit;
        public bool HasLoss;
        public bool IsLargestAckedPacketAppLimited;
        public bool MinRttValid;
    }

    internal class QUIC_CONGESTION_CONTROL
    {
        public string Name;
        public delegate bool QuicCongestionControlCanSend(QUIC_CONGESTION_CONTROL Cc);
        public delegate void QuicCongestionControlSetExemption(QUIC_CONGESTION_CONTROL Cc, byte NumPackets);
        public delegate void QuicCongestionControlReset(QUIC_CONGESTION_CONTROL Cc, bool FullReset);
        public delegate uint QuicCongestionControlGetSendAllowance(QUIC_CONGESTION_CONTROL Cc, long TimeSinceLastSend, bool TimeSinceLastSendValid);
        public delegate uint QuicCongestionControlOnDataSent(QUIC_CONGESTION_CONTROL Cc, uint NumRetransmittableBytes);
        public delegate bool QuicCongestionControlOnDataInvalidated(QUIC_CONGESTION_CONTROL Cc, uint NumRetransmittableBytes);
        public delegate bool QuicCongestionControlOnDataAcknowledged(QUIC_CONGESTION_CONTROL Cc, QUIC_ACK_EVENT AckEvent);

        void (* QuicCongestionControlOnDataLost) (
            _In_ struct QUIC_CONGESTION_CONTROL* Cc,
        _In_ const QUIC_LOSS_EVENT* LossEvent
        );

        void (* QuicCongestionControlOnEcn) (
            _In_ struct QUIC_CONGESTION_CONTROL* Cc,
        _In_ const QUIC_ECN_EVENT* LossEvent
        );

        BOOLEAN(*QuicCongestionControlOnSpuriousCongestionEvent)(
            _In_ struct QUIC_CONGESTION_CONTROL* Cc
        );

    void (* QuicCongestionControlLogOutFlowStatus) (
        _In_ const struct QUIC_CONGESTION_CONTROL* Cc
        );

    uint8_t(*QuicCongestionControlGetExemptions)(
        _In_ const struct QUIC_CONGESTION_CONTROL* Cc
        );

    uint32_t(*QuicCongestionControlGetBytesInFlightMax)(
        _In_ const struct QUIC_CONGESTION_CONTROL* Cc
        );

    uint32_t(*QuicCongestionControlGetCongestionWindow)(
        _In_ const struct QUIC_CONGESTION_CONTROL* Cc
        );

    BOOLEAN(*QuicCongestionControlIsAppLimited)(
        _In_ const struct QUIC_CONGESTION_CONTROL* Cc
        );

    void (* QuicCongestionControlSetAppLimited) (
        _In_ struct QUIC_CONGESTION_CONTROL* Cc
        );

    //
    // Algorithm specific state.
    //
    union {
        QUIC_CONGESTION_CONTROL_CUBIC Cubic;
        QUIC_CONGESTION_CONTROL_BBR Bbr;
    };

}

}
