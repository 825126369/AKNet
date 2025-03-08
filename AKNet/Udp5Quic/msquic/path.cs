namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_PATH
    {
        public byte ID;

        //
        // Indicates the path object is actively in use.
        //
        public bool InUse ;

        //
        // Indicates this is the primary path being used by the connection.
        //
        public bool IsActive ;

        //
        // Indicates whether this connection initiated a CID change, and therefore
        // shouldn't respond to the peer's next CID change with one of its own.
        //
        public bool InitiatedCidUpdate ;

        //
        // Indicates that the first RTT sample has been taken. Until this is set,
        // the RTT estimate is set to a default value.
        //
        public bool GotFirstRttSample ;

        //
        // Indicates a valid (not dropped) packet has been received on this path.
        //
        public bool GotValidPacket ;

        //
        // Indicates the peer's source IP address has been validated.
        //
        public bool IsPeerValidated ;

        //
        // Indicates the minimum MTU has been validated.
        //
        public bool IsMinMtuValidated ;

        //
        // Current value to encode in the short header spin bit field.
        //
        public bool SpinBit ;

        //
        // The current path challenge needs to be sent out.
        //
        public bool SendChallenge ;

        //
        // The current path response needs to be sent out.
        //
        public bool SendResponse ;

        //
        // Indicates the partition has updated for this path.
        //
        public byte PartitionUpdated ;

        //
        // ECN validation state.
        //
        public byte EcnValidationState : 2;

        //
        // Indicates whether this connection offloads encryption workload to HW
        //
        public bool EncryptionOffloading ;

        //
        // The ending time of ECN validation testing state in microseconds.
        //
        public ulong EcnTestingEndingTime;

        //
        // The currently calculated path MTU.
        //
        public ushort Mtu;

        //
        // The local socket MTU.
        //
        public ushort LocalMtu;

        //
        // MTU Discovery logic.
        //
        QUIC_MTU_DISCOVERY MtuDiscovery;

        //
        // The binding used for sending/receiving UDP packets.
        //
        QUIC_BINDING* Binding;

        //
        // The network route.
        //
        CXPLAT_ROUTE Route;

        //
        // The destination CID used for sending on this path.
        //
        QUIC_CID_LIST_ENTRY* DestCid;

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
