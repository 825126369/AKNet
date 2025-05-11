using System.Collections.Generic;
using System.Runtime;

namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_VERSION_SETTINGS
    {
        public uint[] AcceptableVersions = null;
        public uint[] OfferedVersions;
        public readonly List<uint> FullyDeployedVersions = new List<uint>();
        public int AcceptableVersionsLength;
        public int OfferedVersionsLength;
    }

    internal class QUIC_GLOBAL_SETTINGS
    {
        public ulong IsSetFlags;
        public IsSet_DATA IsSet;
        public ushort RetryMemoryLimit;
        public ushort LoadBalancingMode;
        public uint FixedServerID;

        public class IsSet_DATA
        {
            public bool RetryMemoryLimit;//: 1
            public long LoadBalancingMode;//: 1
            public long FixedServerID;//: 1
            public long RESERVED;//: 61;
        }
    }

    internal class QUIC_SETTINGS
    {
        public QUIC_VERSION_SETTINGS VersionSettings;
        public ulong IsSetFlags;

        public long MaxBytesPerKey;
        public long HandshakeIdleTimeoutMs;
        public long IdleTimeoutMs;
        public long MtuDiscoverySearchCompleteTimeoutUs;
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
        public bool FixedServerID;                 // Global only
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
        public bool EncryptionOffloadAllowed;
        public bool ReliableResetEnabled;
        public bool OneWayDelayEnabled;
        public bool NetStatsEventEnabled;
        public bool StreamMultiReceiveEnabled;
        public byte MtuDiscoveryMissingProbeCount;
    }

    internal static partial class MSQuicFunc
    {
        public static readonly ulong E_GLOBAL_SETTING_FLAG_RetryMemoryLimit = BIT(0);
        public static readonly ulong E_GLOBAL_SETTING_FLAG_LoadBalancingMode = BIT(1);
        public static readonly ulong E_GLOBAL_SETTING_FLAG_FixedServerID = BIT(2);
        public static readonly ulong E_GLOBAL_SETTING_FLAG_RESERVED = BIT(3);
        

        public static readonly ulong E_SETTING_FLAG_MaxBytesPerKey = BIT(0);
        public static readonly ulong E_SETTING_FLAG_HandshakeIdleTimeoutMs = BIT(1);
        public static readonly ulong E_SETTING_FLAG_IdleTimeoutMs = BIT(2);
        public static readonly ulong E_SETTING_FLAG_TlsClientMaxSendBuffer = BIT(3);
        public static readonly ulong E_SETTING_FLAG_TlsServerMaxSendBuffer = BIT(4);
        public static readonly ulong E_SETTING_FLAG_StreamRecvWindowDefault = BIT(5);
        public static readonly ulong E_SETTING_FLAG_StreamRecvWindowBidiLocalDefault = BIT(6);
        public static readonly ulong E_SETTING_FLAG_StreamRecvWindowBidiRemoteDefault = BIT(7);
        public static readonly ulong E_SETTING_FLAG_StreamRecvWindowUnidiDefault = BIT(8);
        public static readonly ulong E_SETTING_FLAG_StreamRecvBufferDefault = BIT(9);
        public static readonly ulong E_SETTING_FLAG_ConnFlowControlWindow = BIT(10);
        public static readonly ulong E_SETTING_FLAG_MaxWorkerQueueDelayUs = BIT(11);
        public static readonly ulong E_SETTING_FLAG_MaxStatelessOperations = BIT(12);
        public static readonly ulong E_SETTING_FLAG_InitialWindowPackets = BIT(13);
        public static readonly ulong E_SETTING_FLAG_SendIdleTimeoutMs = BIT(14);
        public static readonly ulong E_SETTING_FLAG_InitialRttMs = BIT(15);
        public static readonly ulong E_SETTING_FLAG_MaxAckDelayMs = BIT(16);
        public static readonly ulong E_SETTING_FLAG_DisconnectTimeoutMs = BIT(17);
        public static readonly ulong E_SETTING_FLAG_KeepAliveIntervalMs = BIT(18);
        public static readonly ulong E_SETTING_FLAG_PeerBidiStreamCount = BIT(19);
        public static readonly ulong E_SETTING_FLAG_PeerUnidiStreamCount = BIT(20);
        public static readonly ulong E_SETTING_FLAG_RetryMemoryLimit = BIT(21);
        public static readonly ulong E_SETTING_FLAG_LoadBalancingMode = BIT(22);
        public static readonly ulong E_SETTING_FLAG_FixedServerID = BIT(23);
        public static readonly ulong E_SETTING_FLAG_MaxOperationsPerDrain = BIT(24);
        public static readonly ulong E_SETTING_FLAG_SendBufferingEnabled = BIT(25);
        public static readonly ulong E_SETTING_FLAG_PacingEnabled = BIT(26);
        public static readonly ulong E_SETTING_FLAG_MigrationEnabled = BIT(27);
        public static readonly ulong E_SETTING_FLAG_DatagramReceiveEnabled = BIT(28);
        public static readonly ulong E_SETTING_FLAG_ServerResumptionLevel = BIT(29);
        public static readonly ulong E_SETTING_FLAG_VersionSettings = BIT(30);
        public static readonly ulong E_SETTING_FLAG_VersionNegotiationExtEnabled = BIT(31);
        public static readonly ulong E_SETTING_FLAG_MinimumMtu = BIT(32);
        public static readonly ulong E_SETTING_FLAG_MaximumMtu = BIT(33);
        public static readonly ulong E_SETTING_FLAG_MtuDiscoverySearchCompleteTimeoutUs = BIT(34);
        public static readonly ulong E_SETTING_FLAG_MtuDiscoveryMissingProbeCount = BIT(35);
        public static readonly ulong E_SETTING_FLAG_MaxBindingStatelessOperations = BIT(36);
        public static readonly ulong E_SETTING_FLAG_StatelessOperationExpirationMs = BIT(37);
        public static readonly ulong E_SETTING_FLAG_CongestionControlAlgorithm = BIT(38);
        public static readonly ulong E_SETTING_FLAG_DestCidUpdateIdleTimeoutMs = BIT(39);
        public static readonly ulong E_SETTING_FLAG_GreaseQuicBitEnabled = BIT(40);
        public static readonly ulong E_SETTING_FLAG_EcnEnabled = BIT(41);
        public static readonly ulong E_SETTING_FLAG_HyStartEnabled = BIT(42);
        public static readonly ulong E_SETTING_FLAG_EncryptionOffloadAllowed = BIT(43);
        public static readonly ulong E_SETTING_FLAG_ReliableResetEnabled = BIT(44);
        public static readonly ulong E_SETTING_FLAG_OneWayDelayEnabled = BIT(45);
        public static readonly ulong E_SETTING_FLAG_NetStatsEventEnabled = BIT(46);
        public static readonly ulong E_SETTING_FLAG_StreamMultiReceiveEnabled = BIT(47);
        
        static void QuicSettingsSetDefault(QUIC_SETTINGS Settings)
        {
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_SendBufferingEnabled))
            {
                Settings.SendBufferingEnabled = QUIC_DEFAULT_SEND_BUFFERING_ENABLE;
            }
            
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_PacingEnabled))
            {
                Settings.PacingEnabled = QUIC_DEFAULT_SEND_PACING;
            }

            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MigrationEnabled))
            {
                Settings.MigrationEnabled = QUIC_DEFAULT_MIGRATION_ENABLED;
            }

            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_DatagramReceiveEnabled))
            {
                Settings.DatagramReceiveEnabled = QUIC_DEFAULT_DATAGRAM_RECEIVE_ENABLED;
            }

            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MaxOperationsPerDrain))
            {
                Settings.MaxOperationsPerDrain = QUIC_MAX_OPERATIONS_PER_DRAIN;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_RetryMemoryLimit))
            {
                Settings.RetryMemoryLimit = QUIC_DEFAULT_RETRY_MEMORY_FRACTION;
            }

            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_LoadBalancingMode))
            {
                Settings.LoadBalancingMode = QUIC_DEFAULT_LOAD_BALANCING_MODE;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_FixedServerID))
            {
                Settings.FixedServerID = false;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MaxWorkerQueueDelayUs))
            {
                Settings.MaxWorkerQueueDelayUs = QUIC_MAX_WORKER_QUEUE_DELAY * 1000;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MaxStatelessOperations))
            {
                Settings.MaxStatelessOperations = QUIC_MAX_STATELESS_OPERATIONS;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_InitialWindowPackets))
            {
                Settings.InitialWindowPackets = QUIC_INITIAL_WINDOW_PACKETS;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_SendIdleTimeoutMs))
            {
                Settings.SendIdleTimeoutMs = QUIC_DEFAULT_SEND_IDLE_TIMEOUT_MS;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_InitialRttMs))
            {
                Settings.InitialRttMs = QUIC_INITIAL_RTT;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MaxAckDelayMs))
            {
                Settings.MaxAckDelayMs = QUIC_TP_MAX_ACK_DELAY_DEFAULT;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_DisconnectTimeoutMs))
            {
                Settings.DisconnectTimeoutMs = QUIC_DEFAULT_DISCONNECT_TIMEOUT;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_KeepAliveIntervalMs))
            {
                Settings.KeepAliveIntervalMs = QUIC_DEFAULT_KEEP_ALIVE_INTERVAL;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_IdleTimeoutMs))
            {
                Settings.IdleTimeoutMs = QUIC_DEFAULT_IDLE_TIMEOUT;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_HandshakeIdleTimeoutMs))
            {
                Settings.HandshakeIdleTimeoutMs = QUIC_DEFAULT_HANDSHAKE_IDLE_TIMEOUT;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_PeerBidiStreamCount))
            {
                Settings.PeerBidiStreamCount = 0;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_PeerUnidiStreamCount))
            {
                Settings.PeerUnidiStreamCount = 0;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_TlsClientMaxSendBuffer))
            {
                Settings.TlsClientMaxSendBuffer = QUIC_MAX_TLS_CLIENT_SEND_BUFFER;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_TlsClientMaxSendBuffer))
            {
                Settings.TlsClientMaxSendBuffer = QUIC_MAX_TLS_SERVER_SEND_BUFFER;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowDefault))
            {
                Settings.StreamRecvWindowDefault = QUIC_DEFAULT_STREAM_FC_WINDOW_SIZE;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiLocalDefault))
            {
                Settings.StreamRecvWindowBidiLocalDefault = QUIC_DEFAULT_STREAM_FC_WINDOW_SIZE;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiRemoteDefault))
            {
                Settings.StreamRecvWindowBidiRemoteDefault = QUIC_DEFAULT_STREAM_FC_WINDOW_SIZE;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowUnidiDefault))
            {
                Settings.StreamRecvWindowUnidiDefault = QUIC_DEFAULT_STREAM_FC_WINDOW_SIZE;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_StreamRecvBufferDefault))
            {
                Settings.StreamRecvBufferDefault = QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_ConnFlowControlWindow))
            {
                Settings.ConnFlowControlWindow = QUIC_DEFAULT_CONN_FLOW_CONTROL_WINDOW;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MaxBytesPerKey))
            {
                Settings.MaxBytesPerKey = QUIC_DEFAULT_MAX_BYTES_PER_KEY;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_ServerResumptionLevel))
            {
                Settings.ServerResumptionLevel = QUIC_DEFAULT_SERVER_RESUMPTION_LEVEL;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_VersionNegotiationExtEnabled))
            {
                Settings.VersionNegotiationExtEnabled = QUIC_DEFAULT_VERSION_NEGOTIATION_EXT_ENABLED;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MinimumMtu))
            {
                Settings.MinimumMtu = QUIC_DPLPMTUD_DEFAULT_MIN_MTU;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MaximumMtu))
            {
                Settings.MaximumMtu = QUIC_DPLPMTUD_DEFAULT_MAX_MTU;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MtuDiscoveryMissingProbeCount))
            {
                Settings.MtuDiscoveryMissingProbeCount = QUIC_DPLPMTUD_MAX_PROBES;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MtuDiscoverySearchCompleteTimeoutUs))
            {
                Settings.MtuDiscoverySearchCompleteTimeoutUs = QUIC_DPLPMTUD_RAISE_TIMER_TIMEOUT;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_MaxBindingStatelessOperations))
            {
                Settings.MaxBindingStatelessOperations = QUIC_MAX_BINDING_STATELESS_OPERATIONS;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_StatelessOperationExpirationMs))
            {
                Settings.StatelessOperationExpirationMs = QUIC_STATELESS_OPERATION_EXPIRATION_MS;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_CongestionControlAlgorithm))
            {
                Settings.CongestionControlAlgorithm = QUIC_CONGESTION_CONTROL_ALGORITHM_DEFAULT;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_DestCidUpdateIdleTimeoutMs))
            {
                Settings.DestCidUpdateIdleTimeoutMs = QUIC_DEFAULT_DEST_CID_UPDATE_IDLE_TIMEOUT_MS;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_GreaseQuicBitEnabled))
            {
                Settings.GreaseQuicBitEnabled = QUIC_DEFAULT_GREASE_QUIC_BIT_ENABLED;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_EcnEnabled))
            {
                Settings.EcnEnabled = QUIC_DEFAULT_ECN_ENABLED;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_HyStartEnabled))
            {
                Settings.HyStartEnabled = QUIC_DEFAULT_HYSTART_ENABLED;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_EncryptionOffloadAllowed))
            {
                Settings.EncryptionOffloadAllowed = QUIC_DEFAULT_ENCRYPTION_OFFLOAD_ALLOWED;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_ReliableResetEnabled))
            {
                Settings.ReliableResetEnabled = QUIC_DEFAULT_RELIABLE_RESET_ENABLED;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_OneWayDelayEnabled))
            {
                Settings.OneWayDelayEnabled = QUIC_DEFAULT_ONE_WAY_DELAY_ENABLED;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_NetStatsEventEnabled))
            {
                Settings.NetStatsEventEnabled = QUIC_DEFAULT_NET_STATS_EVENT_ENABLED;
            }
            if (!HasFlag(Settings.IsSetFlags, E_SETTING_FLAG_StreamMultiReceiveEnabled))
            {
                Settings.StreamMultiReceiveEnabled = QUIC_DEFAULT_STREAM_MULTI_RECEIVE_ENABLED;
            }
        }

        static ulong QuicSettingsSettingsToInternal(int SettingsSize, QUIC_SETTINGS Settings, QUIC_SETTINGS InternalSettings)
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

        static void QuicSettingsCopy(QUIC_SETTINGS Destination, QUIC_SETTINGS Source)
        {
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_SendBufferingEnabled))
            {
                Destination.SendBufferingEnabled = Source.SendBufferingEnabled;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_PacingEnabled))
            {
                Destination.PacingEnabled = Source.PacingEnabled;
            }

            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MigrationEnabled))
            {
                Destination.MigrationEnabled = Source.MigrationEnabled;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_DatagramReceiveEnabled))
            {
                Destination.DatagramReceiveEnabled = Source.DatagramReceiveEnabled;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxOperationsPerDrain))
            {
                Destination.MaxOperationsPerDrain = Source.MaxOperationsPerDrain;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_RetryMemoryLimit))
            {
                Destination.RetryMemoryLimit = Source.RetryMemoryLimit;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_LoadBalancingMode))
            {
                Destination.LoadBalancingMode = Source.LoadBalancingMode;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_FixedServerID))
            {
                Destination.FixedServerID = Source.FixedServerID;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxWorkerQueueDelayUs))
            {
                Destination.MaxWorkerQueueDelayUs = Source.MaxWorkerQueueDelayUs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxStatelessOperations))
            {
                Destination.MaxStatelessOperations = Source.MaxStatelessOperations;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_InitialWindowPackets))
            {
                Destination.InitialWindowPackets = Source.InitialWindowPackets;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_SendIdleTimeoutMs))
            {
                Destination.SendIdleTimeoutMs = Source.SendIdleTimeoutMs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_InitialRttMs))
            {
                Destination.InitialRttMs = Source.InitialRttMs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxAckDelayMs))
            {
                Destination.MaxAckDelayMs = Source.MaxAckDelayMs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_DisconnectTimeoutMs))
            {
                Destination.DisconnectTimeoutMs = Source.DisconnectTimeoutMs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_KeepAliveIntervalMs))
            {
                Destination.KeepAliveIntervalMs = Source.KeepAliveIntervalMs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_IdleTimeoutMs))
            {
                Destination.IdleTimeoutMs = Source.IdleTimeoutMs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_HandshakeIdleTimeoutMs))
            {
                Destination.HandshakeIdleTimeoutMs = Source.HandshakeIdleTimeoutMs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_PeerBidiStreamCount))
            {
                Destination.PeerBidiStreamCount = Source.PeerBidiStreamCount;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_PeerUnidiStreamCount))
            {
                Destination.PeerUnidiStreamCount = Source.PeerUnidiStreamCount;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_TlsClientMaxSendBuffer))
            {
                Destination.TlsClientMaxSendBuffer = Source.TlsClientMaxSendBuffer;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_TlsClientMaxSendBuffer))
            {
                Destination.TlsClientMaxSendBuffer = Source.TlsClientMaxSendBuffer;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowDefault))
            {
                Destination.StreamRecvWindowDefault = Source.StreamRecvWindowDefault;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiLocalDefault))
            {
                Destination.StreamRecvWindowBidiLocalDefault = Source.StreamRecvWindowBidiLocalDefault;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiRemoteDefault))
            {
                Destination.StreamRecvWindowBidiRemoteDefault = Source.StreamRecvWindowBidiRemoteDefault;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowUnidiDefault))
            {
                Destination.StreamRecvWindowUnidiDefault = Source.StreamRecvWindowUnidiDefault;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvBufferDefault))
            {
                Destination.StreamRecvBufferDefault = Source.StreamRecvBufferDefault;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_ConnFlowControlWindow))
            {
                Destination.ConnFlowControlWindow = Source.ConnFlowControlWindow;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxBytesPerKey))
            {
                Destination.MaxBytesPerKey = Source.MaxBytesPerKey;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_ServerResumptionLevel))
            {
                Destination.ServerResumptionLevel = Source.ServerResumptionLevel;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_VersionNegotiationExtEnabled))
            {
                Destination.VersionNegotiationExtEnabled = Source.VersionNegotiationExtEnabled;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_VersionSettings))
            {
                Destination.VersionSettings = null;
                if (Source.VersionSettings != null)
                {
                    Destination.VersionSettings = QuicSettingsCopyVersionSettings(Source.VersionSettings, false);
                }
            }

            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MinimumMtu) && !HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaximumMtu))
            {
                Destination.MinimumMtu = Source.MinimumMtu;
                Destination.MaximumMtu = Source.MaximumMtu;
            }
            else if (HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MinimumMtu) && !HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaximumMtu))
            {
                if (Source.MaximumMtu > Destination.MinimumMtu)
                {
                    Destination.MaximumMtu = Source.MaximumMtu;
                }
            }
            else if (HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaximumMtu) && !HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MinimumMtu))
            {
                if (Source.MinimumMtu < Destination.MaximumMtu)
                {
                    Destination.MinimumMtu = Source.MinimumMtu;
                }
            }

            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MtuDiscoveryMissingProbeCount))
            {
                Destination.MtuDiscoveryMissingProbeCount = Source.MtuDiscoveryMissingProbeCount;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MtuDiscoverySearchCompleteTimeoutUs))
            {
                Destination.MtuDiscoverySearchCompleteTimeoutUs = Source.MtuDiscoverySearchCompleteTimeoutUs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxBindingStatelessOperations))
            {
                Destination.MaxBindingStatelessOperations = Source.MaxBindingStatelessOperations;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StatelessOperationExpirationMs))
            {
                Destination.StatelessOperationExpirationMs = Source.StatelessOperationExpirationMs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_CongestionControlAlgorithm))
            {
                Destination.CongestionControlAlgorithm = Source.CongestionControlAlgorithm;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_DestCidUpdateIdleTimeoutMs))
            {
                Destination.DestCidUpdateIdleTimeoutMs = Source.DestCidUpdateIdleTimeoutMs;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_GreaseQuicBitEnabled))
            {
                Destination.GreaseQuicBitEnabled = Source.GreaseQuicBitEnabled;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_EcnEnabled))
            {
                Destination.EcnEnabled = Source.EcnEnabled;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_HyStartEnabled))
            {
                Destination.HyStartEnabled = Source.HyStartEnabled;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_EncryptionOffloadAllowed))
            {
                Destination.EncryptionOffloadAllowed = Source.EncryptionOffloadAllowed;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_ReliableResetEnabled))
            {
                Destination.ReliableResetEnabled = Source.ReliableResetEnabled;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_OneWayDelayEnabled))
            {
                Destination.OneWayDelayEnabled = Source.OneWayDelayEnabled;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_NetStatsEventEnabled))
            {
                Destination.NetStatsEventEnabled = Source.NetStatsEventEnabled;
            }
            if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamMultiReceiveEnabled))
            {
                Destination.StreamMultiReceiveEnabled = Source.StreamMultiReceiveEnabled;
            }
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

        static void QuicSettingsCleanup(QUIC_SETTINGS Settings)
        {
            if (Settings.VersionSettings != null)
            {
                Settings.VersionSettings = null;
                SetFlag(Settings.IsSetFlags, E_SETTING_FLAG_VersionSettings, false);
            }
        }

    }

}
