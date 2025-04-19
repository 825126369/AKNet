namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_TRANSPORT_PARAMETERS: CXPLAT_POOL_Interface<QUIC_TRANSPORT_PARAMETERS>
    {
        public readonly CXPLAT_POOL_ENTRY<QUIC_TRANSPORT_PARAMETERS> POOL_ENTRY = null;

        public uint Flags;
        public int IdleTimeout;
        public int InitialMaxStreamDataBidiLocal;
        public int InitialMaxStreamDataBidiRemote;
        public int InitialMaxStreamDataUni;
        public int InitialMaxData;
        public int InitialMaxBidiStreams;
        public int InitialMaxUniStreams;
        public int MaxUdpPayloadSize;
        public long AckDelayExponent;
        public long MaxAckDelay;
        public long MinAckDelay;
        public int ActiveConnectionIdLimit;
        public int MaxDatagramFrameSize;
        public int InitialSourceConnectionIDLength;
        public int CibirLength;
        public int CibirOffset;
        public readonly byte[] StatelessResetToken = new byte[MSQuicFunc.QUIC_STATELESS_RESET_TOKEN_LENGTH];
        public string PreferredAddress;
        public byte[] OriginalDestinationConnectionID = new byte[MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1];
        public int OriginalDestinationConnectionIDLength;
        public byte[] RetrySourceConnectionID = new byte[MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1];
        public int RetrySourceConnectionIDLength;
        public int VersionInfoLength;
        public byte[] VersionInfo = null;
        public readonly byte[] InitialSourceConnectionID = new byte[MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1];

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
