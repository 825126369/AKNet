namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_CERTIFICATE_FILE_PROTECTED
    {
        [NativeTypeName("const char *")]
        public sbyte* PrivateKeyFile;

        [NativeTypeName("const char *")]
        public sbyte* CertificateFile;

        [NativeTypeName("const char *")]
        public sbyte* PrivateKeyPassword;
    }
}
