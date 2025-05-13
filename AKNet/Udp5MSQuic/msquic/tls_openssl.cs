using AKNet.Common;
using AKNet.Udp5MSQuic.Common;
using System;
using System.Net.Security;

namespace AKNet.Udp5MSQuic.Common
{
    public class CXPLAT_SEC_CONFIG
    {
        
    }

    internal class CXPLAT_TLS
    {
        public CXPLAT_SEC_CONFIG SecConfig;
        public QUIC_HKDF_LABELS HkdfLabels;
        public bool IsServer;
        public bool PeerCertReceived;
        public bool PeerTPReceived;

        public ushort QuicTpExtType;
        public ushort AlpnBufferLength;
        public byte[] AlpnBuffer;
        public byte[] SNI;
        public SslStream Ssl;
        public CXPLAT_TLS_PROCESS_STATE State;
        public uint ResultFlags;
        public QUIC_CONNECTION Connection;
        public QUIC_TLS_SECRETS TlsSecrets;
    }

    internal static partial class MSQuicFunc
    {
        static ulong CxPlatTlsSecConfigCreate(QUIC_CREDENTIAL_CONFIG CredConfig, CXPLAT_TLS_CREDENTIAL_FLAGS TlsCredFlags,
            CXPLAT_TLS_CALLBACKS TlsCallbacks, object Context, CXPLAT_SEC_CONFIG_CREATE_COMPLETE CompletionHandler)
{
    QUIC_CREDENTIAL_FLAGS CredConfigFlags = CredConfig.Flags;

    if (CredConfigFlags & QUIC_CREDENTIAL_FLAG_LOAD_ASYNCHRONOUS &&
        CredConfig->AsyncHandler == NULL) {
        return QUIC_STATUS_INVALID_PARAMETER;
    }

    if (CredConfigFlags & QUIC_CREDENTIAL_FLAG_ENABLE_OCSP ||
        CredConfigFlags & QUIC_CREDENTIAL_FLAG_USE_SUPPLIED_CREDENTIALS ||
        CredConfigFlags & QUIC_CREDENTIAL_FLAG_USE_SYSTEM_MAPPER ||
        CredConfigFlags & QUIC_CREDENTIAL_FLAG_INPROC_PEER_CERTIFICATE) {
        return QUIC_STATUS_NOT_SUPPORTED; // Not supported by this TLS implementation
    }

# ifdef CX_PLATFORM_USES_TLS_BUILTIN_CERTIFICATE
CredConfigFlags |= QUIC_CREDENTIAL_FLAG_USE_TLS_BUILTIN_CERTIFICATE_VALIDATION;
#endif

if ((CredConfigFlags & QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION) &&
    !(CredConfigFlags & QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED))
{
    return QUIC_STATUS_INVALID_PARAMETER; // Defer validation without indication doesn't make sense.
}

if ((CredConfigFlags & QUIC_CREDENTIAL_FLAG_USE_TLS_BUILTIN_CERTIFICATE_VALIDATION) &&
    (CredConfigFlags & QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_END_CERT ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_CHAIN ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_IGNORE_NO_REVOCATION_CHECK ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_IGNORE_REVOCATION_OFFLINE ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_CACHE_ONLY_URL_RETRIEVAL ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_CACHE_ONLY ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_DISABLE_AIA))
{
    return QUIC_STATUS_INVALID_PARAMETER;
}

# ifdef CX_PLATFORM_DARWIN
if (((CredConfigFlags & QUIC_CREDENTIAL_FLAG_USE_TLS_BUILTIN_CERTIFICATE_VALIDATION) == 0) &&
    (CredConfigFlags & QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_END_CERT ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_IGNORE_NO_REVOCATION_CHECK ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_IGNORE_REVOCATION_OFFLINE ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_CACHE_ONLY_URL_RETRIEVAL ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_CACHE_ONLY ||
    CredConfigFlags & QUIC_CREDENTIAL_FLAG_DISABLE_AIA))
{
    return QUIC_STATUS_INVALID_PARAMETER;
}
#endif

if (CredConfig->Reserved != NULL)
{
    return QUIC_STATUS_INVALID_PARAMETER; // Not currently used and should be NULL.
}

if (CredConfig->Type == QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE)
{
    if (CredConfig->CertificateFile == NULL ||
        CredConfig->CertificateFile->CertificateFile == NULL ||
        CredConfig->CertificateFile->PrivateKeyFile == NULL)
    {
        return QUIC_STATUS_INVALID_PARAMETER;
    }
}
else if (CredConfig->Type == QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE_PROTECTED)
{
    if (CredConfig->CertificateFileProtected == NULL ||
        CredConfig->CertificateFileProtected->CertificateFile == NULL ||
        CredConfig->CertificateFileProtected->PrivateKeyFile == NULL ||
        CredConfig->CertificateFileProtected->PrivateKeyPassword == NULL)
    {
        return QUIC_STATUS_INVALID_PARAMETER;
    }
}
else if (CredConfig->Type == QUIC_CREDENTIAL_TYPE_CERTIFICATE_PKCS12)
{
    if (CredConfig->CertificatePkcs12 == NULL ||
        CredConfig->CertificatePkcs12->Asn1Blob == NULL ||
        CredConfig->CertificatePkcs12->Asn1BlobLength == 0)
    {
        return QUIC_STATUS_INVALID_PARAMETER;
    }
}
else if (CredConfig->Type == QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH ||
    CredConfig->Type == QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH_STORE ||
    CredConfig->Type == QUIC_CREDENTIAL_TYPE_CERTIFICATE_CONTEXT)
{ // NOLINT bugprone-branch-clone
# ifndef _WIN32
    return QUIC_STATUS_NOT_SUPPORTED; // Only supported on windows.
#endif
    // Windows parameters checked later
}
else if (CredConfig->Type == QUIC_CREDENTIAL_TYPE_NONE)
{
    if (!(CredConfigFlags & QUIC_CREDENTIAL_FLAG_CLIENT))
    {
        return QUIC_STATUS_INVALID_PARAMETER; // Required for server
    }
}
else
{
    return QUIC_STATUS_NOT_SUPPORTED;
}

if (CredConfigFlags & QUIC_CREDENTIAL_FLAG_SET_ALLOWED_CIPHER_SUITES)
{
    if ((CredConfig->AllowedCipherSuites &
        (QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256 |
        QUIC_ALLOWED_CIPHER_SUITE_AES_256_GCM_SHA384 |
        QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256)) == 0)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            CredConfig->AllowedCipherSuites,
            "No valid cipher suites presented");
        return QUIC_STATUS_INVALID_PARAMETER;
    }
    if (CredConfig->AllowedCipherSuites == QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256 &&
        !CxPlatCryptSupports(CXPLAT_AEAD_CHACHA20_POLY1305))
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            CredConfig->AllowedCipherSuites,
            "Only CHACHA requested but not available");
        return QUIC_STATUS_NOT_SUPPORTED;
    }
}

