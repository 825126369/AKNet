using AKNet.BoringSSL;
using AKNet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace AKNet.Udp5MSQuic.Common
{
    internal class CXPLAT_SEC_CONFIG
    {
        public IntPtr SSLCtx;
        public QUIC_TICKET_KEY_CONFIG TicketKey;
        public CXPLAT_TLS_CALLBACKS Callbacks;
        public QUIC_CREDENTIAL_FLAGS Flags;
        public CXPLAT_TLS_CREDENTIAL_FLAGS TlsFlags;
    }

    internal class CXPLAT_TLS
    {
        public CXPLAT_SEC_CONFIG SecConfig;
        public QUIC_HKDF_LABELS HkdfLabels;
        public bool IsServer;
        public bool PeerCertReceived;
        public bool PeerTPReceived;
        public uint QuicTpExtType;
        public QUIC_ALPN_BUFFER AlpnBuffer;
        public string SNI; //目标主机地址//域名
        public IntPtr Ssl;
        public CXPLAT_TLS_PROCESS_STATE State;
        public uint ResultFlags;
        public QUIC_CONNECTION Connection;
        public QUIC_TLS_SECRETS TlsSecrets;

        public void WriteFrom(Span<byte> buffer)
        {

        }

        public void WriteTo(Span<byte> buffer)
        {

        }
    }

    internal static partial class MSQuicFunc
    {
        public const int PFX_PASSWORD_LENGTH = 33;
        public const int CXPLAT_TLS_DEFAULT_VERIFY_DEPTH = 10;
        static readonly SSL_QUIC_METHOD OpenSslQuicCallbacks = new SSL_QUIC_METHOD(
                CxPlatTlsSetEncryptionSecretsCallback,
                CxPlatTlsAddHandshakeDataCallback,
                CxPlatTlsFlushFlightCallback,
                CxPlatTlsSendAlertCallback
        );

        static int CxPlatTlsSecConfigCreate(QUIC_CREDENTIAL_CONFIG CredConfig, CXPLAT_TLS_CREDENTIAL_FLAGS TlsCredFlags,
            CXPLAT_TLS_CALLBACKS TlsCallbacks, object Context, CXPLAT_SEC_CONFIG_CREATE_COMPLETE CompletionHandler)
        {
            QUIC_CREDENTIAL_FLAGS CredConfigFlags = CredConfig.Flags;

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_LOAD_ASYNCHRONOUS) && CredConfig.AsyncHandler == null)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

#if CX_PLATFORM_USES_TLS_BUILTIN_CERTIFICATE
            CredConfigFlags |= QUIC_CREDENTIAL_FLAG_USE_TLS_BUILTIN_CERTIFICATE_VALIDATION;
#endif

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION) &&
                !(CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED)))
            {
                return QUIC_STATUS_INVALID_PARAMETER; // Defer validation without indication doesn't make sense.
            }

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_USE_TLS_BUILTIN_CERTIFICATE_VALIDATION) && false)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            if (CredConfig.Reserved != null)
            {
                return QUIC_STATUS_INVALID_PARAMETER; // Not currently used and should be NULL.
            }

            if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE)
            {
                if (CredConfig.CertificateFile == null || CredConfig.CertificateFile.CertificateFile == null || CredConfig.CertificateFile.PrivateKeyFile == null)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }
            }
            else if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE_PROTECTED)
            {
                if (CredConfig.CertificateFileProtected == null ||
                    CredConfig.CertificateFileProtected.CertificateFile == null ||
                    CredConfig.CertificateFileProtected.PrivateKeyFile == null ||
                    CredConfig.CertificateFileProtected.PrivateKeyPassword == null)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }
            }
            else if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_PKCS12)
            {
                if (CredConfig.CertificatePkcs12 == null ||
                    CredConfig.CertificatePkcs12.Asn1Blob == null)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }
            }
            else if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return QUIC_STATUS_NOT_SUPPORTED; // Only supported on windows.
                }
            }
            else if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_NONE)
            {
                if (!(CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_CLIENT)))
                {
                    return QUIC_STATUS_INVALID_PARAMETER; // Required for server
                }
            }
            else
            {
                return QUIC_STATUS_NOT_SUPPORTED;
            }

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_SET_ALLOWED_CIPHER_SUITES))
            {
                if (!CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256) &&
                     !CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_AES_256_GCM_SHA384) &&
                    !CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256))
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }

                if (CredConfig.AllowedCipherSuites == QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256 &&
                    !CxPlatCryptSupports(CXPLAT_AEAD_TYPE.CXPLAT_AEAD_CHACHA20_POLY1305))
                {
                    return QUIC_STATUS_NOT_SUPPORTED;
                }
            }

            int Status = QUIC_STATUS_SUCCESS;
            int Ret = 0;
            CXPLAT_SEC_CONFIG SecurityConfig = new CXPLAT_SEC_CONFIG();
            IntPtr X509Cert = IntPtr.Zero;
            IntPtr PrivateKey = IntPtr.Zero;

            SecurityConfig.Callbacks = TlsCallbacks;
            SecurityConfig.Flags = CredConfigFlags;
            SecurityConfig.TlsFlags = TlsCredFlags;
            SecurityConfig.SSLCtx = BoringSSLFunc.SSL_CTX_new();
            if (SecurityConfig.SSLCtx == null)
            {
                Status = QUIC_STATUS_TLS_ERROR;
                goto Exit;
            }

            Ret = BoringSSLFunc.SSL_CTX_set_min_proto_version(SecurityConfig.SSLCtx, BoringSSLFunc.TLS1_3_VERSION);
            if (Ret != 1)
            {
                Status = QUIC_STATUS_TLS_ERROR;
                goto Exit;
            }

            Ret = BoringSSLFunc.SSL_CTX_set_max_proto_version(SecurityConfig.SSLCtx, BoringSSLFunc.TLS1_3_VERSION);
            if (Ret != 1)
            {
                Status = QUIC_STATUS_TLS_ERROR;
                goto Exit;
            }

            string CipherSuites = CXPLAT_TLS_DEFAULT_SSL_CIPHERS;
            List<string> CipherSuitesList = new List<string>();
            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_SET_ALLOWED_CIPHER_SUITES))
            {
                if (CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256))
                {
                    CipherSuitesList.Add(CXPLAT_TLS_AES_128_GCM_SHA256);
                }

                if (CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_AES_256_GCM_SHA384))
                {
                    CipherSuitesList.Add(CXPLAT_TLS_AES_256_GCM_SHA384);
                }

                if (CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256))
                {
                    CipherSuitesList.Add(CXPLAT_TLS_CHACHA20_POLY1305_SHA256);
                }

                CipherSuites = string.Join(':', CipherSuitesList);
            }

            Ret = BoringSSLFunc.SSL_CTX_set_ciphersuites(SecurityConfig.SSLCtx, CipherSuites);
            if (Ret != 1)
            {
                Status = QUIC_STATUS_TLS_ERROR;
                goto Exit;
            }

            if (SecurityConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_USE_TLS_BUILTIN_CERTIFICATE_VALIDATION))
            {
                Ret = BoringSSLFunc.SSL_CTX_set_default_verify_paths(SecurityConfig.SSLCtx);
                if (Ret != 1)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }
            }

            Ret = BoringSSLFunc.SSL_CTX_set_quic_method(SecurityConfig.SSLCtx, OpenSslQuicCallbacks);
            if (Ret != 1)
            {
                Status = QUIC_STATUS_TLS_ERROR;
                goto Exit;
            }

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_CLIENT) &&
                !TlsCredFlags.HasFlag(CXPLAT_TLS_CREDENTIAL_FLAGS.CXPLAT_TLS_CREDENTIAL_FLAG_DISABLE_RESUMPTION))
            {
                BoringSSLFunc.SSL_CTX_set_session_cache_mode(SecurityConfig.SSLCtx,
                    BoringSSLFunc.SSL_SESS_CACHE_CLIENT | BoringSSLFunc.SSL_SESS_CACHE_NO_INTERNAL_STORE);
                BoringSSLFunc.SSL_CTX_sess_set_new_cb(SecurityConfig.SSLCtx, CxPlatTlsOnClientSessionTicketReceived);
            }

            if (!CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_CLIENT))
            {
                if (!TlsCredFlags.HasFlag(CXPLAT_TLS_CREDENTIAL_FLAGS.CXPLAT_TLS_CREDENTIAL_FLAG_DISABLE_RESUMPTION))
                {
                    Ret = BoringSSLFunc.SSL_CTX_set_max_early_data(SecurityConfig.SSLCtx, 0xFFFFFFFF);
                    if (Ret != 1)
                    {
                        Status = QUIC_STATUS_TLS_ERROR;
                        goto Exit;
                    }

                    Ret = BoringSSLFunc.SSL_CTX_set_session_ticket_cb(
                        SecurityConfig.SSLCtx,
                        null,
                        CxPlatTlsOnServerSessionTicketDecrypted);
                    if (Ret != 1)
                    {
                        Status = QUIC_STATUS_TLS_ERROR;
                        goto Exit;
                    }
                }

                Ret = BoringSSLFunc.SSL_CTX_set_num_tickets(SecurityConfig.SSLCtx, 0);
                if (Ret != 1)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }
            }


            // 设置证书，这么大的东西，你竟然没设置
            if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE ||
                CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE_PROTECTED)
            {
                if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE_PROTECTED)
                {
                    BoringSSLFunc.SSL_CTX_set_default_passwd_cb_userdata(SecurityConfig.SSLCtx, CredConfig.CertificateFileProtected.PrivateKeyPassword);
                }

                Ret = BoringSSLFunc.SSL_CTX_use_PrivateKey_file(SecurityConfig.SSLCtx, CredConfig.CertificateFile.PrivateKeyFile, BoringSSLFunc.SSL_FILETYPE_PEM);
                if (Ret != 1)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }

                Ret = BoringSSLFunc.SSL_CTX_use_certificate_chain_file(SecurityConfig.SSLCtx, CredConfig.CertificateFile.CertificateFile);
                if (Ret != 1)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }
            }
            else if (CredConfig.Type != QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_NONE)
            {
                IntPtr Bio = BoringSSLFunc.BIO_new();
                IntPtr Pkcs12;
                string Password = null;

                if (Bio == null)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }

                BoringSSLFunc.BIO_set_mem_eof_return(Bio, 0);

                if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_PKCS12)
                {
                    Password = CredConfig.CertificatePkcs12.PrivateKeyPassword;
                    Ret = BoringSSLFunc.BIO_write(Bio, CredConfig.CertificatePkcs12.Asn1Blob.GetSpan());
                    if (Ret < 0)
                    {
                        Status = QUIC_STATUS_TLS_ERROR;
                        goto Exit;
                    }
                }
                else
                {
                    //用hash 去查找 证书，Windows 专用
                    Span<byte> PasswordBuffer = new byte[PFX_PASSWORD_LENGTH];
                    CxPlatRandom.Random(PasswordBuffer);
                    Password = Convert.ToBase64String(PasswordBuffer);

                    var mCert = X509CertTool.GetPfxCertByHash(CredConfig.CertificateHash.Hash, Password);
                    NetLog.Log("mCert Has 私钥: " + mCert.HasPrivateKey);
                    byte[] PfxBlob = mCert.GetRawCertData();
                    Ret = BoringSSLFunc.BIO_write(Bio, PfxBlob.AsSpan());
                    if (Ret < 0)
                    {
                        Status = QUIC_STATUS_TLS_ERROR;
                        goto Exit;
                    }
                }
                
                Pkcs12 = BoringSSLFunc.d2i_PKCS12_bio(Bio);
                BoringSSLFunc.BIO_free(Bio);
                Bio = IntPtr.Zero;

                if (Pkcs12 == IntPtr.Zero)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }

                IntPtr CaCertificates = IntPtr.Zero;
                Ret = BoringSSLFunc.PKCS12_parse(Pkcs12, Password, ref PrivateKey, ref X509Cert, ref CaCertificates);
                if (CaCertificates != IntPtr.Zero)
                {
                    IntPtr CaCert;
                    while ((CaCert = BoringSSLFunc.sk_X509_pop(CaCertificates)) != IntPtr.Zero)
                    {
                        BoringSSLFunc.SSL_CTX_add_extra_chain_cert(SecurityConfig.SSLCtx, CaCert);
                    }
                    BoringSSLFunc.sk_X509_free(CaCertificates);
                }
                if (Pkcs12 != IntPtr.Zero)
                {
                    BoringSSLFunc.PKCS12_free(Pkcs12);
                }

                if (Ret != 1)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }

                Ret = BoringSSLFunc.SSL_CTX_use_PrivateKey(SecurityConfig.SSLCtx, PrivateKey);
                if (Ret != 1)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }

                Ret = BoringSSLFunc.SSL_CTX_use_certificate(SecurityConfig.SSLCtx, X509Cert);
                if (Ret != 1)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }
            }

            if (CredConfig.Type != QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_NONE)
            {
                Ret = BoringSSLFunc.SSL_CTX_check_private_key(SecurityConfig.SSLCtx);
                if (Ret != 1)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }
            }

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_SET_CA_CERTIFICATE_FILE) && CredConfig.CaCertificateFile != null)
            {
                Ret = BoringSSLFunc.SSL_CTX_load_verify_locations(SecurityConfig.SSLCtx, CredConfig.CaCertificateFile, null);
                if (Ret != 1)
                {
                    Status = QUIC_STATUS_TLS_ERROR;
                    goto Exit;
                }
            }

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_CLIENT))
            {
                BoringSSLFunc.SSL_CTX_set_cert_verify_callback(SecurityConfig.SSLCtx, CxPlatTlsCertificateVerifyCallback, null);
                BoringSSLFunc.SSL_CTX_set_verify(SecurityConfig.SSLCtx, BoringSSLFunc.SSL_VERIFY_PEER, null);
                if (!CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION) &&
                    !CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION))
                {
                    BoringSSLFunc.SSL_CTX_set_verify_depth(SecurityConfig.SSLCtx, CXPLAT_TLS_DEFAULT_VERIFY_DEPTH);
                }
            }
            else
            {
                BoringSSLFunc.SSL_CTX_set_options(
                    SecurityConfig.SSLCtx,
                    (BoringSSLFunc.SSL_OP_ALL & ~BoringSSLFunc.SSL_OP_DONT_INSERT_EMPTY_FRAGMENTS) |
                    BoringSSLFunc.SSL_OP_SINGLE_ECDH_USE |
                    BoringSSLFunc.SSL_OP_CIPHER_SERVER_PREFERENCE |
                    BoringSSLFunc.SSL_OP_NO_ANTI_REPLAY);

                BoringSSLFunc.SSL_CTX_clear_options(SecurityConfig.SSLCtx, BoringSSLFunc.SSL_OP_ENABLE_MIDDLEBOX_COMPAT);
                BoringSSLFunc.SSL_CTX_set_mode(SecurityConfig.SSLCtx, BoringSSLFunc.SSL_MODE_RELEASE_BUFFERS);

                if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED) ||
                    CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_REQUIRE_CLIENT_AUTHENTICATION))
                {
                    BoringSSLFunc.SSL_CTX_set_cert_verify_callback(
                        SecurityConfig.SSLCtx,
                        CxPlatTlsCertificateVerifyCallback,
                        null);
                }

                if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_REQUIRE_CLIENT_AUTHENTICATION))
                {
                    int VerifyMode = BoringSSLFunc.SSL_VERIFY_PEER;
                    if (!CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION))
                    {
                        BoringSSLFunc.SSL_CTX_set_verify_depth(SecurityConfig.SSLCtx, CXPLAT_TLS_DEFAULT_VERIFY_DEPTH);
                    }
                    if (!CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION) &&
                        !CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION))
                    {
                        VerifyMode |= BoringSSLFunc.SSL_VERIFY_FAIL_IF_NO_PEER_CERT;
                    }

                    BoringSSLFunc.SSL_CTX_set_verify(SecurityConfig.SSLCtx, VerifyMode, null);
                }

                BoringSSLFunc.SSL_CTX_set_alpn_select_cb(SecurityConfig.SSLCtx, CxPlatTlsAlpnSelectCallback, null);
                BoringSSLFunc.SSL_CTX_set_max_early_data(SecurityConfig.SSLCtx, uint.MaxValue);
                BoringSSLFunc.SSL_CTX_set_client_hello_cb(SecurityConfig.SSLCtx, CxPlatTlsClientHelloCallback, null);
            }


            CompletionHandler(CredConfig, Context, Status, SecurityConfig);
            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_LOAD_ASYNCHRONOUS))
            {
                Status = QUIC_STATUS_PENDING;
            }
            else
            {
                Status = QUIC_STATUS_SUCCESS;
            }

        Exit:
            if (X509Cert != IntPtr.Zero)
            {
                BoringSSLFunc.X509_free(X509Cert);
            }
            
            if (PrivateKey != IntPtr.Zero)
            {
                BoringSSLFunc.EVP_PKEY_free(PrivateKey);
            }

            return Status;
        }

        static int CxPlatTlsInitialize(CXPLAT_TLS_CONFIG Config, CXPLAT_TLS_PROCESS_STATE State, ref CXPLAT_TLS NewTlsContext)
        {
            int Status = QUIC_STATUS_SUCCESS;
            CXPLAT_TLS TlsContext = null;

            int ServerNameLength = 0;
            NetLog.Assert(Config.HkdfLabels != null);
            if (Config.SecConfig == null)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

            TlsContext = new CXPLAT_TLS();
            if (TlsContext == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            TlsContext.Connection = Config.Connection;
            TlsContext.HkdfLabels = Config.HkdfLabels;
            TlsContext.IsServer = Config.IsServer;
            TlsContext.SecConfig = Config.SecConfig;
            TlsContext.QuicTpExtType = Config.TPType;
            TlsContext.AlpnBuffer = Config.AlpnBuffer;
            TlsContext.TlsSecrets = Config.TlsSecrets;

            if (!Config.IsServer)
            {
                if (!string.IsNullOrWhiteSpace(Config.ServerName))
                {
                    ServerNameLength = Config.ServerName.Length;
                    if (ServerNameLength >= QUIC_MAX_SNI_LENGTH)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                        goto Exit;
                    }

                    TlsContext.SNI = Config.ServerName;
                }
            }

            TlsContext.Ssl = BoringSSLFunc.SSL_new(Config.SecConfig.SSLCtx);
            if (TlsContext.Ssl == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            BoringSSLFunc.SSL_set_app_data(TlsContext.Ssl, TlsContext);
            if (Config.IsServer)
            {
                BoringSSLFunc.SSL_set_accept_state(TlsContext.Ssl);
            }
            else
            {
                BoringSSLFunc.SSL_set_connect_state(TlsContext.Ssl);
                BoringSSLFunc.SSL_set_tlsext_host_name(TlsContext.Ssl, TlsContext.SNI);
                BoringSSLFunc.SSL_set_alpn_protos(TlsContext.Ssl, TlsContext.AlpnBuffer.GetSpan());
            }

            if (!Config.SecConfig.TlsFlags.HasFlag(CXPLAT_TLS_CREDENTIAL_FLAGS.CXPLAT_TLS_CREDENTIAL_FLAG_DISABLE_RESUMPTION))
            {
                if (Config.ResumptionTicketBuffer != null && Config.ResumptionTicketBuffer.Length > 0)
                {
                    IntPtr Bio = BoringSSLFunc.BIO_new_mem_buf(Config.ResumptionTicketBuffer.GetSpan());
                    if (Bio != null)
                    {
                        IntPtr Session = BoringSSLFunc.PEM_read_bio_SSL_SESSION(Bio, out _, IntPtr.Zero, IntPtr.Zero);
                        if (Session != null)
                        {
                            if (BoringSSLFunc.SSL_set_session(TlsContext.Ssl, Session) == 0)
                            {
                                NetLog.LogError("SSL_set_session failed");
                            }
                            BoringSSLFunc.SSL_SESSION_free(Session);
                        }
                        else
                        {
                            NetLog.LogError("PEM_read_bio_SSL_SESSION failed");
                        }
                        BoringSSLFunc.BIO_free(Bio);
                    }
                    else
                    {
                        NetLog.LogError("BIO_new_mem_buf failed");
                    }
                }

                if (Config.IsServer || Config.ResumptionTicketBuffer != null)
                {
                    BoringSSLFunc.SSL_set_quic_early_data_enabled(TlsContext.Ssl, true);
                }
            }

            BoringSSLFunc.SSL_set_quic_use_legacy_codepoint(TlsContext.Ssl,
                TlsContext.QuicTpExtType == TLS_EXTENSION_TYPE_QUIC_TRANSPORT_PARAMETERS_DRAFT);
            
            if (BoringSSLFunc.SSL_set_quic_transport_params(TlsContext.Ssl, Config.LocalTPBuffer.GetSpan()) != 1)
            {
                Status = QUIC_STATUS_TLS_ERROR;
                goto Exit;
            }

            Config.LocalTPBuffer = null;
            if (Config.ResumptionTicketBuffer != null)
            {
                Config.ResumptionTicketBuffer = null;
            }

            NewTlsContext = TlsContext;
            TlsContext = null;
        Exit:
            return Status;
        }

        static void CxPlatTlsUninitialize(CXPLAT_TLS TlsContext)
        {
            if (TlsContext != null)
            {
                TlsContext.SNI = null;
                TlsContext.Ssl = IntPtr.Zero;
            }
        }

        static bool QuicTlsPopulateOffloadKeys(CXPLAT_TLS TlsContext, QUIC_PACKET_KEY PacketKey, string SecretName, CXPLAT_QEO_CONNECTION Offload)
        {
            int Status = QuicPacketKeyDeriveOffload(
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
            TlsContext.State = State;
            TlsContext.ResultFlags = 0;
            if (DataType == CXPLAT_TLS_DATA_TYPE.CXPLAT_TLS_TICKET_DATA)
            {
                IntPtr Session = BoringSSLFunc.SSL_get_session(TlsContext.Ssl);
                if (Session == null)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    goto Exit;
                }

                if (BoringSSLFunc.SSL_SESSION_set1_ticket_appdata(Session, Buffer.GetSpan()) == 0)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    goto Exit;
                }

                if (BoringSSLFunc.SSL_new_session_ticket(TlsContext.Ssl) == 0)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    goto Exit;
                }

                int Ret = BoringSSLFunc.SSL_do_handshake(TlsContext.Ssl);
                if (Ret != 1)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    goto Exit;
                }

                goto Exit;
            }

            if (Buffer.Length != 0)
            {
                if (BoringSSLFunc.SSL_provide_quic_data(
                        TlsContext.Ssl,
                        (ssl_encryption_level_t)(int)TlsContext.State.ReadKey,
                        Buffer.GetSpan()) != 1)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    goto Exit;
                }
            }

            if (!State.HandshakeComplete)
            {
                int Ret = BoringSSLFunc.SSL_do_handshake(TlsContext.Ssl);
                if (Ret <= 0)
                {
                    int Err = BoringSSLFunc.SSL_get_error(TlsContext.Ssl, Ret);
                    switch (Err)
                    {
                        case BoringSSLFunc.SSL_ERROR_WANT_READ:
                        case BoringSSLFunc.SSL_ERROR_WANT_WRITE:
                            if (!TlsContext.IsServer && TlsContext.PeerTPReceived == false)
                            {
                                Span<byte> TransportParams;
                                BoringSSLFunc.SSL_get_peer_quic_transport_params(TlsContext.Ssl, out TransportParams);
                                if (!TransportParams.IsEmpty)
                                {
                                    TlsContext.PeerTPReceived = true;
                                    if (!TlsContext.SecConfig.Callbacks.ReceiveTP(TlsContext.Connection, TransportParams))
                                    {
                                        TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                                        goto Exit;
                                    }
                                }
                            }
                            goto Exit;
                        case BoringSSLFunc.SSL_ERROR_SSL:
                            {
                                BoringSSLFunc.print_openssl_errors();
                                TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                                goto Exit;
                            }

                        default:
                            BoringSSLFunc.print_openssl_errors();
                            TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                            goto Exit;
                    }
                }

                if (!TlsContext.IsServer)
                {
                    Span<byte> NegotiatedAlpn;
                    BoringSSLFunc.SSL_get0_alpn_selected(TlsContext.Ssl, out NegotiatedAlpn);
                    if (NegotiatedAlpn.Length == 0)
                    {
                        TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                        goto Exit;
                    }
                    if (NegotiatedAlpn.Length > byte.MaxValue)
                    {
                        TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                        goto Exit;
                    }
                    TlsContext.State.NegotiatedAlpn = CxPlatTlsAlpnFindInList(TlsContext.AlpnBuffer.GetSpan(), NegotiatedAlpn);
                    if (TlsContext.State.NegotiatedAlpn == null)
                    {
                        TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                        goto Exit;
                    }
                }
                else if (TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED) &&
                    !TlsContext.PeerCertReceived)
                {
                    int ValidationResult =
                        (!TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION) &&
                        (TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_REQUIRE_CLIENT_AUTHENTICATION)) ||
                        TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION)) ?
                            QUIC_STATUS_CERT_NO_CERT :
                            QUIC_STATUS_SUCCESS;

                    if (!TlsContext.SecConfig.Callbacks.CertificateReceived(
                            TlsContext.Connection,
                            null,
                            null,
                            0,
                            ValidationResult))
                    {
                        TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                        TlsContext.State.AlertCode = CXPLAT_TLS_ALERT_CODES.CXPLAT_TLS_ALERT_CODE_REQUIRED_CERTIFICATE;
                        goto Exit;
                    }
                }

                State.HandshakeComplete = true;
                if (BoringSSLFunc.SSL_session_reused(TlsContext.Ssl) > 0)
                {
                    State.SessionResumed = true;
                }

                if (!TlsContext.IsServer)
                {
                    int EarlyDataStatus = BoringSSLFunc.SSL_get_early_data_status(TlsContext.Ssl);
                    if (EarlyDataStatus == BoringSSLFunc.SSL_EARLY_DATA_ACCEPTED)
                    {
                        State.EarlyDataState = CXPLAT_TLS_EARLY_DATA_STATE.CXPLAT_TLS_EARLY_DATA_ACCEPTED;
                        SetFlag(TlsContext.ResultFlags , (ulong)CXPLAT_TLS_RESULT_FLAGS.CXPLAT_TLS_RESULT_EARLY_DATA_ACCEPT, true);
                    }
                    else if (EarlyDataStatus == BoringSSLFunc.SSL_EARLY_DATA_REJECTED)
                    {
                        State.EarlyDataState = CXPLAT_TLS_EARLY_DATA_STATE.CXPLAT_TLS_EARLY_DATA_REJECTED;
                        SetFlag(TlsContext.ResultFlags, (ulong)CXPLAT_TLS_RESULT_FLAGS.CXPLAT_TLS_RESULT_EARLY_DATA_REJECT, true);
                    }
                }
                TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_HANDSHAKE_COMPLETE;

                if (TlsContext.IsServer)
                {
                    TlsContext.State.ReadKey =  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT;
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_READ_KEY_UPDATED;
                }
                else if (!TlsContext.PeerTPReceived)
                {
                    Span<byte> TransportParams;
                    BoringSSLFunc.SSL_get_peer_quic_transport_params(TlsContext.Ssl, out TransportParams);
                    if (TransportParams == null)
                    {
                        TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                        goto Exit;
                    }
                    
                    TlsContext.PeerTPReceived = true;
                    if (!TlsContext.SecConfig.Callbacks.ReceiveTP(
                            TlsContext.Connection,
                            TransportParams))
                    {
                        TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                        goto Exit;
                    }
                }
            }
            else
            {
                if (BoringSSLFunc.SSL_process_quic_post_handshake(TlsContext.Ssl) != 1)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    goto Exit;
                }
            }

        Exit:
            if (!BoolOk(TlsContext.ResultFlags & CXPLAT_TLS_RESULT_ERROR))
            {
                if (State.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE] != null && State.BufferOffsetHandshake == 0)
                {
                    State.BufferOffsetHandshake = State.BufferTotalLength;
                }

                if (State.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT] != null && State.BufferOffset1Rtt == 0)
                {
                    State.BufferOffset1Rtt = State.BufferTotalLength;
                }
            }

            return TlsContext.ResultFlags;
        }

        static void CxPlatTlsUpdateHkdfLabels(CXPLAT_TLS TlsContext, QUIC_HKDF_LABELS Labels)
        {
            TlsContext.HkdfLabels = Labels;
        }

        static void CxPlatTlsNegotiatedCiphers(CXPLAT_TLS TlsContext, ref CXPLAT_AEAD_TYPE AeadType, ref CXPLAT_HASH_TYPE HashType)
        {
            switch (BoringSSLFunc.SSL_CIPHER_get_id(BoringSSLFunc.SSL_get_current_cipher(TlsContext.Ssl)))
            {
                case 0x03001301U: // TLS_AES_128_GCM_SHA256
                    AeadType = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;
                    HashType = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
                    break;
                case 0x03001302U: // TLS_AES_256_GCM_SHA384
                    AeadType = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM;
                    HashType = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA384;
                    break;
                case 0x03001303U: // TLS_CHACHA20_POLY1305_SHA256
                    AeadType = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_CHACHA20_POLY1305;
                    HashType = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
                    break;
                default:
                    NetLog.Assert(false);
                    break;
            }
        }

        static unsafe int CxPlatTlsSetEncryptionSecretsCallback(IntPtr Ssl, ssl_encryption_level_t Level, IntPtr ReadSecretIntPtr,
            IntPtr WriteSecretIntPtr, int SecretLen)
        {
            NetLog.Log("OpenSSL CallBack: CxPlatTlsSetEncryptionSecretsCallback");
            ReadOnlySpan<byte> ReadSecret = new ReadOnlySpan<byte>(ReadSecretIntPtr.ToPointer(), SecretLen);
            ReadOnlySpan<byte> WriteSecret = new ReadOnlySpan<byte>(WriteSecretIntPtr.ToPointer(), SecretLen);

            CXPLAT_TLS TlsContext = BoringSSLFunc.SSL_get_app_data<CXPLAT_TLS>(Ssl);
            CXPLAT_TLS_PROCESS_STATE TlsState = TlsContext.State;
            QUIC_PACKET_KEY_TYPE KeyType = (QUIC_PACKET_KEY_TYPE)Level;
            int Status;

            CXPLAT_SECRET Secret = new CXPLAT_SECRET();
            CxPlatTlsNegotiatedCiphers(TlsContext, ref Secret.Aead, ref Secret.Hash);

            if (WriteSecret != null)
            {
                WriteSecret.CopyTo(Secret.Secret.GetSpan());
                NetLog.Assert(TlsState.WriteKeys[(int)KeyType] == null);
                Status = QuicPacketKeyDerive(KeyType, TlsContext.HkdfLabels,
                        Secret,
                        "write secret",
                        true,
                        ref TlsState.WriteKeys[(int)KeyType]);

                if (QUIC_FAILED(Status))
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    return -1;
                }

                TlsState.WriteKey = KeyType;
                TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_WRITE_KEY_UPDATED;

                if (TlsContext.IsServer && KeyType ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_EARLY_DATA_ACCEPT;
                    TlsContext.State.EarlyDataState =  CXPLAT_TLS_EARLY_DATA_STATE.CXPLAT_TLS_EARLY_DATA_ACCEPTED;
                }
            }

            if (ReadSecret != null)
            {
                ReadSecret.Slice(0, SecretLen).CopyTo(Secret.Secret.GetSpan());
                NetLog.Assert(TlsState.ReadKeys[(int)KeyType] == null);
                Status = QuicPacketKeyDerive(KeyType, TlsContext.HkdfLabels, Secret,
                        "read secret",
                        true,
                        ref TlsState.ReadKeys[(int)KeyType]);

                if (QUIC_FAILED(Status))
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    return -1;
                }

                if (TlsContext.IsServer && KeyType ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
                {
                }
                else
                {
                    TlsState.ReadKey = KeyType;
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_READ_KEY_UPDATED;
                }
            }

            if (TlsContext.TlsSecrets != null)
            {
                TlsContext.TlsSecrets.SecretLength = SecretLen;
                switch (KeyType)
                {
                    case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE:
                        NetLog.Assert(ReadSecret != null && WriteSecret != null);
                        if (TlsContext.IsServer)
                        {
                            ReadSecret.CopyTo(TlsContext.TlsSecrets.ClientHandshakeTrafficSecret.AsSpan());
                            WriteSecret.CopyTo(TlsContext.TlsSecrets.ServerHandshakeTrafficSecret.AsSpan());
                        }
                        else
                        {
                            WriteSecret.CopyTo(TlsContext.TlsSecrets.ClientHandshakeTrafficSecret.AsSpan());
                            ReadSecret.CopyTo(TlsContext.TlsSecrets.ServerHandshakeTrafficSecret.AsSpan());
                        }
                        TlsContext.TlsSecrets.IsSet.ClientHandshakeTrafficSecret = true;
                        TlsContext.TlsSecrets.IsSet.ServerHandshakeTrafficSecret = true;
                        break;
                    case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT:
                        NetLog.Assert(ReadSecret != null && WriteSecret != null);
                        if (TlsContext.IsServer)
                        {
                            ReadSecret.CopyTo(TlsContext.TlsSecrets.ClientTrafficSecret0.AsSpan());
                            WriteSecret.CopyTo(TlsContext.TlsSecrets.ServerTrafficSecret0.AsSpan());
                        }
                        else
                        {
                            WriteSecret.CopyTo(TlsContext.TlsSecrets.ClientTrafficSecret0.AsSpan());
                            ReadSecret.CopyTo(TlsContext.TlsSecrets.ServerTrafficSecret0.AsSpan());
                        }

                        TlsContext.TlsSecrets.IsSet.ClientTrafficSecret0 = true;
                        TlsContext.TlsSecrets.IsSet.ServerTrafficSecret0 = true;
                        TlsContext.TlsSecrets = null;
                        break;
                    case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT:
                        if (TlsContext.IsServer)
                        {
                            NetLog.Assert(ReadSecret != null);
                            ReadSecret.CopyTo(TlsContext.TlsSecrets.ClientEarlyTrafficSecret.AsSpan());
                            TlsContext.TlsSecrets.IsSet.ClientEarlyTrafficSecret = true;
                        }
                        else
                        {
                            NetLog.Assert(WriteSecret != null);
                            WriteSecret.CopyTo(TlsContext.TlsSecrets.ClientEarlyTrafficSecret.AsSpan());
                            TlsContext.TlsSecrets.IsSet.ClientEarlyTrafficSecret = true;
                        }
                        break;
                    default:
                        break;
                }
            }

            return 1;
        }

        static unsafe int CxPlatTlsAddHandshakeDataCallback(IntPtr Ssl, ssl_encryption_level_t Level, IntPtr DataIntPtr, int Length)
        {
            NetLog.Log("OpenSSL CallBack: CxPlatTlsAddHandshakeDataCallback");
            ReadOnlySpan<byte> Data = new ReadOnlySpan<byte>(DataIntPtr.ToPointer(), Length);

            CXPLAT_TLS TlsContext = BoringSSLFunc.SSL_get_app_data<CXPLAT_TLS>(Ssl);
            CXPLAT_TLS_PROCESS_STATE TlsState = TlsContext.State;

            QUIC_PACKET_KEY_TYPE KeyType = (QUIC_PACKET_KEY_TYPE)Level;
            if (HasFlag(TlsContext.ResultFlags, CXPLAT_TLS_RESULT_ERROR))
            {
                return -1;
            }
            NetLog.Assert(KeyType == 0 || TlsState.WriteKeys[(int)KeyType] != null);

            if (Length + TlsState.BufferLength > 0xF000)
            {
                TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                return -1;
            }

            if (Length + TlsState.BufferLength > TlsState.BufferAllocLength)
            {
                int NewBufferAllocLength = TlsState.BufferAllocLength;
                while (Length + TlsState.BufferLength > NewBufferAllocLength)
                {
                    NewBufferAllocLength <<= 1;
                }

                byte[] NewBuffer = new byte[NewBufferAllocLength];
                if (NewBuffer == null)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    return -1;
                }

                TlsState.Buffer.AsSpan().Slice(0, TlsState.BufferLength).CopyTo(NewBuffer);
                TlsState.Buffer = NewBuffer;
                TlsState.BufferAllocLength = NewBufferAllocLength;
            }

            switch (KeyType)
            {
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE:
                    if (TlsState.BufferOffsetHandshake == 0)
                    {
                        TlsState.BufferOffsetHandshake = TlsState.BufferTotalLength;
                    }
                    break;
                case QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT:
                    if (TlsState.BufferOffset1Rtt == 0)
                    {
                        TlsState.BufferOffset1Rtt = TlsState.BufferTotalLength;
                    }
                    break;
                default:
                    break;
            }

            Data.CopyTo(TlsState.Buffer.AsSpan().Slice(TlsState.BufferLength));
            TlsState.BufferLength += Length;
            TlsState.BufferTotalLength += Length;

            TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_DATA;
            return 1;
        }

        static int CxPlatTlsFlushFlightCallback(IntPtr Ssl)
        {
            NetLog.Log("OpenSSL CallBack: CxPlatTlsFlushFlightCallback");
            return 1;
        }

        //当 TLS 或 QUIC 协议层检测到错误时，会通过 Alert 消息通知对方。
        static int CxPlatTlsSendAlertCallback(IntPtr Ssl, ssl_encryption_level_t Level, byte Alert)
        {
            NetLog.Log($"OpenSSL CallBack: CxPlatTlsSendAlertCallback: {Alert}");

            CXPLAT_TLS TlsContext = BoringSSLFunc.SSL_get_app_data<CXPLAT_TLS>(Ssl);
            TlsContext.State.AlertCode = (CXPLAT_TLS_ALERT_CODES)Alert;
            TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
            return 1;
        }

        static int CxPlatTlsOnClientSessionTicketReceived(IntPtr Ssl, IntPtr Session)
        {
            NetLog.Log("OpenSSL CallBack: CxPlatTlsOnClientSessionTicketReceived");
            CXPLAT_TLS TlsContext = BoringSSLFunc.SSL_get_app_data<CXPLAT_TLS>(Ssl);

            IntPtr Bio = BoringSSLFunc.BIO_new();
            if (Bio != null)
            {
                if (BoringSSLFunc.PEM_write_bio_SSL_SESSION(Bio, Session) == 1)
                {
                    Span<byte> Data = null;
                    BoringSSLFunc.BIO_get_mem_data(Bio, out Data);
                    if (Data.Length < ushort.MaxValue)
                    {
                        NetLog.Log($"Received session ticket, {Data.Length} bytes");
                        TlsContext.SecConfig.Callbacks.ReceiveTicket(TlsContext.Connection, Data);
                    }
                    else
                    {
                        NetLog.Assert(false);
                    }
                }
                else
                {
                    NetLog.Assert(false);
                }
                BoringSSLFunc.BIO_free(Bio);
            }
            else
            {
                NetLog.Assert(false);
            }

            return 0;
        }

        static int CxPlatTlsOnServerSessionTicketGenerated(IntPtr Ssl, IntPtr arg)
        {

            CXPLAT_TLS TlsContext = BoringSSLFunc.SSL_get_app_data<CXPLAT_TLS>(Ssl);
            return 1;
        }

        static int CxPlatTlsOnServerSessionTicketDecrypted(IntPtr Ssl, IntPtr Session, string keyname, int keyname_length, int status, IntPtr arg)
        {
            NetLog.Log("OpenSSL CallBack: CxPlatTlsOnServerSessionTicketDecrypted");
            CXPLAT_TLS TlsContext = BoringSSLFunc.SSL_get_app_data<CXPLAT_TLS>(Ssl);

            int Result;
            if (status == BoringSSLFunc.SSL_TICKET_SUCCESS) {
                Result = BoringSSLFunc.SSL_TICKET_RETURN_USE;
            } else if (status == BoringSSLFunc.SSL_TICKET_SUCCESS_RENEW) {
                Result = BoringSSLFunc.SSL_TICKET_RETURN_USE_RENEW;
            } else
            {
                Result = BoringSSLFunc.SSL_TICKET_RETURN_IGNORE_RENEW;
            }

            ReadOnlySpan<byte> Buffer = null;
            if (Session != null && BoringSSLFunc.SSL_SESSION_get0_ticket_appdata(Session, out Buffer) > 0)
            {
                if (!TlsContext.SecConfig.Callbacks.ReceiveTicket(TlsContext.Connection, Buffer))
                {
                    if (status == BoringSSLFunc.SSL_TICKET_SUCCESS_RENEW)
                    {
                        Result = BoringSSLFunc.SSL_TICKET_RETURN_IGNORE_RENEW;
                    }
                    else
                    {
                        Result = BoringSSLFunc.SSL_TICKET_RETURN_IGNORE;
                    }
                }
            }

            return Result;
        }

        static int CxPlatTlsClientHelloCallback(IntPtr Ssl, ref int Alert, IntPtr arg)
        {
            NetLog.Log("OpenSSL CallBack: CxPlatTlsClientHelloCallback");
            CXPLAT_TLS TlsContext = BoringSSLFunc.SSL_get_app_data<CXPLAT_TLS>(Ssl);

            ReadOnlySpan<byte> TransportParams;
            if (BoringSSLFunc.SSL_client_hello_get0_ext(Ssl, TlsContext.QuicTpExtType, out TransportParams) == 0)
            {
                TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                Alert = BoringSSLFunc.SSL_AD_INTERNAL_ERROR;
                return BoringSSLFunc.SSL_CLIENT_HELLO_ERROR;
            }

            return BoringSSLFunc.SSL_CLIENT_HELLO_SUCCESS;
        }

        static unsafe int CxPlatTlsAlpnSelectCallback(IntPtr Ssl, out IntPtr Out, out byte OutLen, IntPtr In, int InLen, IntPtr Arg)
        {
            NetLog.Log("OpenSSL CallBack: CxPlatTlsAlpnSelectCallback");
            CXPLAT_TLS TlsContext = BoringSSLFunc.SSL_get_app_data<CXPLAT_TLS>(Ssl);

            NetLog.Assert(TlsContext.State.NegotiatedAlpn != null);
            OutLen = (byte)TlsContext.State.NegotiatedAlpn[0];

            ReadOnlySpan<byte> alpnBuf = (TlsContext.State.NegotiatedAlpn + 1).GetSpan();
            fixed (void* alpnPtr = alpnBuf)
            {
                Out = (IntPtr)alpnPtr;
            }
            return BoringSSLFunc.SSL_TLSEXT_ERR_OK;
        }

        static unsafe int CxPlatTlsCertificateVerifyCallback(IntPtr x509_ctx, IntPtr param)
        {
            NetLog.Log("OpenSSL CallBack: CxPlatTlsCertificateVerifyCallback");
            int CertificateVerified = 0;
            int status = 1;
            ReadOnlySpan<byte> OpenSSLCertBuffer = null;
            ReadOnlySpan<byte> PortableCertificate = null;
            ReadOnlySpan<byte> PortableChain = null;
            IntPtr Cert = BoringSSLFunc.X509_STORE_CTX_get0_cert(x509_ctx);
            IntPtr Ssl = BoringSSLFunc.X509_STORE_CTX_get_ex_data(x509_ctx);
            CXPLAT_TLS TlsContext = BoringSSLFunc.SSL_get_app_data<CXPLAT_TLS>(Ssl);

            int ValidationResult = BoringSSLFunc.X509_V_OK;
            bool IsDeferredValidationOrClientAuth =
                TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_REQUIRE_CLIENT_AUTHENTICATION) ||
                TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION);

            TlsContext.PeerCertReceived = (Cert != null);

            if ((TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_CLIENT) || IsDeferredValidationOrClientAuth) &&
                !TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION))
            {
                //你不使用内置验证，我使用，我方便。 我总是使用OpenSSL 内置的证书验证
                CertificateVerified = BoringSSLFunc.X509_verify_cert(x509_ctx);
                if (IsDeferredValidationOrClientAuth &&
                    CertificateVerified <= 0)
                {
                    ValidationResult = (int)CxPlatTlsMapOpenSSLErrorToQuicStatus(BoringSSLFunc.X509_STORE_CTX_get_error(x509_ctx));
                }
            }
            else if ((TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED) &&
                       (TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_USE_PORTABLE_CERTIFICATES))))
            {
                BoringSSLFunc.X509_verify_cert(x509_ctx);
            }

            if (!TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION) &&
                !TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION) &&
                !BoolOk(CertificateVerified))
            {
                return 0;
            }

            //这里必须是可移值的证书，否则C# 没法解析
            //if (TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_USE_PORTABLE_CERTIFICATES))
            {
                if (Cert != null)
                {
                    BoringSSLFunc.i2d_X509(Cert, out PortableCertificate);
                    if (PortableCertificate == null)
                    {
                        BoringSSLFunc.X509_STORE_CTX_set_error(x509_ctx, BoringSSLFunc.X509_V_ERR_OUT_OF_MEM);
                        return 0;
                    }
                }

                if (x509_ctx != null)
                {
                    int ChainCount;
                    IntPtr Chain = BoringSSLFunc.X509_STORE_CTX_get0_chain(x509_ctx);
                    if ((ChainCount = BoringSSLFunc.sk_X509_num(Chain)) > 0)
                    {
                        IntPtr p7 = BoringSSLFunc.PKCS7_new();
                        if (p7 != null)
                        {
                            BoringSSLFunc.PKCS7_set_type(p7, BoringSSLFunc.NID_pkcs7_signed);
                            BoringSSLFunc.PKCS7_content_new(p7, BoringSSLFunc.NID_pkcs7_data);

                            for (int i = 0; i < ChainCount; i++)
                            {
                                BoringSSLFunc.PKCS7_add_certificate(p7, BoringSSLFunc.sk_X509_value(Chain, i));
                            }
                            BoringSSLFunc.i2d_PKCS7(p7, out PortableChain);
                            BoringSSLFunc.PKCS7_free(p7);
                        }
                    }
                }
            }

            if ((TlsContext.SecConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED) &&
                !TlsContext.SecConfig.Callbacks.CertificateReceived(TlsContext.Connection, PortableCertificate,PortableChain, 0, ValidationResult)))
            {
                BoringSSLFunc.X509_STORE_CTX_set_error(x509_ctx, BoringSSLFunc.X509_V_ERR_CERT_REJECTED);
                status = 0;
            }

            if (PortableCertificate != null)
            {
                fixed (void* ptr = PortableCertificate)
                {
                    BoringSSLFunc.OPENSSL_free((IntPtr)ptr);
                }
            }
            if (PortableChain != null)
            {
                fixed (void* ptr = PortableChain)
                {
                    BoringSSLFunc.OPENSSL_free((IntPtr)ptr);
                }
            }

            return status;
        }

        static int CxPlatTlsMapOpenSSLErrorToQuicStatus(int OpenSSLError)
        {
            switch (OpenSSLError)
            {
                case BoringSSLFunc.X509_V_ERR_CERT_REJECTED:
                    return QUIC_STATUS_BAD_CERTIFICATE;
                case BoringSSLFunc.X509_V_ERR_CERT_REVOKED:
                    return QUIC_STATUS_REVOKED_CERTIFICATE;
                case BoringSSLFunc.X509_V_ERR_CERT_HAS_EXPIRED:
                    return QUIC_STATUS_CERT_EXPIRED;
                case BoringSSLFunc.X509_V_ERR_DEPTH_ZERO_SELF_SIGNED_CERT:
                case BoringSSLFunc.X509_V_ERR_CERT_UNTRUSTED:
                    return QUIC_STATUS_CERT_UNTRUSTED_ROOT;
                default:
                    return QUIC_STATUS_TLS_ERROR;
            }
        }

    }
}
