namespace AKNet.MSQuicWrapper
{
    public partial struct QUIC_LISTENER_STATISTICS
    {
        [NativeTypeName("uint64_t")]
        public ulong TotalAcceptedConnections;

        [NativeTypeName("uint64_t")]
        public ulong TotalRejectedConnections;

        [NativeTypeName("uint64_t")]
        public ulong BindingRecvDroppedPackets;
    }
}
