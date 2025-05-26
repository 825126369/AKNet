namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_SCHANNEL_CONTEXT_ATTRIBUTE_EX_W
    {
        [NativeTypeName("unsigned long")]
        public uint Attribute;

        [NativeTypeName("unsigned long")]
        public uint BufferLength;

        public void* Buffer;
    }
}
