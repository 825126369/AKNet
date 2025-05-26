namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_SCHANNEL_CONTEXT_ATTRIBUTE_W
    {
        [NativeTypeName("unsigned long")]
        public uint Attribute;

        public void* Buffer;
    }
}
