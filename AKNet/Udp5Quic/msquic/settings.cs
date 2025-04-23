using System.Runtime;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_SETTINGS_INTERNAL
    {
        public QUIC_VERSION_SETTINGS VersionSettings;
        public ulong IsSetFlags;
        public IsSet_DATA IsSet;
        public class IsSet_DATA
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
            public bool RESERVED;
        }

        public byte MaxBytesPerKey;
        public byte HandshakeIdleTimeoutMs;
        public byte IdleTimeoutMs;
        public byte MtuDiscoverySearchCompleteTimeoutUs;
        public uint TlsClientMaxSendBuffer;
        public uint TlsServerMaxSendBuffer;
        public uint StreamRecvWindowDefault;
        public int StreamRecvWindowBidiLocalDefault;
        public int StreamRecvWindowBidiRemoteDefault;
        public int StreamRecvWindowUnidiDefault;
        public uint StreamRecvBufferDefault;
        public uint ConnFlowControlWindow;
        public uint MaxWorkerQueueDelayUs;
        public uint MaxStatelessOperations;
        public uint InitialWindowPackets;
        public long SendIdleTimeoutMs;
        public long InitialRttMs;
        public long MaxAckDelayMs;
        public long DisconnectTimeoutMs;
        public long KeepAliveIntervalMs;
        public long DestCidUpdateIdleTimeoutMs;
        public uint FixedServerID;                 // Global only
        public ushort PeerBidiStreamCount;
        public ushort PeerUnidiStreamCount;
        public ushort RetryMemoryLimit;              // Global only
        public QUIC_LOAD_BALANCING_MODE LoadBalancingMode;             // Global only
        public ushort MinimumMtu;
        public ushort MaximumMtu;
        public ushort MaxBindingStatelessOperations;
        public ushort StatelessOperationExpirationMs;
        public QUIC_CONGESTION_CONTROL_ALGORITHM CongestionControlAlgorithm;
        public byte MaxOperationsPerDrain;
        public bool SendBufferingEnabled;
        public bool PacingEnabled;
        public bool MigrationEnabled;
        public bool DatagramReceiveEnabled;
        public QUIC_SERVER_RESUMPTION_LEVEL ServerResumptionLevel;    // QUIC_SERVER_RESUMPTION_LEVEL
        public bool VersionNegotiationExtEnabled;
        public bool GreaseQuicBitEnabled;
        public bool EcnEnabled;
        public bool HyStartEnabled;
        public byte EncryptionOffloadAllowed;
        public bool ReliableResetEnabled;
        public bool OneWayDelayEnabled;
        public bool NetStatsEventEnabled;
        public bool StreamMultiReceiveEnabled;
        public byte MtuDiscoveryMissingProbeCount;
    }

    internal static partial class MSQuicFunc
    {

        static ulong QuicSettingsSettingsToInternal(int SettingsSize, QUIC_SETTINGS_INTERNAL Settings, QUIC_SETTINGS_INTERNAL InternalSettings)
        {
            //InternalSettings.IsSetFlags = 0;
            //SETTING_COPY_TO_INTERNAL(MaxBytesPerKey, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(HandshakeIdleTimeoutMs, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(IdleTimeoutMs, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(MtuDiscoverySearchCompleteTimeoutUs, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(TlsClientMaxSendBuffer, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(TlsServerMaxSendBuffer, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(StreamRecvWindowDefault, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(StreamRecvBufferDefault, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(ConnFlowControlWindow, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(MaxWorkerQueueDelayUs, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(MaxStatelessOperations, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(InitialWindowPackets, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(SendIdleTimeoutMs, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(InitialRttMs, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(MaxAckDelayMs, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(DisconnectTimeoutMs, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(KeepAliveIntervalMs, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(CongestionControlAlgorithm, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(PeerBidiStreamCount, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(PeerUnidiStreamCount, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(MaxBindingStatelessOperations, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(StatelessOperationExpirationMs, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(MinimumMtu, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(MaximumMtu, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(MaxOperationsPerDrain, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(MtuDiscoveryMissingProbeCount, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(SendBufferingEnabled, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(PacingEnabled, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(MigrationEnabled, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(DatagramReceiveEnabled, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(ServerResumptionLevel, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(GreaseQuicBitEnabled, Settings, InternalSettings);
            //SETTING_COPY_TO_INTERNAL(EcnEnabled, Settings, InternalSettings);

            ////
            //// N.B. Anything after this needs to be size checked
            ////

            ////
            //// The below is how to add a new field while checking size.
            ////
            //// SETTING_COPY_TO_INTERNAL_SIZED(
            ////     MtuDiscoveryMissingProbeCount,
            ////     QUIC_SETTINGS,
            ////     Settings,
            ////     SettingsSize,
            ////     InternalSettings);

            //SETTING_COPY_TO_INTERNAL_SIZED(
            //    DestCidUpdateIdleTimeoutMs,
            //    QUIC_SETTINGS,
            //    Settings,
            //    SettingsSize,
            //    InternalSettings);

            //SETTING_COPY_FLAG_TO_INTERNAL_SIZED(
            //    Flags,
            //    HyStartEnabled,
            //    QUIC_SETTINGS,
            //    Settings,
            //    SettingsSize,
            //    InternalSettings);

            //SETTING_COPY_FLAG_TO_INTERNAL_SIZED(
            //    Flags,
            //    EncryptionOffloadAllowed,
            //    QUIC_SETTINGS,
            //    Settings,
            //    SettingsSize,
            //    InternalSettings);

            //SETTING_COPY_FLAG_TO_INTERNAL_SIZED(
            //    Flags,
            //    ReliableResetEnabled,
            //    QUIC_SETTINGS,
            //    Settings,
            //    SettingsSize,
            //    InternalSettings);

            //SETTING_COPY_FLAG_TO_INTERNAL_SIZED(
            //    Flags,
            //    OneWayDelayEnabled,
            //    QUIC_SETTINGS,
            //    Settings,
            //    SettingsSize,
            //    InternalSettings);

            //SETTING_COPY_TO_INTERNAL_SIZED(
            //    StreamRecvWindowBidiLocalDefault,
            //    QUIC_SETTINGS,
            //    Settings,
            //    SettingsSize,
            //    InternalSettings);

            //SETTING_COPY_TO_INTERNAL_SIZED(
            //    StreamRecvWindowBidiRemoteDefault,
            //    QUIC_SETTINGS,
            //    Settings,
            //    SettingsSize,
            //    InternalSettings);

            //SETTING_COPY_TO_INTERNAL_SIZED(
            //    StreamRecvWindowUnidiDefault,
            //    QUIC_SETTINGS,
            //    Settings,
            //    SettingsSize,
            //    InternalSettings);

            //SETTING_COPY_FLAG_TO_INTERNAL_SIZED(
            //    Flags,
            //    NetStatsEventEnabled,
            //    QUIC_SETTINGS,
            //    Settings,
            //    SettingsSize,
            //    InternalSettings);

            //SETTING_COPY_FLAG_TO_INTERNAL_SIZED(
            //    Flags,
            //    StreamMultiReceiveEnabled,
            //    QUIC_SETTINGS,
            //    Settings,
            //    SettingsSize,
            //    InternalSettings);

            return QUIC_STATUS_SUCCESS;
        }

        static void QuicSettingsCopy(QUIC_SETTINGS_INTERNAL Destination, QUIC_SETTINGS_INTERNAL Source)
        {
            //if (!Destination.IsSet.SendBufferingEnabled)
            //{
            //    Destination.SendBufferingEnabled = Source.SendBufferingEnabled;
            //}
            //if (!Destination.IsSet.PacingEnabled)
            //{
            //    Destination.PacingEnabled = Source.PacingEnabled;
            //}

            //if (!Destination.IsSet.MigrationEnabled)
            //{
            //    Destination.MigrationEnabled = Source.MigrationEnabled;
            //}
            //if (!Destination.IsSet.DatagramReceiveEnabled)
            //{
            //    Destination.DatagramReceiveEnabled = Source.DatagramReceiveEnabled;
            //}
            //if (!Destination.IsSet.MaxOperationsPerDrain)
            //{
            //    Destination.MaxOperationsPerDrain = Source.MaxOperationsPerDrain;
            //}
            //if (!Destination.IsSet.RetryMemoryLimit)
            //{
            //    Destination.RetryMemoryLimit = Source.RetryMemoryLimit;
            //}
            //if (!Destination.IsSet.LoadBalancingMode)
            //{
            //    Destination.LoadBalancingMode = Source.LoadBalancingMode;
            //}
            //if (!Destination.IsSet.FixedServerID)
            //{
            //    Destination.FixedServerID = Source.FixedServerID;
            //}
            //if (!Destination.IsSet.MaxWorkerQueueDelayUs)
            //{
            //    Destination.MaxWorkerQueueDelayUs = Source.MaxWorkerQueueDelayUs;
            //}
            //if (!Destination.IsSet.MaxStatelessOperations)
            //{
            //    Destination.MaxStatelessOperations = Source.MaxStatelessOperations;
            //}
            //if (!Destination.IsSet.InitialWindowPackets)
            //{
            //    Destination.InitialWindowPackets = Source.InitialWindowPackets;
            //}
            //if (!Destination.IsSet.SendIdleTimeoutMs)
            //{
            //    Destination.SendIdleTimeoutMs = Source.SendIdleTimeoutMs;
            //}
            //if (!Destination.IsSet.InitialRttMs)
            //{
            //    Destination.InitialRttMs = Source.InitialRttMs;
            //}
            //if (!Destination.IsSet.MaxAckDelayMs)
            //{
            //    Destination.MaxAckDelayMs = Source.MaxAckDelayMs;
            //}
            //if (!Destination.IsSet.DisconnectTimeoutMs)
            //{
            //    Destination.DisconnectTimeoutMs = Source.DisconnectTimeoutMs;
            //}
            //if (!Destination.IsSet.KeepAliveIntervalMs)
            //{
            //    Destination.KeepAliveIntervalMs = Source.KeepAliveIntervalMs;
            //}
            //if (!Destination.IsSet.IdleTimeoutMs)
            //{
            //    Destination.IdleTimeoutMs = Source.IdleTimeoutMs;
            //}
            //if (!Destination.IsSet.HandshakeIdleTimeoutMs)
            //{
            //    Destination.HandshakeIdleTimeoutMs = Source.HandshakeIdleTimeoutMs;
            //}
            //if (!Destination.IsSet.PeerBidiStreamCount)
            //{
            //    Destination.PeerBidiStreamCount = Source.PeerBidiStreamCount;
            //}
            //if (!Destination.IsSet.PeerUnidiStreamCount)
            //{
            //    Destination.PeerUnidiStreamCount = Source.PeerUnidiStreamCount;
            //}
            //if (!Destination.IsSet.TlsClientMaxSendBuffer)
            //{
            //    Destination.TlsClientMaxSendBuffer = Source.TlsClientMaxSendBuffer;
            //}
            //if (!Destination.IsSet.TlsClientMaxSendBuffer)
            //{
            //    Destination.TlsClientMaxSendBuffer = Source.TlsClientMaxSendBuffer;
            //}
            //if (!Destination.IsSet.StreamRecvWindowDefault)
            //{
            //    Destination.StreamRecvWindowDefault = Source.StreamRecvWindowDefault;
            //}
            //if (!Destination.IsSet.StreamRecvWindowBidiLocalDefault)
            //{
            //    Destination.StreamRecvWindowBidiLocalDefault = Source.StreamRecvWindowBidiLocalDefault;
            //}
            //if (!Destination.IsSet.StreamRecvWindowBidiRemoteDefault)
            //{
            //    Destination.StreamRecvWindowBidiRemoteDefault = Source.StreamRecvWindowBidiRemoteDefault;
            //}
            //if (!Destination.IsSet.StreamRecvWindowUnidiDefault)
            //{
            //    Destination.StreamRecvWindowUnidiDefault = Source.StreamRecvWindowUnidiDefault;
            //}
            //if (!Destination.IsSet.StreamRecvBufferDefault)
            //{
            //    Destination.StreamRecvBufferDefault = Source.StreamRecvBufferDefault;
            //}
            //if (!Destination.IsSet.ConnFlowControlWindow)
            //{
            //    Destination.ConnFlowControlWindow = Source.ConnFlowControlWindow;
            //}
            //if (!Destination.IsSet.MaxBytesPerKey)
            //{
            //    Destination.MaxBytesPerKey = Source.MaxBytesPerKey;
            //}
            //if (!Destination.IsSet.ServerResumptionLevel)
            //{
            //    Destination.ServerResumptionLevel = Source.ServerResumptionLevel;
            //}
            //if (!Destination.IsSet.VersionNegotiationExtEnabled)
            //{
            //    Destination.VersionNegotiationExtEnabled = Source.VersionNegotiationExtEnabled;
            //}
            //if (!Destination.IsSet.VersionSettings)
            //{
            //    Destination.VersionSettings = null;
            //    if (Source.VersionSettings != null)
            //    {
            //        Destination.VersionSettings = QuicSettingsCopyVersionSettings(Source.VersionSettings, false);
            //    }
            //}

            //if (!Destination.IsSet.MinimumMtu && !Destination.IsSet.MaximumMtu)
            //{
            //    Destination.MinimumMtu = Source.MinimumMtu;
            //    Destination.MaximumMtu = Source.MaximumMtu;
            //}
            //else if (Destination.IsSet.MinimumMtu && !Destination.IsSet.MaximumMtu)
            //{
            //    if (Source.MaximumMtu > Destination.MinimumMtu)
            //    {
            //        Destination.MaximumMtu = Source.MaximumMtu;
            //    }
            //}
            //else if (Destination.IsSet.MaximumMtu && !Destination.IsSet.MinimumMtu)
            //{
            //    if (Source.MinimumMtu < Destination.MaximumMtu)
            //    {
            //        Destination.MinimumMtu = Source.MinimumMtu;
            //    }
            //}

            //if (!Destination.IsSet.MtuDiscoveryMissingProbeCount)
            //{
            //    Destination.MtuDiscoveryMissingProbeCount = Source.MtuDiscoveryMissingProbeCount;
            //}
            //if (!Destination.IsSet.MtuDiscoverySearchCompleteTimeoutUs)
            //{
            //    Destination.MtuDiscoverySearchCompleteTimeoutUs = Source.MtuDiscoverySearchCompleteTimeoutUs;
            //}
            //if (!Destination.IsSet.MaxBindingStatelessOperations)
            //{
            //    Destination.MaxBindingStatelessOperations = Source.MaxBindingStatelessOperations;
            //}
            //if (!Destination.IsSet.StatelessOperationExpirationMs)
            //{
            //    Destination.StatelessOperationExpirationMs = Source.StatelessOperationExpirationMs;
            //}
            //if (!Destination.IsSet.CongestionControlAlgorithm)
            //{
            //    Destination.CongestionControlAlgorithm = Source.CongestionControlAlgorithm;
            //}
            //if (!Destination.IsSet.DestCidUpdateIdleTimeoutMs)
            //{
            //    Destination.DestCidUpdateIdleTimeoutMs = Source.DestCidUpdateIdleTimeoutMs;
            //}
            //if (!Destination.IsSet.GreaseQuicBitEnabled)
            //{
            //    Destination.GreaseQuicBitEnabled = Source.GreaseQuicBitEnabled;
            //}
            //if (!Destination.IsSet.EcnEnabled)
            //{
            //    Destination.EcnEnabled = Source.EcnEnabled;
            //}
            //if (!Destination.IsSet.HyStartEnabled)
            //{
            //    Destination.HyStartEnabled = Source.HyStartEnabled;
            //}
            //if (!Destination.IsSet.EncryptionOffloadAllowed)
            //{
            //    Destination.EncryptionOffloadAllowed = Source.EncryptionOffloadAllowed;
            //}
            //if (!Destination.IsSet.ReliableResetEnabled)
            //{
            //    Destination.ReliableResetEnabled = Source.ReliableResetEnabled;
            //}
            //if (!Destination.IsSet.OneWayDelayEnabled)
            //{
            //    Destination.OneWayDelayEnabled = Source.OneWayDelayEnabled;
            //}
            //if (!Destination.IsSet.NetStatsEventEnabled)
            //{
            //    Destination.NetStatsEventEnabled = Source.NetStatsEventEnabled;
            //}
            //if (!Destination.IsSet.StreamMultiReceiveEnabled)
            //{
            //    Destination.StreamMultiReceiveEnabled = Source.StreamMultiReceiveEnabled;
            //}
        }

        static QUIC_VERSION_SETTINGS QuicSettingsCopyVersionSettings(QUIC_VERSION_SETTINGS Source, bool CopyExternalToInternal)
        {
            QUIC_VERSION_SETTINGS Destination = null;
            //int AllocSize = sizeof(*Destination) + (Source.AcceptableVersionsLength * sizeof(int)) +  
            //    (Source.OfferedVersionsLength * sizeof(int)) + (Source.FullyDeployedVersionsLength * sizeof(int));

            //Destination = CXPLAT_ALLOC_NONPAGED(AllocSize, QUIC_POOL_VERSION_SETTINGS);
            //if (Destination == null)
            //{
            //    return Destination;
            //}

            //Destination.AcceptableVersions = Destination + 1;
            //Destination.AcceptableVersionsLength = Source.AcceptableVersionsLength;
            //CxPlatCopyMemory((public int*)Destination->AcceptableVersions, Source.AcceptableVersions,Destination.AcceptableVersionsLength * sizeof(int));

            //Destination.OfferedVersions =
            //    Destination.AcceptableVersions + Destination.AcceptableVersionsLength;
            //Destination.OfferedVersionsLength = Source.OfferedVersionsLength;
            //CxPlatCopyMemory(
            //    (public int*)Destination->OfferedVersions,
            //    Source->OfferedVersions,
            //    Destination->OfferedVersionsLength * sizeof(public int));

            //Destination->FullyDeployedVersions =
            //    Destination->OfferedVersions + Destination->OfferedVersionsLength;
            //Destination->FullyDeployedVersionsLength = Source->FullyDeployedVersionsLength;
            //CxPlatCopyMemory(
            //    (public int*)Destination->FullyDeployedVersions,
            //    Source->FullyDeployedVersions,
            //    Destination->FullyDeployedVersionsLength * sizeof(public int));

            //if (CopyExternalToInternal) {
            //    //
            //    // This assumes the external is always in little-endian format
            //    //
            //    for (public int i = 0; i < Destination->AcceptableVersionsLength; ++i) {
            //        ((public int*)Destination->AcceptableVersions)[i] = CxPlatByteSwapUint32(Destination->AcceptableVersions[i]);
            //    }
            //    for (public int i = 0; i < Destination->OfferedVersionsLength; ++i)
            //    {
            //        ((public int*)Destination->OfferedVersions)[i] = CxPlatByteSwapUint32(Destination->OfferedVersions[i]);
            //    }
            //    for (public int i = 0; i < Destination->FullyDeployedVersionsLength; ++i)
            //    {
            //        ((public int*)Destination->FullyDeployedVersions)[i] = CxPlatByteSwapUint32(Destination->FullyDeployedVersions[i]);
            //    }
            //}

            return Destination;
        }

        static void QuicSettingsCleanup(QUIC_SETTINGS_INTERNAL Settings)
        {
            if (Settings.VersionSettings != null)
            {
                Settings.VersionSettings = null;
                Settings.IsSet.VersionSettings = false;
            }
        }

    }

}
