namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_SETTINGS_INTERNAL
    {
        public ulong IsSetFlags;
        public class IsSet
        {
            public byte MaxBytesPerKey;
            public byte HandshakeIdleTimeoutMs;
            public byte IdleTimeoutMs;
            public byte TlsClientMaxSendBuffer;
            public byte TlsServerMaxSendBuffer;
            public byte StreamRecvWindowDefault;
            public byte StreamRecvWindowBidiLocalDefault;
            public byte StreamRecvWindowBidiRemoteDefault;
            public byte StreamRecvWindowUnidiDefault;
            public byte StreamRecvBufferDefault;
            public byte ConnFlowControlWindow;
            public byte MaxWorkerQueueDelayUs;
            public byte MaxStatelessOperations;
            public byte InitialWindowPackets;
            public byte SendIdleTimeoutMs;
            public byte InitialRttMs;
            public byte MaxAckDelayMs;
            public byte DisconnectTimeoutMs;
            public byte KeepAliveIntervalMs;
            public byte PeerBidiStreamCount;
            public byte PeerUnidiStreamCount;
            public byte RetryMemoryLimit;
            public byte LoadBalancingMode;
            public byte FixedServerID;
            public byte MaxOperationsPerDrain;
            public byte SendBufferingEnabled;
            public byte PacingEnabled;
            public byte MigrationEnabled;
            public byte DatagramReceiveEnabled;
            public byte ServerResumptionLevel;
            public byte VersionSettings;
            public byte VersionNegotiationExtEnabled;
            public byte MinimumMtu;
            public byte MaximumMtu;
            public byte MtuDiscoverySearchCompleteTimeoutUs;
            public byte MtuDiscoveryMissingProbeCount;
            public byte MaxBindingStatelessOperations;
            public byte StatelessOperationExpirationMs;
            public byte CongestionControlAlgorithm;
            public byte DestCidUpdateIdleTimeoutMs;
            public byte GreaseQuicBitEnabled;
            public byte EcnEnabled;
            public byte HyStartEnabled;
            public byte EncryptionOffloadAllowed;
            public byte ReliableResetEnabled;
            public byte OneWayDelayEnabled;
            public byte NetStatsEventEnabled;
            public byte StreamMultiReceiveEnabled;
            public byte RESERVED;
        }
        
        public QUIC_VERSION_SETTINGS VersionSettings;
        public byte MaxBytesPerKey;
        public byte HandshakeIdleTimeoutMs;
        public byte IdleTimeoutMs;
        public byte MtuDiscoverySearchCompleteTimeoutUs;
        public uint TlsClientMaxSendBuffer;
        public uint TlsServerMaxSendBuffer;
        public uint StreamRecvWindowDefault;
        public uint StreamRecvWindowBidiLocalDefault;
        public uint StreamRecvWindowBidiRemoteDefault;
        public uint StreamRecvWindowUnidiDefault;
        public uint StreamRecvBufferDefault;
        public uint ConnFlowControlWindow;
        public uint MaxWorkerQueueDelayUs;
        public uint MaxStatelessOperations;
        public uint InitialWindowPackets;
        public uint SendIdleTimeoutMs;
        public uint InitialRttMs;
        public uint MaxAckDelayMs;
        public uint DisconnectTimeoutMs;
        public uint KeepAliveIntervalMs;
        public uint DestCidUpdateIdleTimeoutMs;
        public uint FixedServerID;                 // Global only
        public ushort PeerBidiStreamCount;
        public ushort PeerUnidiStreamCount;
        public ushort RetryMemoryLimit;              // Global only
        public ushort LoadBalancingMode;             // Global only
        public ushort MinimumMtu;
        public ushort MaximumMtu;
        public ushort MaxBindingStatelessOperations;
        public ushort StatelessOperationExpirationMs;
        public ushort CongestionControlAlgorithm;
        public byte MaxOperationsPerDrain;
        public byte SendBufferingEnabled;
        public byte PacingEnabled;
        public byte MigrationEnabled;
        public byte DatagramReceiveEnabled;
        public byte ServerResumptionLevel           : 2;    // QUIC_SERVER_RESUMPTION_LEVEL
        public byte VersionNegotiationExtEnabled;
        public byte GreaseQuicBitEnabled;
        public byte EcnEnabled;
        public byte HyStartEnabled;
        public byte EncryptionOffloadAllowed;
        public byte ReliableResetEnabled;
        public byte OneWayDelayEnabled;
        public byte NetStatsEventEnabled;
        public bool StreamMultiReceiveEnabled;
        public byte MtuDiscoveryMissingProbeCount;
    }

}
