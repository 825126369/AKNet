namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_TRANSPORT_PARAMETERS: CXPLAT_POOL_Interface<QUIC_TRANSPORT_PARAMETERS>
    {
        public readonly CXPLAT_POOL_ENTRY<QUIC_TRANSPORT_PARAMETERS> POOL_ENTRY = null;

        public uint Flags;
        public long IdleTimeout;
        public ulong InitialMaxStreamDataBidiLocal;
        public ulong InitialMaxStreamDataBidiRemote;
        public ulong InitialMaxStreamDataUni;
        public ulong InitialMaxData;
        public ulong InitialMaxBidiStreams;
        public ulong InitialMaxUniStreams;
        public ulong MaxUdpPayloadSize;

        [_Field_range_(0, MSQuicFunc.QUIC_TP_ACK_DELAY_EXPONENT_MAX)]
        public long AckDelayExponent;
        [_Field_range_(0, MSQuicFunc.QUIC_TP_MAX_ACK_DELAY_MAX)]
        public long MaxAckDelay;
        [_Field_range_(0, MSQuicFunc.QUIC_TP_MIN_ACK_DELAY_MAX)]
        public long MinAckDelay;
        [_Field_range_(MSQuicFunc.QUIC_TP_ACTIVE_CONNECTION_ID_LIMIT_MIN, MSQuicFunc.QUIC_VAR_INT_MAX)]
        public long ActiveConnectionIdLimit;
        public long MaxDatagramFrameSize;
        public byte[] InitialSourceConnectionID = new byte[MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1];
        [_Field_range_(0, MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1)]
        public byte InitialSourceConnectionIDLength;
        public long CibirLength;
        public long CibirOffset;
        public byte[] StatelessResetToken = new byte[MSQuicFunc.QUIC_STATELESS_RESET_TOKEN_LENGTH];
        public string PreferredAddress;
        public byte[] OriginalDestinationConnectionID = new byte[MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1];
        [_Field_range_(0, MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1)]
        public byte OriginalDestinationConnectionIDLength;
        public byte[] RetrySourceConnectionID = new byte[MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1];
        [_Field_range_(0, MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1)]
        public byte RetrySourceConnectionIDLength;
        public int VersionInfoLength;
        public byte[] VersionInfo = null;


        public QUIC_TRANSPORT_PARAMETERS()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_TRANSPORT_PARAMETERS>(this);
        }
        public CXPLAT_POOL_ENTRY<QUIC_TRANSPORT_PARAMETERS> GetEntry()
        {
            return POOL_ENTRY;
        }
        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}
