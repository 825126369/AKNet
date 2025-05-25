using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

public unsafe partial struct QUIC_CREDENTIAL_CONFIG
{
    public QUIC_CREDENTIAL_TYPE Type;

    public QUIC_CREDENTIAL_FLAGS Flags;

    [NativeTypeName("__AnonymousRecord_msquic_L345_C5")]
    public _Anonymous_e__Union Anonymous;

    [NativeTypeName("const char *")]
    public sbyte* Principal;

    public void* Reserved;

    [NativeTypeName("QUIC_CREDENTIAL_LOAD_COMPLETE_HANDLER")]
    public IntPtr AsyncHandler;

    public QUIC_ALLOWED_CIPHER_SUITE_FLAGS AllowedCipherSuites;

    [NativeTypeName("const char *")]
    public sbyte* CaCertificateFile;

    public ref QUIC_CERTIFICATE_HASH* CertificateHash
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            fixed (_Anonymous_e__Union* pField = &Anonymous)
            {
                return ref pField->CertificateHash;
            }
        }
    }

    public ref QUIC_CERTIFICATE_HASH_STORE* CertificateHashStore
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            fixed (_Anonymous_e__Union* pField = &Anonymous)
            {
                return ref pField->CertificateHashStore;
            }
        }
    }

    public ref void* CertificateContext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            fixed (_Anonymous_e__Union* pField = &Anonymous)
            {
                return ref pField->CertificateContext;
            }
        }
    }

    public ref QUIC_CERTIFICATE_FILE* CertificateFile
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            fixed (_Anonymous_e__Union* pField = &Anonymous)
            {
                return ref pField->CertificateFile;
            }
        }
    }

    public ref QUIC_CERTIFICATE_FILE_PROTECTED* CertificateFileProtected
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            fixed (_Anonymous_e__Union* pField = &Anonymous)
            {
                return ref pField->CertificateFileProtected;
            }
        }
    }

    public ref QUIC_CERTIFICATE_PKCS12* CertificatePkcs12
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            fixed (_Anonymous_e__Union* pField = &Anonymous)
            {
                return ref pField->CertificatePkcs12;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public QUIC_CERTIFICATE_HASH* CertificateHash;

        [FieldOffset(0)]
        public QUIC_CERTIFICATE_HASH_STORE* CertificateHashStore;

        [FieldOffset(0)]
        [NativeTypeName("QUIC_CERTIFICATE *")]
        public void* CertificateContext;

        [FieldOffset(0)]
        public QUIC_CERTIFICATE_FILE* CertificateFile;

        [FieldOffset(0)]
        public QUIC_CERTIFICATE_FILE_PROTECTED* CertificateFileProtected;

        [FieldOffset(0)]
        public QUIC_CERTIFICATE_PKCS12* CertificatePkcs12;
    }
}
