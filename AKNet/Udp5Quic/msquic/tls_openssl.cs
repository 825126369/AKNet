using System;

namespace AKNet.Udp5Quic.Common
{
    public class CXPLAT_SEC_CONFIG
    {
        
    }

    internal class CXPLAT_TLS
    {
        public CXPLAT_SEC_CONFIG SecConfig;
        //public QUIC_HKDF_LABELS HkdfLabels;

        //
        // Indicates if this context belongs to server side or client side
        // connection.
        //
        BOOLEAN IsServer : 1;

        //
        // Indicates if the peer sent a certificate.
        //
        BOOLEAN PeerCertReceived : 1;

        //
        // Indicates whether the peer's TP has been received.
        //
        BOOLEAN PeerTPReceived : 1;

        //
        // The TLS extension type for the QUIC transport parameters.
        //
        uint16_t QuicTpExtType;

        //
        // The ALPN buffer.
        //
        uint16_t AlpnBufferLength;
        const uint8_t* AlpnBuffer;

        //
        // On client side stores a NULL terminated SNI.
        //
        const char* SNI;

        //
        // Ssl - A SSL object associated with the connection.
        //
        SSL* Ssl;

        //
        // State - The TLS state associated with the connection.
        // ResultFlags - Stores the result of the TLS data processing operation.
        //

        CXPLAT_TLS_PROCESS_STATE* State;
        CXPLAT_TLS_RESULT_FLAGS ResultFlags;

        //
        // Callback context and handler for QUIC TP.
        //
        QUIC_CONNECTION* Connection;

        //
        // Optional struct to log TLS traffic secrets.
        // Only non-null when the connection is configured to log these.
        //
        QUIC_TLS_SECRETS* TlsSecrets;

    }
}