QUIC_STATUS Status = QUIC_STATUS_SUCCESS;
int Ret = 0;
CXPLAT_SEC_CONFIG* SecurityConfig = NULL;
X509* X509Cert = NULL;
EVP_PKEY* PrivateKey = NULL;
char* CipherSuiteString = NULL;

//
// Create a security config.
//

SecurityConfig = CXPLAT_ALLOC_NONPAGED(sizeof(CXPLAT_SEC_CONFIG), QUIC_POOL_TLS_SECCONF);
if (SecurityConfig == NULL)
{
    QuicTraceEvent(
        AllocFailure,
        "Allocation of '%s' failed. (%llu bytes)",
        "CXPLAT_SEC_CONFIG",
        sizeof(CXPLAT_SEC_CONFIG));
    Status = QUIC_STATUS_OUT_OF_MEMORY;
    goto Exit;
}

CxPlatZeroMemory(SecurityConfig, sizeof(CXPLAT_SEC_CONFIG));
SecurityConfig->Callbacks = *TlsCallbacks;
SecurityConfig->Flags = CredConfigFlags;
SecurityConfig->TlsFlags = TlsCredFlags;

//
// Create the a SSL context for the security config.
//

SecurityConfig->SSLCtx = SSL_CTX_new(TLS_method());
if (SecurityConfig->SSLCtx == NULL)
{
    QuicTraceEvent(
        LibraryErrorStatus,
        "[ lib] ERROR, %u, %s.",
        ERR_get_error(),
        "SSL_CTX_new failed");
    Status = QUIC_STATUS_TLS_ERROR;
    goto Exit;
}

//
// Configure the SSL context with the defaults.
//

Ret = SSL_CTX_set_min_proto_version(SecurityConfig->SSLCtx, TLS1_3_VERSION);
if (Ret != 1)
{
    QuicTraceEvent(
        LibraryErrorStatus,
        "[ lib] ERROR, %u, %s.",
        ERR_get_error(),
        "SSL_CTX_set_min_proto_version failed");
    Status = QUIC_STATUS_TLS_ERROR;
    goto Exit;
}

Ret = SSL_CTX_set_max_proto_version(SecurityConfig->SSLCtx, TLS1_3_VERSION);
if (Ret != 1)
{
    QuicTraceEvent(
        LibraryErrorStatus,
        "[ lib] ERROR, %u, %s.",
        ERR_get_error(),
        "SSL_CTX_set_max_proto_version failed");
    Status = QUIC_STATUS_TLS_ERROR;
    goto Exit;
}

