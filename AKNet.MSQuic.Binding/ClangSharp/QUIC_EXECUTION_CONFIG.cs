namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_EXECUTION_CONFIG
    {
        public QUIC_EXECUTION_CONFIG_FLAGS Flags;

        [NativeTypeName("uint32_t")]
        public uint PollingIdleTimeoutUs;

        [NativeTypeName("uint32_t")]
        public uint ProcessorCount;

        [NativeTypeName("uint16_t[1]")]
        public fixed ushort ProcessorList[1];
    }
}
