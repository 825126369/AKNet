namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_PATH
    {
        public byte ID;
        public bool InUse;
        public bool IsActive;
        public bool InitiatedCidUpdate;
        public bool GotFirstRttSample;
        public bool GotValidPacket;
        public bool IsPeerValidated;
        public bool IsMinMtuValidated;
        public bool SpinBit;
        public bool SendChallenge;
        public bool SendResponse;
        public byte PartitionUpdated;
        public byte EcnValidationState;
        public bool EncryptionOffloading;
        public ulong EcnTestingEndingTime;
        public ushort Mtu;
        public ushort LocalMtu;
        public QUIC_MTU_DISCOVERY MtuDiscovery;
        public QUIC_BINDING Binding;
        public CXPLAT_ROUTE Route;
        public QUIC_CID_LIST_ENTRY DestCid;
        
        public ulong SmoothedRtt;
        public ulong LatestRttSample;
        public ulong MinRtt;
        public ulong MaxRtt;
        public ulong RttVariance;
        public ulong OneWayDelay;
        public ulong OneWayDelayLatest;
        
        public uint Allowance;
        public byte[] Response = new byte[8];
        public byte[] Challenge = new byte[8];
        public ulong PathValidationStartTime;
    }
}
