using AKNet.Common;

namespace AKNet.Udp5Quic.Common
{
    internal enum CXPLAT_TLS_EARLY_DATA_STATE
    {
        CXPLAT_TLS_EARLY_DATA_UNKNOWN,
        CXPLAT_TLS_EARLY_DATA_UNSUPPORTED,
        CXPLAT_TLS_EARLY_DATA_REJECTED,
        CXPLAT_TLS_EARLY_DATA_ACCEPTED
    }

    internal enum CXPLAT_TLS_RESULT_FLAGS
    {
        CXPLAT_TLS_RESULT_CONTINUE = 0x0001, // Needs immediate call again. (Used internally to schannel)
        CXPLAT_TLS_RESULT_DATA = 0x0002, // Data ready to be sent.
        CXPLAT_TLS_RESULT_READ_KEY_UPDATED = 0x0004, // ReadKey variable has been updated.
        CXPLAT_TLS_RESULT_WRITE_KEY_UPDATED = 0x0008, // WriteKey variable has been updated.
        CXPLAT_TLS_RESULT_EARLY_DATA_ACCEPT = 0x0010, // The server accepted the early (0-RTT) data.
        CXPLAT_TLS_RESULT_EARLY_DATA_REJECT = 0x0020, // The server rejected the early (0-RTT) data.
        CXPLAT_TLS_RESULT_HANDSHAKE_COMPLETE = 0x0040, // Handshake complete.
        CXPLAT_TLS_RESULT_ERROR = 0x8000  // An error occured.
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
        public byte[] NegotiatedAlpn;
        public QUIC_PACKET_KEY[] ReadKeys = new QUIC_PACKET_KEY[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_COUNT];
        public QUIC_PACKET_KEY[] WriteKeys = new QUIC_PACKET_KEY[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_COUNT];
        public byte[] ClientAlpnList;
        public int ClientAlpnListLength;
    }
    
    internal static partial class MSQuicFunc
    {
        public const int TLS_SMALL_ALPN_BUFFER_SIZE = 16;

        static byte[] CxPlatTlsAlpnFindInList(int AlpnListLength, byte[] AlpnList, int FindAlpnLength, byte[] FindAlpn)
        {
            while (AlpnListLength != 0)
            {
                NetLog.Assert(AlpnList[0] + 1 <= AlpnListLength);
                if (AlpnList[0] == FindAlpnLength && orBufferEqual(AlpnList,1, FindAlpn, 0, FindAlpnLength))
                {
                    return AlpnList;
                }
                AlpnListLength -= AlpnList[0] + 1;
                AlpnList += AlpnList[0] + 1;
            }
            return null;
        }
    }
}
