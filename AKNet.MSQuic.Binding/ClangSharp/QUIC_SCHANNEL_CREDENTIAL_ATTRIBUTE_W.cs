namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_SCHANNEL_CREDENTIAL_ATTRIBUTE_W
    {
        [NativeTypeName("unsigned long")]
        public uint Attribute;

        [NativeTypeName("unsigned long")]
        public uint BufferLength;

        public void* Buffer;
    }
}
