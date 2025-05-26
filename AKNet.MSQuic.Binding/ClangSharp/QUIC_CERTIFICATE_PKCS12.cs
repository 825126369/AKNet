namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_CERTIFICATE_PKCS12
    {
        [NativeTypeName("const uint8_t *")]
        public byte* Asn1Blob;

        [NativeTypeName("uint32_t")]
        public uint Asn1BlobLength;

        [NativeTypeName("const char *")]
        public sbyte* PrivateKeyPassword;
    }
}
