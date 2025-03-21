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

        //
        // RTT moving average, computed as in RFC6298. Units of microseconds.
        //
        public ulong SmoothedRtt;
        public ulong LatestRttSample;
        public ulong MinRtt;
        public ulong MaxRtt;
        public ulong RttVariance;
        public ulong OneWayDelay;
        public ulong OneWayDelayLatest;

        //
        // Used on the server side until the client's IP address has been validated
        // to prevent the server from being used for amplification attacks. A value
        // of UINT32_MAX indicates this variable does not apply.
        //
        public uint Allowance;

        //
        // The last path challenge we received and needs to be sent back as in a
        // PATH_RESPONSE frame.
        //
        public byte[] Response = new byte[8];
        //
        // The current path challenge to send and wait for the peer to echo back.
        //
        public byte[] Challenge = new byte[8];

        //
        // Time when path validation was begun. Used for timing out path validation.
        //
        public ulong PathValidationStartTime;

    }
}
