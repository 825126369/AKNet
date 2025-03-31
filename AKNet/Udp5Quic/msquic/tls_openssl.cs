using System;

namespace AKNet.Udp5Quic.Common
{
    public class CXPLAT_SEC_CONFIG
    {
        
    }

    internal class CXPLAT_TLS
    {
        public CXPLAT_SEC_CONFIG SecConfig;
       // public QUIC_HKDF_LABELS HkdfLabels;
        public bool IsServer;
        public bool PeerCertReceived;
        public bool PeerTPReceived;

        public ushort QuicTpExtType;
        public ushort AlpnBufferLength;
        public byte[] AlpnBuffer;
        public byte[] SNI;
        //public SSL Ssl;
        CXPLAT_TLS_PROCESS_STATE State;
        CXPLAT_TLS_RESULT_FLAGS ResultFlags;
        public QUIC_CONNECTION Connection;
        public QUIC_TLS_SECRETS TlsSecrets;

    }
}
