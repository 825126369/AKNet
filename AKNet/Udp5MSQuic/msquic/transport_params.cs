namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_TRANSPORT_PARAMETERS : CXPLAT_POOL_Interface<QUIC_TRANSPORT_PARAMETERS>
    {
        public CXPLAT_POOL<QUIC_TRANSPORT_PARAMETERS> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<QUIC_TRANSPORT_PARAMETERS> POOL_ENTRY = null;

        public uint Flags;
        public long IdleTimeout;
        public int InitialMaxStreamDataBidiLocal;
        public int InitialMaxStreamDataBidiRemote;
        public int InitialMaxStreamDataUni;
        public int InitialMaxData;
        public int InitialMaxBidiStreams;
        public int InitialMaxUniStreams;
        public int MaxUdpPayloadSize;
        public long AckDelayExponent;
        public long MaxAckDelay; //这个是毫秒
        public long MinAckDelay; //这个是微妙
        public int ActiveConnectionIdLimit;
        public int MaxDatagramFrameSize;
        public int CibirLength;
        public int CibirOffset;
        public readonly byte[] StatelessResetToken = new byte[MSQuicFunc.QUIC_STATELESS_RESET_TOKEN_LENGTH];
        public string PreferredAddress;
        public readonly QUIC_BUFFER OriginalDestinationConnectionID = new QUIC_BUFFER(MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1);
        public readonly QUIC_BUFFER RetrySourceConnectionID = new QUIC_BUFFER(MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1);
        public QUIC_BUFFER VersionInfo = new QUIC_BUFFER();
        public readonly QUIC_BUFFER InitialSourceConnectionID = new QUIC_BUFFER(MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1);

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
            Flags = 0;
            IdleTimeout = 0;
            InitialMaxStreamDataBidiLocal = 0;
            InitialMaxStreamDataBidiRemote = 0;
            InitialMaxStreamDataUni = 0;
            InitialMaxData = 0;
            InitialMaxBidiStreams = 0;
            InitialMaxUniStreams = 0;
            MaxUdpPayloadSize = 0;
            AckDelayExponent = 0;
            MaxAckDelay = 0;
            MinAckDelay = 0;
            ActiveConnectionIdLimit = 0;
            MaxDatagramFrameSize = 0;
            CibirLength = 0;
            CibirOffset = 0;
            PreferredAddress = null;
            VersionInfo = null;
        }

        public void SetPool(CXPLAT_POOL<QUIC_TRANSPORT_PARAMETERS> mPool)
        {
            this.mPool = mPool;
        }

        public CXPLAT_POOL<QUIC_TRANSPORT_PARAMETERS> GetPool()
        {
            return this.mPool;
        }
    }
}