char* CipherSuites = CXPLAT_TLS_DEFAULT_SSL_CIPHERS;
if (CredConfigFlags & QUIC_CREDENTIAL_FLAG_SET_ALLOWED_CIPHER_SUITES)
{
    //
    // Calculate allowed cipher suite string length.
    //
    uint8_t CipherSuiteStringLength = 0;
    uint8_t AllowedCipherSuitesCount = 0;
    if (CredConfig->AllowedCipherSuites & QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256)
    {
        CipherSuiteStringLength += (uint8_t)sizeof(CXPLAT_TLS_AES_128_GCM_SHA256);
        AllowedCipherSuitesCount++;
    }
    if (CredConfig->AllowedCipherSuites & QUIC_ALLOWED_CIPHER_SUITE_AES_256_GCM_SHA384)
    {
        CipherSuiteStringLength += (uint8_t)sizeof(CXPLAT_TLS_AES_256_GCM_SHA384);
        AllowedCipherSuitesCount++;
    }
    if (CredConfig->AllowedCipherSuites & QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256)
    {
        if (AllowedCipherSuitesCount == 0 && !CxPlatCryptSupports(CXPLAT_AEAD_CHACHA20_POLY1305))
        {
            Status = QUIC_STATUS_NOT_SUPPORTED;
            goto Exit;
        }
        CipherSuiteStringLength += (uint8_t)sizeof(CXPLAT_TLS_CHACHA20_POLY1305_SHA256);
        AllowedCipherSuitesCount++;
    }

    CipherSuiteString = CXPLAT_ALLOC_NONPAGED(CipherSuiteStringLength, QUIC_POOL_TLS_CIPHER_SUITE_STRING);
    if (CipherSuiteString == NULL)
    {
        QuicTraceEvent(
            AllocFailure,
            "Allocation of '%s' failed. (%llu bytes)",
            "CipherSuiteString",
            CipherSuiteStringLength);
        Status = QUIC_STATUS_OUT_OF_MEMORY;
        goto Exit;
    }

    //
    // Order of if-statements matters here because OpenSSL uses the order
    // of cipher suites to indicate preference. Below, we use the default
    // order of preference for TLS 1.3 cipher suites.
    //
    uint8_t CipherSuiteStringCursor = 0;
    if (CredConfig->AllowedCipherSuites & QUIC_ALLOWED_CIPHER_SUITE_AES_256_GCM_SHA384)
    {
        CxPlatCopyMemory(
            &CipherSuiteString[CipherSuiteStringCursor],
            CXPLAT_TLS_AES_256_GCM_SHA384,
            sizeof(CXPLAT_TLS_AES_256_GCM_SHA384));
        CipherSuiteStringCursor += (uint8_t)sizeof(CXPLAT_TLS_AES_256_GCM_SHA384);
        if (--AllowedCipherSuitesCount > 0)
        {
            CipherSuiteString[CipherSuiteStringCursor - 1] = ':';
        }
    }
    if (CredConfig->AllowedCipherSuites & QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256)
    {
        CxPlatCopyMemory(
            &CipherSuiteString[CipherSuiteStringCursor],
            CXPLAT_TLS_CHACHA20_POLY1305_SHA256,
            sizeof(CXPLAT_TLS_CHACHA20_POLY1305_SHA256));
        CipherSuiteStringCursor += (uint8_t)sizeof(CXPLAT_TLS_CHACHA20_POLY1305_SHA256);
        if (--AllowedCipherSuitesCount > 0)
        {
            CipherSuiteString[CipherSuiteStringCursor - 1] = ':';
        }
    }
    if (CredConfig->AllowedCipherSuites & QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256)
    {
        CxPlatCopyMemory(
            &CipherSuiteString[CipherSuiteStringCursor],
            CXPLAT_TLS_AES_128_GCM_SHA256,
            sizeof(CXPLAT_TLS_AES_128_GCM_SHA256));
        CipherSuiteStringCursor += (uint8_t)sizeof(CXPLAT_TLS_AES_128_GCM_SHA256);
    }
    CXPLAT_DBG_ASSERT(CipherSuiteStringCursor == CipherSuiteStringLength);
    CipherSuites = CipherSuiteString;
}

Ret =
    SSL_CTX_set_ciphersuites(
        SecurityConfig->SSLCtx,
        CipherSuites);
if (Ret != 1)
{
    QuicTraceEvent(
        LibraryErrorStatus,
        "[ lib] ERROR, %u, %s.",
        ERR_get_error(),
        "SSL_CTX_set_ciphersuites failed");
    Status = QUIC_STATUS_TLS_ERROR;
    goto Exit;
}

