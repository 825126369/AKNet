using AKNet.Common;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace AKNet.Udp5MSQuic.Common
{
    internal class CXPLAT_SEC_CONFIG
    {
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
                NetLog.Assert(CipherSuiteStringCursor == CipherSuiteStringLength);
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
