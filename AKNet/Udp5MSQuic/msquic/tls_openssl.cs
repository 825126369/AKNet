using AKNet.Common;
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