if (SecurityConfig->Flags & QUIC_CREDENTIAL_FLAG_USE_TLS_BUILTIN_CERTIFICATE_VALIDATION)
{
    Ret = SSL_CTX_set_default_verify_paths(SecurityConfig->SSLCtx);
    if (Ret != 1)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "SSL_CTX_set_default_verify_paths failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }
}

Ret = SSL_CTX_set_quic_method(SecurityConfig->SSLCtx, &OpenSslQuicCallbacks);
if (Ret != 1)
{
    QuicTraceEvent(
        LibraryErrorStatus,
        "[ lib] ERROR, %u, %s.",
        ERR_get_error(),
        "SSL_CTX_set_quic_method failed");
    Status = QUIC_STATUS_TLS_ERROR;
    goto Exit;
}

if ((CredConfigFlags & QUIC_CREDENTIAL_FLAG_CLIENT) &&
    !(TlsCredFlags & CXPLAT_TLS_CREDENTIAL_FLAG_DISABLE_RESUMPTION))
{
    SSL_CTX_set_session_cache_mode(
        SecurityConfig->SSLCtx,
        SSL_SESS_CACHE_CLIENT | SSL_SESS_CACHE_NO_INTERNAL_STORE);
    SSL_CTX_sess_set_new_cb(
        SecurityConfig->SSLCtx,
        CxPlatTlsOnClientSessionTicketReceived);
}

if (!(CredConfigFlags & QUIC_CREDENTIAL_FLAG_CLIENT))
{
    if (!(TlsCredFlags & CXPLAT_TLS_CREDENTIAL_FLAG_DISABLE_RESUMPTION))
    {
        Ret = SSL_CTX_set_max_early_data(SecurityConfig->SSLCtx, 0xFFFFFFFF);
        if (Ret != 1)
        {
            QuicTraceEvent(
                LibraryErrorStatus,
                "[ lib] ERROR, %u, %s.",
                ERR_get_error(),
                "SSL_CTX_set_max_early_data failed");
            Status = QUIC_STATUS_TLS_ERROR;
            goto Exit;
        }

        Ret = SSL_CTX_set_session_ticket_cb(
            SecurityConfig->SSLCtx,
            NULL,
            CxPlatTlsOnServerSessionTicketDecrypted,
            NULL);
        if (Ret != 1)
        {
            QuicTraceEvent(
                LibraryErrorStatus,
                "[ lib] ERROR, %u, %s.",
                ERR_get_error(),
                "SSL_CTX_set_session_ticket_cb failed");
            Status = QUIC_STATUS_TLS_ERROR;
            goto Exit;
        }
    }

    Ret = SSL_CTX_set_num_tickets(SecurityConfig->SSLCtx, 0);
    if (Ret != 1)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "SSL_CTX_set_num_tickets failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }
}

//
// Set the certs.
//

