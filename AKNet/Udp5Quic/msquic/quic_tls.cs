using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal enum CXPLAT_TLS_EARLY_DATA_STATE
    {
        CXPLAT_TLS_EARLY_DATA_UNKNOWN,
        CXPLAT_TLS_EARLY_DATA_UNSUPPORTED,
        CXPLAT_TLS_EARLY_DATA_REJECTED,
        CXPLAT_TLS_EARLY_DATA_ACCEPTED
    }

    internal class CXPLAT_TLS_PROCESS_STATE
    {
        public bool HandshakeComplete;
        public bool SessionResumed;
        public CXPLAT_TLS_EARLY_DATA_STATE EarlyDataState;
        public QUIC_PACKET_KEY_TYPE ReadKey;
        public QUIC_PACKET_KEY_TYPE WriteKey;
        public ushort AlertCode;
        public int BufferLength;
        public int BufferAllocLength;
        public int BufferTotalLength;
        public int BufferOffsetHandshake;
        public int BufferOffset1Rtt;
        public byte[] Buffer;
        public byte[] SmallAlpnBuffer = new byte[MSQuicFunc.TLS_SMALL_ALPN_BUFFER_SIZE];
        public QUIC_BUFFER NegotiatedAlpn;
        public QUIC_PACKET_KEY[] ReadKeys = new QUIC_PACKET_KEY[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_COUNT];
        public QUIC_PACKET_KEY[] WriteKeys = new QUIC_PACKET_KEY[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_COUNT];
        public QUIC_BUFFER ClientAlpnList;
    }

    internal class CXPLAT_TLS_CONFIG
    {
        public bool IsServer;
        public QUIC_CONNECTION Connection;
        public QUIC_HKDF_LABELS HkdfLabels;
        public CXPLAT_SEC_CONFIG SecConfig;
        public QUIC_BUFFER AlpnBuffer;
        public uint TPType;
        public string ServerName;
        public byte[] ResumptionTicketBuffer;
        public int ResumptionTicketLength;
        public QUIC_BUFFER LocalTPBuffer;
        public QUIC_TLS_SECRETS TlsSecrets;
    }

    internal enum CXPLAT_TLS_DATA_TYPE
    {
        CXPLAT_TLS_CRYPTO_DATA,
        CXPLAT_TLS_TICKET_DATA

    }
    
    internal static partial class MSQuicFunc
    {
        public const int TLS_SMALL_ALPN_BUFFER_SIZE = 16;
        public const uint TLS_EXTENSION_TYPE_APPLICATION_LAYER_PROTOCOL_NEGOTIATION = 0x0010;  // Host Byte Order
        public const uint TLS_EXTENSION_TYPE_QUIC_TRANSPORT_PARAMETERS_DRAFT = 0xffa5; // Host Byte Order
        public const uint TLS_EXTENSION_TYPE_QUIC_TRANSPORT_PARAMETERS = 0x0039;  // Host Byte Order

        public const uint CXPLAT_TLS_RESULT_CONTINUE = 0x0001; // Needs immediate call again. (Used internally to schannel)
        public const uint CXPLAT_TLS_RESULT_DATA = 0x0002; // Data ready to be sent.
        public const uint CXPLAT_TLS_RESULT_READ_KEY_UPDATED = 0x0004; // ReadKey variable has been updated.
        public const uint CXPLAT_TLS_RESULT_WRITE_KEY_UPDATED = 0x0008; // WriteKey variable has been updated.
        public const uint CXPLAT_TLS_RESULT_EARLY_DATA_ACCEPT = 0x0010; // The server accepted the early (0-RTT) data.
        public const uint CXPLAT_TLS_RESULT_EARLY_DATA_REJECT = 0x0020; // The server rejected the early (0-RTT) data.
        public const uint CXPLAT_TLS_RESULT_HANDSHAKE_COMPLETE = 0x0040; // Handshake complete.
        public const uint CXPLAT_TLS_RESULT_ERROR = 0x8000;  // An error occured.

        static QUIC_SSBuffer CxPlatTlsAlpnFindInList(QUIC_SSBuffer AlpnList, QUIC_SSBuffer FindAlpn)
        {
            while (AlpnList.Length > 0)
            {
                NetLog.Assert(AlpnList[0] + 1 <= AlpnList.Length);
                if (AlpnList[0] == FindAlpn.Length && orBufferEqual(AlpnList.Slice(1), FindAlpn))
                {
                    return AlpnList;
                }

                AlpnList.Slice(AlpnList[0] + 1);
            }
            return null;
        }

    }
}
