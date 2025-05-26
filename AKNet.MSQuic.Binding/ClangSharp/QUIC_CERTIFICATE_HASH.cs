namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_CERTIFICATE_HASH
    {
        [NativeTypeName("uint8_t[20]")]
        public fixed byte ShaHash[20];
    }
}
