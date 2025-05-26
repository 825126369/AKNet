namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_REGISTRATION_CONFIG
    {
        [NativeTypeName("const char *")]
        public sbyte* AppName;

        public QUIC_EXECUTION_PROFILE ExecutionProfile;
    }
}