if (CredConfig->Type == QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE ||
    CredConfig->Type == QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE_PROTECTED)
{

    if (CredConfig->Type == QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE_PROTECTED)
    {
        SSL_CTX_set_default_passwd_cb_userdata(
            SecurityConfig->SSLCtx, (void*)CredConfig->CertificateFileProtected->PrivateKeyPassword);
    }

    Ret =
        SSL_CTX_use_PrivateKey_file(
            SecurityConfig->SSLCtx,
            CredConfig->CertificateFile->PrivateKeyFile,
            SSL_FILETYPE_PEM);
    if (Ret != 1)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "SSL_CTX_use_PrivateKey_file failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }

    Ret =
        SSL_CTX_use_certificate_chain_file(
            SecurityConfig->SSLCtx,
            CredConfig->CertificateFile->CertificateFile);
    if (Ret != 1)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "SSL_CTX_use_certificate_chain_file failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }
}
else if (CredConfig->Type != QUIC_CREDENTIAL_TYPE_NONE)
{
    BIO* Bio = BIO_new(BIO_s_mem());
    PKCS12* Pkcs12 = NULL;
    const char* Password = NULL;
    char PasswordBuffer[PFX_PASSWORD_LENGTH];

    if (!Bio)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "BIO_new failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }

    BIO_set_mem_eof_return(Bio, 0);

    if (CredConfig->Type == QUIC_CREDENTIAL_TYPE_CERTIFICATE_PKCS12)
    {
        Password = CredConfig->CertificatePkcs12->PrivateKeyPassword;
        Ret =
            BIO_write(
                Bio,
                CredConfig->CertificatePkcs12->Asn1Blob,
                CredConfig->CertificatePkcs12->Asn1BlobLength);
        if (Ret < 0)
        {
            QuicTraceEvent(
                LibraryErrorStatus,
                "[ lib] ERROR, %u, %s.",
                ERR_get_error(),
                "BIO_write failed");
            Status = QUIC_STATUS_TLS_ERROR;
            goto Exit;
        }
    }
    else
    {
        uint8_t* PfxBlob = NULL;
        uint32_t PfxSize = 0;
        CxPlatRandom(sizeof(PasswordBuffer), PasswordBuffer);

        //
        // Fixup password to printable characters
        //
        for (uint32_t idx = 0; idx < sizeof(PasswordBuffer); ++idx)
        {
            PasswordBuffer[idx] = ((uint8_t)PasswordBuffer[idx] % 94) + 32;
        }
        PasswordBuffer[PFX_PASSWORD_LENGTH - 1] = 0;
        Password = PasswordBuffer;

        Status =
            CxPlatCertExtractPrivateKey(
                CredConfig,
                PasswordBuffer,
                &PfxBlob,
                &PfxSize);
        if (QUIC_FAILED(Status))
        {
            goto Exit;
        }

        Ret = BIO_write(Bio, PfxBlob, PfxSize);
        CXPLAT_FREE(PfxBlob, QUIC_POOL_TLS_PFX);
        if (Ret < 0)
        {
            QuicTraceEvent(
                LibraryErrorStatus,
                "[ lib] ERROR, %u, %s.",
                ERR_get_error(),
                "BIO_write failed");
            Status = QUIC_STATUS_TLS_ERROR;
            goto Exit;
        }
    }

    Pkcs12 = d2i_PKCS12_bio(Bio, NULL);
    BIO_free(Bio);
    Bio = NULL;

    if (!Pkcs12)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "d2i_PKCS12_bio failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }

    STACK_OF(X509) * CaCertificates = NULL;
    Ret =
        PKCS12_parse(Pkcs12, Password, &PrivateKey, &X509Cert, &CaCertificates);
    if (CaCertificates)
    {
        X509* CaCert;
        while ((CaCert = sk_X509_pop(CaCertificates)) != NULL)
        {
            //
            // This transfers ownership to SSLCtx and CaCert does not need to be freed.
            //
            SSL_CTX_add_extra_chain_cert(SecurityConfig->SSLCtx, CaCert);
        }
        sk_X509_free(CaCertificates);
    }
    if (Pkcs12)
    {
        PKCS12_free(Pkcs12);
    }
    if (Password == PasswordBuffer)
    {
        CxPlatZeroMemory(PasswordBuffer, sizeof(PasswordBuffer));
    }

    if (Ret != 1)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "PKCS12_parse failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }

    Ret =
        SSL_CTX_use_PrivateKey(
            SecurityConfig->SSLCtx,
            PrivateKey);
    if (Ret != 1)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "SSL_CTX_use_PrivateKey_file failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }

    Ret =
        SSL_CTX_use_certificate(
            SecurityConfig->SSLCtx,
            X509Cert);
    if (Ret != 1)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "SSL_CTX_use_certificate failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }
}

if (CredConfig->Type != QUIC_CREDENTIAL_TYPE_NONE)
{
    Ret = SSL_CTX_check_private_key(SecurityConfig->SSLCtx);
    if (Ret != 1)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "SSL_CTX_check_private_key failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }
}

if (CredConfigFlags & QUIC_CREDENTIAL_FLAG_SET_CA_CERTIFICATE_FILE &&
    CredConfig->CaCertificateFile)
{
    Ret =
        SSL_CTX_load_verify_locations(
            SecurityConfig->SSLCtx,
            CredConfig->CaCertificateFile,
            NULL);
    if (Ret != 1)
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            ERR_get_error(),
            "SSL_CTX_load_verify_locations failed");
        Status = QUIC_STATUS_TLS_ERROR;
        goto Exit;
    }
}

