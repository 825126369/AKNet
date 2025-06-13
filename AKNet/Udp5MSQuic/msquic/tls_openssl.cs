using AKNet.BoringSSL;
using AKNet.Common;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

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
        public QUIC_BUFFER AlpnBuffer;
        public string SNI; //目标主机地址//域名
        public IntPtr Ssl;
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

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_LOAD_ASYNCHRONOUS) && CredConfig.AsyncHandler == null)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_ENABLE_OCSP) ||
                CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_USE_SUPPLIED_CREDENTIALS) ||
                CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_USE_SYSTEM_MAPPER) ||
                CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_INPROC_PEER_CERTIFICATE))
            {
                return QUIC_STATUS_NOT_SUPPORTED; // Not supported by this TLS implementation
            }

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION) &&
                !(CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED)))
            {
                return QUIC_STATUS_INVALID_PARAMETER; // Defer validation without indication doesn't make sense.
            }

            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_USE_TLS_BUILTIN_CERTIFICATE_VALIDATION) &&
                (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_END_CERT) ||
                CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_CHAIN) ||
                CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT) ||
                CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_IGNORE_NO_REVOCATION_CHECK) ||
                CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_IGNORE_REVOCATION_OFFLINE) ||
                CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_CACHE_ONLY_URL_RETRIEVAL) ||
                CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_CACHE_ONLY) ||
                CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_DISABLE_AIA)))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
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
                    CredConfig.CertificatePkcs12.Asn1Blob == null ||
                    CredConfig.CertificatePkcs12.Asn1BlobLength == 0)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }
            }
            else if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH ||
                CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH_STORE ||
                CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_CONTEXT)
            {
                
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

            ulong Status = QUIC_STATUS_SUCCESS;
            int Ret = 0;
            X509Certificate2 X509Cert = null;
            byte[] PrivateKey = null;
            CXPLAT_SEC_CONFIG SecurityConfig = new CXPLAT_SEC_CONFIG();
            SecurityConfig.Callbacks = TlsCallbacks;
            SecurityConfig.Flags = CredConfigFlags;
            SecurityConfig.TlsFlags = TlsCredFlags;
            SecurityConfig.SSLCtx = BoringSSLFunc.SSL_CTX_new();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
            string CipherSuiteString = string.Empty;
            string CipherSuites = CXPLAT_TLS_DEFAULT_SSL_CIPHERS;
            if (CredConfigFlags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_SET_ALLOWED_CIPHER_SUITES))
            {
                int CipherSuiteStringLength = 0;
                int AllowedCipherSuitesCount = 0;
                if (CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256))
                {
                    CipherSuiteStringLength += CXPLAT_TLS_AES_128_GCM_SHA256.Length;
                    AllowedCipherSuitesCount++;
                }

                if (CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_AES_256_GCM_SHA384))
                {
                    CipherSuiteStringLength += CXPLAT_TLS_AES_256_GCM_SHA384.Length;
                    AllowedCipherSuitesCount++;
                }

                if (CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256))
                {
                    if (AllowedCipherSuitesCount == 0 && !CxPlatCryptSupports(CXPLAT_AEAD_TYPE.CXPLAT_AEAD_CHACHA20_POLY1305))
                    {
                        Status = QUIC_STATUS_NOT_SUPPORTED;
                        goto Exit;
                    }
                    CipherSuiteStringLength += CXPLAT_TLS_CHACHA20_POLY1305_SHA256.Length;
                    AllowedCipherSuitesCount++;
                }
           

                int CipherSuiteStringCursor = 0;
                if (CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_AES_256_GCM_SHA384))
                {
                    CipherSuiteString = CXPLAT_TLS_AES_256_GCM_SHA384;
                    CipherSuiteStringCursor += CXPLAT_TLS_AES_256_GCM_SHA384.Length;
                    if (--AllowedCipherSuitesCount > 0)
                    {
                        CipherSuiteString += ':';
                    }
                }

                if (CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256))
                {
                    CipherSuiteString += CXPLAT_TLS_CHACHA20_POLY1305_SHA256;
                    CipherSuiteStringCursor += CXPLAT_TLS_CHACHA20_POLY1305_SHA256.Length;
                    if (--AllowedCipherSuitesCount > 0)
                    {
                        CipherSuiteString += ':';
                    }
                }

                if (CredConfig.AllowedCipherSuites.HasFlag(QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256))
                {
                    CipherSuiteString += CXPLAT_TLS_AES_128_GCM_SHA256;
                    CipherSuiteStringCursor += CXPLAT_TLS_AES_128_GCM_SHA256.Length;
                }
                NetLog.Assert(CipherSuiteStringCursor == CipherSuSsliteStringLength);
                CipherSuites = CipherSuiteString;
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
            return Status;
        }

        static ulong CxPlatTlsInitialize(CXPLAT_TLS_CONFIG Config, CXPLAT_TLS_PROCESS_STATE State, ref CXPLAT_TLS NewTlsContext)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
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
                if (Config.ServerName != null)
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

            MemoryStream BufferStream = new MemoryStream(State.Buffer);
            TlsContext.Ssl = new SslStream(BufferStream);
            NewTlsContext = TlsContext;
            TlsContext = null;
        Exit:
            return Status;
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
            TlsContext.State = State;
            TlsContext.ResultFlags = 0;
            if (DataType == CXPLAT_TLS_DATA_TYPE.CXPLAT_TLS_TICKET_DATA)
            {
                SslStream Session = TlsContext.Ssl;
                if (Session == null)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    goto Exit;
                }

                try
                {
                    int bytesRead = Session.Read(Buffer.Buffer, 0, Buffer.Length);
                }
                catch (Exception)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    goto Exit;
                }

                try
                {
                    Session.AuthenticateAsClient(TlsContext.SNI);
                }
                catch (AuthenticationException ex)
                {
                    NetLog.LogError($"TLS authentication failed: {ex.Message}");
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
                        Buffer.Buffer,
                        Buffer.Length) != 1)
                {
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    goto Exit;
                }
            }

            if (!State.HandshakeComplete)
            {
                try
                {
                    SslStream Session = TlsContext.Ssl;
                    Session.AuthenticateAsClient(TlsContext.SNI);
                }
                catch (AuthenticationException ex)
                {
                    NetLog.LogError($"TLS authentication failed: {ex.Message}");
                    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                    goto Exit;
                }

                State.HandshakeComplete = true;
                TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_HANDSHAKE_COMPLETE;

                //if (TlsContext.IsServer)
                //{
                //    TlsContext.State.ReadKey = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT;
                //    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_READ_KEY_UPDATED;
                //}
                //else if (!TlsContext.PeerTPReceived)
                //{
                //    QUIC_SSBuffer TransportParams;
                //    SSL_get_peer_quic_transport_params(TlsContext.Ssl, TransportParams, &TransportParamLen);
                //    if (TransportParams.IsEmpty)
                //    {
                //        NetLog.LogError("No transport parameters received");
                //        TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                //        goto Exit;
                //    }

                //    TlsContext.PeerTPReceived = true;
                //    if (!TlsContext.SecConfig.Callbacks.ReceiveTP(TlsContext.Connection, TransportParamLen, TransportParams))
                //    {
                //        TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                //        goto Exit;
                //    }
                //}
            }
            else
            {
                //if (SSL_process_quic_post_handshake(TlsContext.Ssl) != 1)
                //{
                //    TlsContext.ResultFlags |= CXPLAT_TLS_RESULT_ERROR;
                //    goto Exit;
                //}
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
    }
}
