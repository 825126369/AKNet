namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_SETTINGS_INTERNAL
    {
        public ulong IsSetFlags;
        public class IsSet_Class
        {
            public bool MaxBytesPerKey;
            public bool HandshakeIdleTimeoutMs;
            public bool IdleTimeoutMs;
            public bool TlsClientMaxSendBuffer;
            public bool TlsServerMaxSendBuffer;
            public bool StreamRecvWindowDefault;
            public bool StreamRecvWindowBidiLocalDefault;
            public bool StreamRecvWindowBidiRemoteDefault;
            public bool StreamRecvWindowUnidiDefault;
            public bool StreamRecvBufferDefault;
            public bool ConnFlowControlWindow;
            public bool MaxWorkerQueueDelayUs;
            public bool MaxStatelessOperations;
            public bool InitialWindowPackets;
            public bool SendIdleTimeoutMs;
            public bool InitialRttMs;
            public bool MaxAckDelayMs;
            public bool DisconnectTimeoutMs;
            public bool KeepAliveIntervalMs;
            public bool PeerBidiStreamCount;
            public bool PeerUnidiStreamCount;
            public bool RetryMemoryLimit;
            public bool LoadBalancingMode;
            public bool FixedServerID;
            public bool MaxOperationsPerDrain;
            public bool SendBufferingEnabled;
            public bool PacingEnabled;
            public bool MigrationEnabled;
            public bool DatagramReceiveEnabled;
            public bool ServerResumptionLevel;
            public bool VersionSettings;
            public bool VersionNegotiationExtEnabled;
            public bool MinimumMtu;
            public bool MaximumMtu;
            public bool MtuDiscoverySearchCompleteTimeoutUs;
            public bool MtuDiscoveryMissingProbeCount;
            public bool MaxBindingStatelessOperations;
            public bool StatelessOperationExpirationMs;
            public bool CongestionControlAlgorithm;
            public bool DestCidUpdateIdleTimeoutMs;
            public bool GreaseQuicBitEnabled;
            public bool EcnEnabled;
            public bool HyStartEnabled;
            public bool EncryptionOffloadAllowed;
            public bool ReliableResetEnabled;
            public bool OneWayDelayEnabled;
            public bool NetStatsEventEnabled;
            public bool StreamMultiReceiveEnabled;
            public byte RESERVED;
        }

        public IsSet_Class IsSet;
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
        public bool SendBufferingEnabled;
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

    internal static partial class MSQuicFunc
    {
        static void QuicSettingsCopy(QUIC_SETTINGS_INTERNAL Destination, QUIC_SETTINGS_INTERNAL Source)
        {
            if (!Destination.IsSet.SendBufferingEnabled)
            {
                Destination.SendBufferingEnabled = Source.SendBufferingEnabled;
            }
            if (!Destination.IsSet.PacingEnabled)
            {
                Destination.PacingEnabled = Source.PacingEnabled;
            }

            if (!Destination.IsSet.MigrationEnabled)
            {
                Destination.MigrationEnabled = Source.MigrationEnabled;
            }
            if (!Destination.IsSet.DatagramReceiveEnabled)
            {
                Destination.DatagramReceiveEnabled = Source.DatagramReceiveEnabled;
            }
            if (!Destination.IsSet.MaxOperationsPerDrain)
            {
                Destination.MaxOperationsPerDrain = Source.MaxOperationsPerDrain;
            }
            if (!Destination.IsSet.RetryMemoryLimit)
            {
                Destination.RetryMemoryLimit = Source.RetryMemoryLimit;
            }
            if (!Destination.IsSet.LoadBalancingMode)
            {
                Destination.LoadBalancingMode = Source.LoadBalancingMode;
            }
            if (!Destination.IsSet.FixedServerID)
            {
                Destination.FixedServerID = Source.FixedServerID;
            }
            if (!Destination.IsSet.MaxWorkerQueueDelayUs)
            {
                Destination.MaxWorkerQueueDelayUs = Source.MaxWorkerQueueDelayUs;
            }
            if (!Destination.IsSet.MaxStatelessOperations)
            {
                Destination.MaxStatelessOperations = Source.MaxStatelessOperations;
            }
            if (!Destination.IsSet.InitialWindowPackets)
            {
                Destination.InitialWindowPackets = Source.InitialWindowPackets;
            }
            if (!Destination.IsSet.SendIdleTimeoutMs)
            {
                Destination.SendIdleTimeoutMs = Source.SendIdleTimeoutMs;
            }
            if (!Destination.IsSet.InitialRttMs)
            {
                Destination.InitialRttMs = Source.InitialRttMs;
            }
            if (!Destination.IsSet.MaxAckDelayMs)
            {
                Destination.MaxAckDelayMs = Source.MaxAckDelayMs;
            }
            if (!Destination.IsSet.DisconnectTimeoutMs)
            {
                Destination.DisconnectTimeoutMs = Source.DisconnectTimeoutMs;
            }
            if (!Destination.IsSet.KeepAliveIntervalMs)
            {
                Destination.KeepAliveIntervalMs = Source.KeepAliveIntervalMs;
            }
            if (!Destination.IsSet.IdleTimeoutMs)
            {
                Destination.IdleTimeoutMs = Source.IdleTimeoutMs;
            }
            if (!Destination.IsSet.HandshakeIdleTimeoutMs)
            {
                Destination.HandshakeIdleTimeoutMs = Source.HandshakeIdleTimeoutMs;
            }
            if (!Destination.IsSet.PeerBidiStreamCount)
            {
                Destination.PeerBidiStreamCount = Source.PeerBidiStreamCount;
            }
            if (!Destination.IsSet.PeerUnidiStreamCount)
            {
                Destination.PeerUnidiStreamCount = Source.PeerUnidiStreamCount;
            }
            if (!Destination.IsSet.TlsClientMaxSendBuffer)
            {
                Destination.TlsClientMaxSendBuffer = Source.TlsClientMaxSendBuffer;
            }
            if (!Destination.IsSet.TlsClientMaxSendBuffer)
            {
                Destination.TlsClientMaxSendBuffer = Source.TlsClientMaxSendBuffer;
            }
            if (!Destination.IsSet.StreamRecvWindowDefault)
            {
                Destination.StreamRecvWindowDefault = Source.StreamRecvWindowDefault;
            }
            if (!Destination.IsSet.StreamRecvWindowBidiLocalDefault)
            {
                Destination.StreamRecvWindowBidiLocalDefault = Source.StreamRecvWindowBidiLocalDefault;
            }
            if (!Destination.IsSet.StreamRecvWindowBidiRemoteDefault)
            {
                Destination.StreamRecvWindowBidiRemoteDefault = Source.StreamRecvWindowBidiRemoteDefault;
            }
            if (!Destination.IsSet.StreamRecvWindowUnidiDefault)
            {
                Destination.StreamRecvWindowUnidiDefault = Source.StreamRecvWindowUnidiDefault;
            }
            if (!Destination.IsSet.StreamRecvBufferDefault)
            {
                Destination.StreamRecvBufferDefault = Source.StreamRecvBufferDefault;
            }
            if (!Destination.IsSet.ConnFlowControlWindow)
            {
                Destination.ConnFlowControlWindow = Source.ConnFlowControlWindow;
            }
            if (!Destination.IsSet.MaxBytesPerKey)
            {
                Destination.MaxBytesPerKey = Source.MaxBytesPerKey;
            }
            if (!Destination.IsSet.ServerResumptionLevel)
            {
                Destination.ServerResumptionLevel = Source.ServerResumptionLevel;
            }
            if (!Destination.IsSet.VersionNegotiationExtEnabled)
            {
                Destination.VersionNegotiationExtEnabled = Source.VersionNegotiationExtEnabled;
            }
            if (!Destination.IsSet.VersionSettings)
            {
                if (Destination.VersionSettings)
                {
                    CXPLAT_FREE(Destination.VersionSettings, QUIC_POOL_VERSION_SETTINGS);
                    Destination.VersionSettings = NULL;
                }
                if (Source.VersionSettings != NULL)
                {
                    Destination.VersionSettings =
                        QuicSettingsCopyVersionSettings(Source.VersionSettings, FALSE);
                }
            }

            if (!Destination.IsSet.MinimumMtu && !Destination.IsSet.MaximumMtu)
            {
                Destination.MinimumMtu = Source.MinimumMtu;
                Destination.MaximumMtu = Source.MaximumMtu;
            }
            else if (Destination.IsSet.MinimumMtu && !Destination.IsSet.MaximumMtu)
            {
                if (Source.MaximumMtu > Destination.MinimumMtu)
                {
                    Destination.MaximumMtu = Source.MaximumMtu;
                }
            }
            else if (Destination.IsSet.MaximumMtu && !Destination.IsSet.MinimumMtu)
            {
                if (Source.MinimumMtu < Destination.MaximumMtu)
                {
                    Destination.MinimumMtu = Source.MinimumMtu;
                }
            }

            if (!Destination.IsSet.MtuDiscoveryMissingProbeCount)
            {
                Destination.MtuDiscoveryMissingProbeCount = Source.MtuDiscoveryMissingProbeCount;
            }
            if (!Destination.IsSet.MtuDiscoverySearchCompleteTimeoutUs)
            {
                Destination.MtuDiscoverySearchCompleteTimeoutUs = Source.MtuDiscoverySearchCompleteTimeoutUs;
            }
            if (!Destination.IsSet.MaxBindingStatelessOperations)
            {
                Destination.MaxBindingStatelessOperations = Source.MaxBindingStatelessOperations;
            }
            if (!Destination.IsSet.StatelessOperationExpirationMs)
            {
                Destination.StatelessOperationExpirationMs = Source.StatelessOperationExpirationMs;
            }
            if (!Destination.IsSet.CongestionControlAlgorithm)
            {
                Destination.CongestionControlAlgorithm = Source.CongestionControlAlgorithm;
            }
            if (!Destination.IsSet.DestCidUpdateIdleTimeoutMs)
            {
                Destination.DestCidUpdateIdleTimeoutMs = Source.DestCidUpdateIdleTimeoutMs;
            }
            if (!Destination.IsSet.GreaseQuicBitEnabled)
            {
                Destination.GreaseQuicBitEnabled = Source.GreaseQuicBitEnabled;
            }
            if (!Destination.IsSet.EcnEnabled)
            {
                Destination.EcnEnabled = Source.EcnEnabled;
            }
            if (!Destination.IsSet.HyStartEnabled)
            {
                Destination.HyStartEnabled = Source.HyStartEnabled;
            }
            if (!Destination.IsSet.EncryptionOffloadAllowed)
            {
                Destination.EncryptionOffloadAllowed = Source.EncryptionOffloadAllowed;
            }
            if (!Destination.IsSet.ReliableResetEnabled)
            {
                Destination.ReliableResetEnabled = Source.ReliableResetEnabled;
            }
            if (!Destination.IsSet.OneWayDelayEnabled)
            {
                Destination.OneWayDelayEnabled = Source.OneWayDelayEnabled;
            }
            if (!Destination.IsSet.NetStatsEventEnabled)
            {
                Destination.NetStatsEventEnabled = Source.NetStatsEventEnabled;
            }
            if (!Destination.IsSet.StreamMultiReceiveEnabled)
            {
                Destination.StreamMultiReceiveEnabled = Source.StreamMultiReceiveEnabled;
            }
        }
    }

}