if (CredConfigFlags & QUIC_CREDENTIAL_FLAG_CLIENT)
{
    SSL_CTX_set_cert_verify_callback(SecurityConfig->SSLCtx, CxPlatTlsCertificateVerifyCallback, NULL);
    SSL_CTX_set_verify(SecurityConfig->SSLCtx, SSL_VERIFY_PEER, NULL);
    if (!(CredConfigFlags & (QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION | QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION)))
    {
        SSL_CTX_set_verify_depth(SecurityConfig->SSLCtx, CXPLAT_TLS_DEFAULT_VERIFY_DEPTH);
    }

    //
    // TODO - Support additional certificate validation parameters, such as
    // the location of the trusted root CAs (SSL_CTX_load_verify_locations)?
    //

}
else
{
    SSL_CTX_set_options(
        SecurityConfig->SSLCtx,
        (SSL_OP_ALL & ~SSL_OP_DONT_INSERT_EMPTY_FRAGMENTS) |
        SSL_OP_SINGLE_ECDH_USE |
        SSL_OP_CIPHER_SERVER_PREFERENCE |
        SSL_OP_NO_ANTI_REPLAY);
    SSL_CTX_clear_options(SecurityConfig->SSLCtx, SSL_OP_ENABLE_MIDDLEBOX_COMPAT);
    SSL_CTX_set_mode(SecurityConfig->SSLCtx, SSL_MODE_RELEASE_BUFFERS);

    if (CredConfigFlags & QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED ||
        CredConfigFlags & QUIC_CREDENTIAL_FLAG_REQUIRE_CLIENT_AUTHENTICATION)
    {
        SSL_CTX_set_cert_verify_callback(
            SecurityConfig->SSLCtx,
            CxPlatTlsCertificateVerifyCallback,
            NULL);
    }

    if (CredConfigFlags & QUIC_CREDENTIAL_FLAG_REQUIRE_CLIENT_AUTHENTICATION)
    {
        int VerifyMode = SSL_VERIFY_PEER;
        if (!(CredConfigFlags & QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION))
        {
            SSL_CTX_set_verify_depth(
                SecurityConfig->SSLCtx,
                CXPLAT_TLS_DEFAULT_VERIFY_DEPTH);
        }
        if (!(CredConfigFlags & (QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION |
            QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION)))
        {
            VerifyMode |= SSL_VERIFY_FAIL_IF_NO_PEER_CERT;
        }
        SSL_CTX_set_verify(
            SecurityConfig->SSLCtx,
            VerifyMode,
            NULL);
    }

    SSL_CTX_set_alpn_select_cb(SecurityConfig->SSLCtx, CxPlatTlsAlpnSelectCallback, NULL);

    SSL_CTX_set_max_early_data(SecurityConfig->SSLCtx, UINT32_MAX);
    SSL_CTX_set_client_hello_cb(SecurityConfig->SSLCtx, CxPlatTlsClientHelloCallback, NULL);
}

//
// Invoke completion inline.
//

CompletionHandler(CredConfig, Context, Status, SecurityConfig);
SecurityConfig = NULL;

if (CredConfigFlags & QUIC_CREDENTIAL_FLAG_LOAD_ASYNCHRONOUS)
{
    Status = QUIC_STATUS_PENDING;
}
else
{
    Status = QUIC_STATUS_SUCCESS;
}

Exit:

if (SecurityConfig != NULL)
{
    CxPlatTlsSecConfigDelete(SecurityConfig);
}

if (CipherSuiteString != NULL)
{
    CxPlatFree(CipherSuiteString, QUIC_POOL_TLS_CIPHER_SUITE_STRING);
}

if (X509Cert != NULL)
{
    X509_free(X509Cert);
}

if (PrivateKey != NULL)
{
    EVP_PKEY_free(PrivateKey);
}

