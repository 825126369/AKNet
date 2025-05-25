namespace AKNet.MSQuicWrapper;

public unsafe partial struct QUIC_CERTIFICATE_FILE
{
    [NativeTypeName("const char *")]
    public sbyte* PrivateKeyFile;

    [NativeTypeName("const char *")]
    public sbyte* CertificateFile;
}
