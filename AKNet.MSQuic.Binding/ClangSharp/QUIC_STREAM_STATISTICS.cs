namespace AKNet.MSQuicWrapper;

public partial struct QUIC_STREAM_STATISTICS
{
    [NativeTypeName("uint64_t")]
    public ulong ConnBlockedBySchedulingUs;

    [NativeTypeName("uint64_t")]
    public ulong ConnBlockedByPacingUs;

    [NativeTypeName("uint64_t")]
    public ulong ConnBlockedByAmplificationProtUs;

    [NativeTypeName("uint64_t")]
    public ulong ConnBlockedByCongestionControlUs;

    [NativeTypeName("uint64_t")]
    public ulong ConnBlockedByFlowControlUs;

    [NativeTypeName("uint64_t")]
    public ulong StreamBlockedByIdFlowControlUs;

    [NativeTypeName("uint64_t")]
    public ulong StreamBlockedByFlowControlUs;

    [NativeTypeName("uint64_t")]
    public ulong StreamBlockedByAppUs;
}