return Status;
}

        static ulong CxPlatTlsInitialize(CXPLAT_TLS_CONFIG Config, CXPLAT_TLS_PROCESS_STATE State, CXPLAT_TLS NewTlsContext)
        {
            return 0;
        }

        static void CxPlatTlsUninitialize(CXPLAT_TLS TlsContext)
        {
            if (TlsContext != null)
            {
                
            }
        }

        static bool QuicTlsPopulateOffloadKeys(CXPLAT_TLS TlsContext, QUIC_PACKET_KEY PacketKey, string SecretName, CXPLAT_QEO_CONNECTION Offload)
        {
            ulong Status = QuicPacketKeyDeriveOffload(
                    TlsContext.HkdfLabels,
                    PacketKey,
                    SecretName,
                    Offload);
            if (!QUIC_SUCCEEDED(Status))
            {
                goto Error;
            }

        Error:
            return QUIC_SUCCEEDED(Status);
        }

        static uint CxPlatTlsProcessData(CXPLAT_TLS TlsContext, CXPLAT_TLS_DATA_TYPE DataType, QUIC_BUFFER Buffer, CXPLAT_TLS_PROCESS_STATE State)
        {
            NetLog.Assert(Buffer != null || Buffer.Length == 0);

            //    TlsContext.State = State;
            //    TlsContext.ResultFlags = 0;
            //    if (DataType == CXPLAT_TLS_DATA_TYPE.CXPLAT_TLS_TICKET_DATA)
            //    {
            //        SslStream Session = TlsContext.Ssl;
            //        if (Session == null)
            //        {
            //            TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //            goto Exit;
            //        }

            //        try
            //        {
            //            int bytesRead = Session.Read(Buffer, 0, BufferLength);
            //        }
            //        catch (Exception)
            //        {
            //            TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //            goto Exit;
            //        }

            //        if (!SSL_new_session_ticket(TlsContext->Ssl))
            //        {
            //            TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //            goto Exit;
            //        }

            //        int Ret = SSL_do_handshake(TlsContext->Ssl);
            //        if (Ret != 1)
            //        {
            //            TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //            goto Exit;
            //        }

            //        goto Exit;
            //    }

            //    if (BufferLength != 0)
            //    {
            //        QuicTraceLogConnVerbose(
            //            OpenSslProcessData,
            //            TlsContext->Connection,
            //            "Processing %u received bytes",
            //            *BufferLength);

            //        if (SSL_provide_quic_data(
            //                TlsContext->Ssl,
            //                (OSSL_ENCRYPTION_LEVEL)TlsContext->State->ReadKey,
            //                Buffer,
            //                *BufferLength) != 1)
            //        {
            //            char buf[256];
            //            QuicTraceLogConnError(
            //                OpenSslQuicDataErrorStr,
            //                TlsContext->Connection,
            //                "SSL_provide_quic_data failed: %s",
            //                ERR_error_string(ERR_get_error(), buf));
            //            TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //            goto Exit;
            //        }
            //    }

            //    if (!State.HandshakeComplete)
            //    {
            //        int Ret = SSL_do_handshake(TlsContext->Ssl);
            //        if (Ret <= 0)
            //        {
            //            int Err = SSL_get_error(TlsContext->Ssl, Ret);
            //            switch (Err)
            //            {
            //                case SSL_ERROR_WANT_READ:
            //                case SSL_ERROR_WANT_WRITE:
            //                    if (!TlsContext->IsServer && TlsContext->PeerTPReceived == FALSE)
            //                    {
            //                        const uint8_t* TransportParams;
            //                        size_t TransportParamLen;
            //                        SSL_get_peer_quic_transport_params(
            //                                TlsContext->Ssl, &TransportParams, &TransportParamLen);
            //                        if (TransportParams != NULL && TransportParamLen != 0)
            //                        {
            //                            TlsContext->PeerTPReceived = TRUE;
            //                            if (!TlsContext->SecConfig->Callbacks.ReceiveTP(
            //                                    TlsContext->Connection,
            //                                    (uint16_t)TransportParamLen,
            //                                    TransportParams))
            //                            {
            //                                TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //                                goto Exit;
            //                            }
            //                        }
            //                    }
            //                    goto Exit;

            //                case SSL_ERROR_SSL:
            //                    {
            //                        char buf[256];
            //                        const char* file;
            //                        int line;
            //                        QuicTraceLogConnError(
            //                            OpenSslHandshakeErrorStr,
            //                            TlsContext->Connection,
            //                            "TLS handshake error: %s, file:%s:%d",
            //                            buf,
            //                            (strlen(file) > OpenSslFilePrefixLength ? file + OpenSslFilePrefixLength : file),
            //                            line);
            //                        TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //                        goto Exit;
            //                    }

            //                default:
            //                    QuicTraceLogConnError(
            //                        OpenSslHandshakeError,
            //                        TlsContext->Connection,
            //                        "TLS handshake error: %d",
            //                        Err);
            //                    TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //                    goto Exit;
            //            }
            //        }

            //        if (!TlsContext->IsServer)
            //        {
            //            const uint8_t* NegotiatedAlpn;
            //            uint32_t NegotiatedAlpnLength;
            //            SSL_get0_alpn_selected(TlsContext->Ssl, &NegotiatedAlpn, &NegotiatedAlpnLength);
            //            if (NegotiatedAlpnLength == 0)
            //            {
            //                QuicTraceLogConnError(
            //                    OpenSslAlpnNegotiationFailure,
            //                    TlsContext->Connection,
            //                    "Failed to negotiate ALPN");
            //                TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //                goto Exit;
            //            }
            //            if (NegotiatedAlpnLength > UINT8_MAX)
            //            {
            //                QuicTraceLogConnError(
            //                    OpenSslInvalidAlpnLength,
            //                    TlsContext->Connection,
            //                    "Invalid negotiated ALPN length");
            //                TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //                goto Exit;
            //            }
            //            TlsContext->State->NegotiatedAlpn =
            //                CxPlatTlsAlpnFindInList(
            //                    TlsContext->AlpnBufferLength,
            //                    TlsContext->AlpnBuffer,
            //                    (uint8_t)NegotiatedAlpnLength,
            //                    NegotiatedAlpn);
            //            if (TlsContext->State->NegotiatedAlpn == NULL)
            //            {
            //                QuicTraceLogConnError(
            //                    OpenSslNoMatchingAlpn,
            //                    TlsContext->Connection,
            //                    "Failed to find a matching ALPN");
            //                TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //                goto Exit;
            //            }
            //        }
            //        else if ((TlsContext->SecConfig->Flags & QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED) &&
            //            !TlsContext->PeerCertReceived)
            //        {
            //            QUIC_STATUS ValidationResult =
            //                (!(TlsContext->SecConfig->Flags & QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION) &&
            //                (TlsContext->SecConfig->Flags & QUIC_CREDENTIAL_FLAG_REQUIRE_CLIENT_AUTHENTICATION ||
            //                TlsContext->SecConfig->Flags & QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION)) ?
            //                    QUIC_STATUS_CERT_NO_CERT :
            //                    QUIC_STATUS_SUCCESS;

            //            if (!TlsContext->SecConfig->Callbacks.CertificateReceived(
            //                    TlsContext->Connection,
            //                    NULL,
            //                    NULL,
            //                    0,
            //                    ValidationResult))
            //            {
            //                QuicTraceEvent(
            //                    TlsError,
            //                    "[ tls][%p] ERROR, %s.",
            //                    TlsContext->Connection,
            //                    "Indicate null certificate received failed");
            //                TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //                TlsContext->State->AlertCode = CXPLAT_TLS_ALERT_CODE_REQUIRED_CERTIFICATE;
            //                goto Exit;
            //            }
            //        }

            //        State.HandshakeComplete = true;
            //        if (SSL_session_reused(TlsContext->Ssl))
            //        {
            //            QuicTraceLogConnInfo(
            //                OpenSslHandshakeResumed,
            //                TlsContext->Connection,
            //                "TLS Handshake resumed");
            //            State->SessionResumed = TRUE;
            //        }
            //        if (!TlsContext->IsServer)
            //        {
            //            int EarlyDataStatus = SSL_get_early_data_status(TlsContext->Ssl);
            //            if (EarlyDataStatus == SSL_EARLY_DATA_ACCEPTED)
            //            {
            //                State->EarlyDataState = CXPLAT_TLS_EARLY_DATA_ACCEPTED;
            //                TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_EARLY_DATA_ACCEPT;

            //            }
            //            else if (EarlyDataStatus == SSL_EARLY_DATA_REJECTED)
            //            {
            //                State->EarlyDataState = CXPLAT_TLS_EARLY_DATA_REJECTED;
            //                TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_EARLY_DATA_REJECT;
            //            }
            //        }
            //        TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_HANDSHAKE_COMPLETE;

            //        if (TlsContext->IsServer)
            //        {
            //            TlsContext->State->ReadKey = QUIC_PACKET_KEY_1_RTT;
            //            TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_READ_KEY_UPDATED;
            //        }
            //        else if (!TlsContext->PeerTPReceived)
            //        {
            //            const uint8_t* TransportParams;
            //            size_t TransportParamLen;
            //            SSL_get_peer_quic_transport_params(
            //                    TlsContext->Ssl, &TransportParams, &TransportParamLen);
            //            if (TransportParams == NULL || TransportParamLen == 0)
            //            {
            //                QuicTraceLogConnError(
            //                    OpenSslMissingTransportParameters,
            //                    TlsContext->Connection,
            //                    "No transport parameters received");
            //                TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //                goto Exit;
            //            }
            //            TlsContext->PeerTPReceived = TRUE;
            //            if (!TlsContext->SecConfig->Callbacks.ReceiveTP(
            //                    TlsContext->Connection,
            //                    (uint16_t)TransportParamLen,
            //                    TransportParams))
            //            {
            //                TlsContext->ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //                goto Exit;
            //            }
            //        }

            //    }
            //    else
            //    {
            //        if (SSL_process_quic_post_handshake(TlsContext.Ssl) != 1)
            //        {
            //            TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            //            goto Exit;
            //        }
            //    }

            //Exit:
            //    if (!BoolOk(TlsContext.ResultFlags & CXPLAT_TLS_RESULT_ERROR))
            //    {
            //        if (State.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE] != null && State.BufferOffsetHandshake == 0)
            //        {
            //            State.BufferOffsetHandshake = State.BufferTotalLength;
            //        }
            //        if (State.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT] != null && State.BufferOffset1Rtt == 0)
            //        {
            //            State.BufferOffset1Rtt = State.BufferTotalLength;
            //        }
            //    }

            return TlsContext.ResultFlags;
        }

        static void CxPlatTlsUpdateHkdfLabels(CXPLAT_TLS TlsContext, QUIC_HKDF_LABELS Labels)
        {
            TlsContext.HkdfLabels = Labels;
        }
    }
}
