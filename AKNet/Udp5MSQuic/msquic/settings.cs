using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_VERSION_SETTINGS
    {
        public uint[] AcceptableVersions = null;
        public uint[] OfferedVersions;
        public readonly List<uint> FullyDeployedVersions = new List<uint>();
        public int AcceptableVersionsLength;
        public int OfferedVersionsLength;

        public static implicit operator QUIC_VERSION_SETTINGS(ReadOnlySpan<byte> ssBuffer)
        {
            QUIC_VERSION_SETTINGS mm = new QUIC_VERSION_SETTINGS();
            mm.WriteFrom(ssBuffer);
            return mm;
        }
        public void WriteTo(Span<byte> Buffer)
        {

        }
        public void WriteFrom(ReadOnlySpan<byte> Buffer)
        {

        }
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

    //[StructLayout(LayoutKind.Explicit)]
    internal class QUIC_SETTINGS
    {
        public static implicit operator QUIC_SETTINGS(ReadOnlySpan<byte> ssBuffer)
        {
            QUIC_SETTINGS mm = new QUIC_SETTINGS();
            mm.WriteFrom(ssBuffer);
            return mm;
        }

        public void WriteTo(Span<byte> Buffer)
        {

        }

        public void WriteFrom(ReadOnlySpan<byte> Buffer)
        {

        }

        public ulong IsSetFlags;
        public QUIC_VERSION_SETTINGS VersionSettings;
        public long MaxBytesPerKey;
        public long HandshakeIdleTimeoutMs;
        public long IdleTimeoutMs; //如果在指定的毫秒数内没有收到任何来自对端的数据包（包括加密流量和 keep-alive），连接将被自动关闭。
        public long MtuDiscoverySearchCompleteTimeoutUs;
        public int TlsClientMaxSendBuffer;
        public int TlsServerMaxSendBuffer;
        public int StreamRecvWindowDefault;
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

        static void QuicSettingsSettingsToInternal(QUIC_SETTINGS Settings, QUIC_SETTINGS InternalSettings)
        {
            QuicSettingsCopy2(InternalSettings, Settings);
        }

        static void QuicSettingsCopy2(QUIC_SETTINGS Destination, QUIC_SETTINGS Source)
        {
            Destination.IsSetFlags = Source.IsSetFlags;
            Destination.MaxBytesPerKey = Source.MaxBytesPerKey;
            Destination.HandshakeIdleTimeoutMs = Source.HandshakeIdleTimeoutMs;
            Destination.IdleTimeoutMs = Source.IdleTimeoutMs;
            Destination.MtuDiscoverySearchCompleteTimeoutUs = Source.MtuDiscoverySearchCompleteTimeoutUs;
            Destination.TlsClientMaxSendBuffer = Source.TlsClientMaxSendBuffer;
            Destination.TlsServerMaxSendBuffer = Source.TlsServerMaxSendBuffer;
            Destination.StreamRecvWindowDefault = Source.StreamRecvWindowDefault;
            Destination.ConnFlowControlWindow = Source.ConnFlowControlWindow;
            Destination.MaxWorkerQueueDelayUs = Source.MaxWorkerQueueDelayUs;
            Destination.MaxStatelessOperations = Source.MaxStatelessOperations;
            Destination.InitialWindowPackets = Source.InitialWindowPackets;
            Destination.SendIdleTimeoutMs = Source.SendIdleTimeoutMs;
            Destination.InitialRttMs = Source.InitialRttMs;
            Destination.MaxAckDelayMs = Source.MaxAckDelayMs;
            Destination.DisconnectTimeoutMs = Source.DisconnectTimeoutMs;
            Destination.KeepAliveIntervalMs = Source.KeepAliveIntervalMs;
            Destination.CongestionControlAlgorithm = Source.CongestionControlAlgorithm;
            Destination.PeerBidiStreamCount = Source.PeerBidiStreamCount;
            Destination.PeerUnidiStreamCount = Source.PeerUnidiStreamCount;
            Destination.MaxBindingStatelessOperations = Source.MaxBindingStatelessOperations;
            Destination.StatelessOperationExpirationMs = Source.StatelessOperationExpirationMs;
            Destination.MinimumMtu = Source.MinimumMtu;
            Destination.MaximumMtu = Source.MaximumMtu;
            Destination.MaxOperationsPerDrain = Source.MaxOperationsPerDrain;
            Destination.MtuDiscoveryMissingProbeCount = Source.MtuDiscoveryMissingProbeCount;
            Destination.SendBufferingEnabled = Source.SendBufferingEnabled;
            Destination.PacingEnabled = Source.PacingEnabled;
            Destination.MigrationEnabled = Source.MigrationEnabled;
            Destination.DatagramReceiveEnabled = Source.DatagramReceiveEnabled;
            Destination.ServerResumptionLevel = Source.ServerResumptionLevel;
            Destination.GreaseQuicBitEnabled = Source.GreaseQuicBitEnabled;
            Destination.EcnEnabled = Source.EcnEnabled;
            Destination.DestCidUpdateIdleTimeoutMs = Source.DestCidUpdateIdleTimeoutMs;
            Destination.HyStartEnabled = Source.HyStartEnabled;
            Destination.EncryptionOffloadAllowed = Source.EncryptionOffloadAllowed;
            Destination.ReliableResetEnabled = Source.ReliableResetEnabled;
            Destination.OneWayDelayEnabled = Source.OneWayDelayEnabled;
            Destination.StreamRecvWindowBidiLocalDefault = Source.StreamRecvWindowBidiLocalDefault;
            Destination.StreamRecvWindowBidiRemoteDefault = Source.StreamRecvWindowBidiRemoteDefault;
            Destination.StreamRecvWindowUnidiDefault = Source.StreamRecvWindowUnidiDefault;
            Destination.NetStatsEventEnabled = Source.NetStatsEventEnabled;
            Destination.StreamMultiReceiveEnabled = Source.StreamMultiReceiveEnabled;
            Destination.StreamRecvWindowBidiLocalDefault = Source.StreamRecvWindowBidiLocalDefault;
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
                SetFlag(ref Settings.IsSetFlags, E_SETTING_FLAG_VersionSettings, false);
            }
        }

        static ulong QuicSettingsVersionSettingsToInternal(QUIC_VERSION_SETTINGS Settings, QUIC_SETTINGS InternalSettings)
        {
            if (Settings == null)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            InternalSettings.IsSetFlags = 0;
            for (int i = 0; i < Settings.AcceptableVersionsLength; ++i)
            {
                if (!QuicIsVersionSupported(Settings.AcceptableVersions[i]) &&
                    !QuicIsVersionReserved(Settings.AcceptableVersions[i]))
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }
            }

            for (int i = 0; i < Settings.OfferedVersionsLength; ++i)
            {
                if (!QuicIsVersionSupported(Settings.OfferedVersions[i]) &&
                    !QuicIsVersionReserved(Settings.OfferedVersions[i]))
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }
            }

            for (int i = 0; i < Settings.FullyDeployedVersions.Count; ++i)
            {
                if (!QuicIsVersionSupported(Settings.FullyDeployedVersions[i]) &&
                    !QuicIsVersionReserved(Settings.FullyDeployedVersions[i]))
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }
            }

            if (Settings.AcceptableVersionsLength == 0 &&
                Settings.FullyDeployedVersions.Count == 0 &&
                Settings.OfferedVersionsLength == 0)
            {
                SetFlag(ref InternalSettings.IsSetFlags, E_SETTING_FLAG_VersionNegotiationExtEnabled, true);
                SetFlag(ref InternalSettings.IsSetFlags, E_SETTING_FLAG_VersionSettings, true);
                InternalSettings.VersionNegotiationExtEnabled = true;
                InternalSettings.VersionSettings = null;
            }
            else
            {
                SetFlag(ref InternalSettings.IsSetFlags, E_SETTING_FLAG_VersionNegotiationExtEnabled, true);
                InternalSettings.VersionNegotiationExtEnabled = true;
                InternalSettings.VersionSettings = QuicSettingsCopyVersionSettings(Settings, true);
                if (InternalSettings.VersionSettings == null)
                {
                    return QUIC_STATUS_OUT_OF_MEMORY;
                }
                SetFlag(ref InternalSettings.IsSetFlags, E_SETTING_FLAG_VersionSettings, true);
            }
            return QUIC_STATUS_SUCCESS;
        }

        static bool QuicSettingApply(QUIC_SETTINGS Destination, bool OverWrite, bool AllowMtuAndEcnChanges, QUIC_SETTINGS Source)
        {
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_SendBufferingEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_SendBufferingEnabled) || OverWrite))
            {
                Destination.SendBufferingEnabled = Source.SendBufferingEnabled;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_SendBufferingEnabled, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_PacingEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_PacingEnabled) || OverWrite))
            {
                Destination.PacingEnabled = Source.PacingEnabled;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_PacingEnabled, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MigrationEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MigrationEnabled) || OverWrite))
            {
                Destination.MigrationEnabled = Source.MigrationEnabled;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MigrationEnabled, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_DatagramReceiveEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_DatagramReceiveEnabled) || OverWrite))
            {
                Destination.DatagramReceiveEnabled = Source.DatagramReceiveEnabled;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_DatagramReceiveEnabled, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MaxOperationsPerDrain) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxOperationsPerDrain) || OverWrite))
            {
                if (Source.MaxOperationsPerDrain == 0)
                {
                    return false;
                }
                Destination.MaxOperationsPerDrain = Source.MaxOperationsPerDrain;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MaxOperationsPerDrain, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_RetryMemoryLimit) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_RetryMemoryLimit) || OverWrite))
            {
                Destination.RetryMemoryLimit = Source.RetryMemoryLimit;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_RetryMemoryLimit, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_LoadBalancingMode) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_LoadBalancingMode) || OverWrite))
            {
                if (Source.LoadBalancingMode >= QUIC_LOAD_BALANCING_MODE.QUIC_LOAD_BALANCING_COUNT)
                {
                    return false;
                }
                Destination.LoadBalancingMode = Source.LoadBalancingMode;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_LoadBalancingMode, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_FixedServerID) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_FixedServerID) || OverWrite))
            {
                Destination.FixedServerID = Source.FixedServerID;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_FixedServerID, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MaxWorkerQueueDelayUs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxWorkerQueueDelayUs) || OverWrite))
            {
                Destination.MaxWorkerQueueDelayUs = Source.MaxWorkerQueueDelayUs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MaxWorkerQueueDelayUs, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MaxStatelessOperations) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxStatelessOperations) || OverWrite))
            {
                Destination.MaxStatelessOperations = Source.MaxStatelessOperations;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MaxStatelessOperations, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_InitialWindowPackets) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_InitialWindowPackets) || OverWrite))
            {
                Destination.InitialWindowPackets = Source.InitialWindowPackets;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_InitialWindowPackets, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_SendIdleTimeoutMs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_SendIdleTimeoutMs) || OverWrite))
            {
                Destination.SendIdleTimeoutMs = Source.SendIdleTimeoutMs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_SendIdleTimeoutMs, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_InitialRttMs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_InitialRttMs) || OverWrite))
            {
                if (Source.InitialRttMs == 0)
                {
                    return false;
                }
                Destination.InitialRttMs = Source.InitialRttMs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_InitialRttMs, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MaxAckDelayMs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxAckDelayMs) || OverWrite))
            {
                if (Source.MaxAckDelayMs > QUIC_TP_MAX_ACK_DELAY_MAX)
                {
                    return false;
                }
                Destination.MaxAckDelayMs = Source.MaxAckDelayMs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MaxAckDelayMs, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_DisconnectTimeoutMs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_DisconnectTimeoutMs) || OverWrite))
            {
                if (Source.DisconnectTimeoutMs == 0 || Source.DisconnectTimeoutMs > QUIC_MAX_DISCONNECT_TIMEOUT)
                {
                    return false;
                }
                Destination.DisconnectTimeoutMs = Source.DisconnectTimeoutMs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_DisconnectTimeoutMs, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_KeepAliveIntervalMs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_KeepAliveIntervalMs) || OverWrite))
            {
                Destination.KeepAliveIntervalMs = Source.KeepAliveIntervalMs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_KeepAliveIntervalMs, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_IdleTimeoutMs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_IdleTimeoutMs) || OverWrite))
            {
                if (Source.IdleTimeoutMs > (long)QUIC_VAR_INT_MAX)
                {
                    return false;
                }
                Destination.IdleTimeoutMs = Source.IdleTimeoutMs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_IdleTimeoutMs, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_HandshakeIdleTimeoutMs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_HandshakeIdleTimeoutMs) || OverWrite))
            {
                if (Source.HandshakeIdleTimeoutMs > (long)QUIC_VAR_INT_MAX)
                {
                    return false;
                }
                Destination.HandshakeIdleTimeoutMs = Source.HandshakeIdleTimeoutMs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_HandshakeIdleTimeoutMs, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_PeerBidiStreamCount) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_PeerBidiStreamCount) || OverWrite))
            {
                Destination.PeerBidiStreamCount = Source.PeerBidiStreamCount;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_PeerBidiStreamCount, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_PeerUnidiStreamCount) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_PeerUnidiStreamCount) || OverWrite))
            {
                Destination.PeerUnidiStreamCount = Source.PeerUnidiStreamCount;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_PeerUnidiStreamCount, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_TlsClientMaxSendBuffer) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_TlsClientMaxSendBuffer) || OverWrite))
            {
                Destination.TlsClientMaxSendBuffer = Source.TlsClientMaxSendBuffer;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_TlsClientMaxSendBuffer, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_TlsClientMaxSendBuffer) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_TlsClientMaxSendBuffer) || OverWrite))
            {
                Destination.TlsClientMaxSendBuffer = Source.TlsClientMaxSendBuffer;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_TlsClientMaxSendBuffer, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowDefault) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowDefault) || OverWrite))
            {
                if (Source.StreamRecvWindowDefault == 0 || (Source.StreamRecvWindowDefault & (Source.StreamRecvWindowDefault - 1)) != 0)
                {
                    return false; // Must be power of 2
                }
                Destination.StreamRecvWindowDefault = Source.StreamRecvWindowDefault;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowDefault, true);

                if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiLocalDefault) || OverWrite)
                {
                    Destination.StreamRecvWindowBidiLocalDefault = Source.StreamRecvWindowDefault;
                }
                if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiRemoteDefault) || OverWrite)
                {
                    Destination.StreamRecvWindowBidiRemoteDefault = Source.StreamRecvWindowDefault;
                }
                if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowUnidiDefault) || OverWrite)
                {
                    Destination.StreamRecvWindowUnidiDefault = Source.StreamRecvWindowDefault;
                }
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiLocalDefault) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiLocalDefault) || OverWrite))
            {
                if (Source.StreamRecvWindowBidiLocalDefault == 0 || (Source.StreamRecvWindowBidiLocalDefault & (Source.StreamRecvWindowBidiLocalDefault - 1)) != 0)
                {
                    return false; // Must be power of 2
                }
                Destination.StreamRecvWindowBidiLocalDefault = Source.StreamRecvWindowBidiLocalDefault;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiLocalDefault, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiRemoteDefault) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiRemoteDefault) || OverWrite))
            {
                if (Source.StreamRecvWindowBidiRemoteDefault == 0 || (Source.StreamRecvWindowBidiRemoteDefault & (Source.StreamRecvWindowBidiRemoteDefault - 1)) != 0)
                {
                    return false; // Must be power of 2
                }
                Destination.StreamRecvWindowBidiRemoteDefault = Source.StreamRecvWindowBidiRemoteDefault;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowBidiRemoteDefault, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowUnidiDefault) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowUnidiDefault) || OverWrite))
            {
                if (Source.StreamRecvWindowUnidiDefault == 0 || (Source.StreamRecvWindowUnidiDefault & (Source.StreamRecvWindowUnidiDefault - 1)) != 0)
                {
                    return false; // Must be power of 2
                }
                Destination.StreamRecvWindowUnidiDefault = Source.StreamRecvWindowUnidiDefault;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvWindowUnidiDefault, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_StreamRecvBufferDefault) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvBufferDefault) || OverWrite))
            {
                if (Source.StreamRecvBufferDefault < QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE)
                {
                    return false;
                }
                Destination.StreamRecvBufferDefault = Source.StreamRecvBufferDefault;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_StreamRecvBufferDefault, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_ConnFlowControlWindow) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_ConnFlowControlWindow) || OverWrite))
            {
                Destination.ConnFlowControlWindow = Source.ConnFlowControlWindow;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_ConnFlowControlWindow, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MaxBytesPerKey) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxBytesPerKey) || OverWrite))
            {
                if (Source.MaxBytesPerKey > QUIC_DEFAULT_MAX_BYTES_PER_KEY)
                {
                    return false;
                }
                Destination.MaxBytesPerKey = Source.MaxBytesPerKey;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MaxBytesPerKey, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_ServerResumptionLevel) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_ServerResumptionLevel) || OverWrite))
            {
                if (Source.ServerResumptionLevel > QUIC_SERVER_RESUMPTION_LEVEL.QUIC_SERVER_RESUME_AND_ZERORTT)
                {
                    return false;
                }
                Destination.ServerResumptionLevel = Source.ServerResumptionLevel;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_ServerResumptionLevel, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_VersionNegotiationExtEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_VersionNegotiationExtEnabled) || OverWrite))
            {
                Destination.VersionNegotiationExtEnabled = Source.VersionNegotiationExtEnabled;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_VersionNegotiationExtEnabled, true);
            }

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_VersionSettings))
            {
                if ((HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_VersionSettings) && OverWrite) ||
                    (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_VersionSettings) && Destination.VersionSettings != null))
                {
                    Destination.VersionSettings = null;
                    SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_VersionSettings, false);
                }

                if (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_VersionSettings) && Source.VersionSettings != null)
                {
                    Destination.VersionSettings = QuicSettingsCopyVersionSettings(Source.VersionSettings, false);
                    if (Destination.VersionSettings == null)
                    {
                        return false;
                    }
                    SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_VersionSettings, true);
                }
            }

            if (AllowMtuAndEcnChanges)
            {
                int MinimumMtu = HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MinimumMtu) ? Destination.MinimumMtu : QUIC_DPLPMTUD_MIN_MTU;
                int MaximumMtu = HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaximumMtu) ? Destination.MaximumMtu : CXPLAT_MAX_MTU;
                if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MinimumMtu) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MinimumMtu) || OverWrite))
                {
                    MinimumMtu = Source.MinimumMtu;
                    if (MinimumMtu < QUIC_DPLPMTUD_MIN_MTU)
                    {
                        MinimumMtu = QUIC_DPLPMTUD_MIN_MTU;
                    }
                    else if (MinimumMtu > CXPLAT_MAX_MTU)
                    {
                        MinimumMtu = CXPLAT_MAX_MTU;
                    }
                }
                if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MaximumMtu) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaximumMtu) || OverWrite))
                {
                    MaximumMtu = Source.MaximumMtu;
                    if (MaximumMtu < QUIC_DPLPMTUD_MIN_MTU)
                    {
                        MaximumMtu = QUIC_DPLPMTUD_MIN_MTU;
                    }
                    else if (MaximumMtu > CXPLAT_MAX_MTU)
                    {
                        MaximumMtu = CXPLAT_MAX_MTU;
                    }
                }
                if (MinimumMtu > MaximumMtu)
                {
                    return false;
                }
                if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MinimumMtu))
                {
                    SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MinimumMtu, true);
                }
                if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MaximumMtu))
                {
                    SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MaximumMtu, true);
                }
                Destination.MinimumMtu = (ushort)MinimumMtu;
                Destination.MaximumMtu = (ushort)MaximumMtu;
            }
            else if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MinimumMtu) || HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MaximumMtu))
            {
                return false;
            }

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MtuDiscoverySearchCompleteTimeoutUs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MtuDiscoverySearchCompleteTimeoutUs) || OverWrite))
            {
                Destination.MtuDiscoverySearchCompleteTimeoutUs = Source.MtuDiscoverySearchCompleteTimeoutUs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MtuDiscoverySearchCompleteTimeoutUs, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MtuDiscoveryMissingProbeCount) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MtuDiscoveryMissingProbeCount) || OverWrite))
            {
                Destination.MtuDiscoveryMissingProbeCount = Source.MtuDiscoveryMissingProbeCount;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MtuDiscoveryMissingProbeCount, true);
            }

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_MaxBindingStatelessOperations) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_MaxBindingStatelessOperations) || OverWrite))
            {
                Destination.MaxBindingStatelessOperations = Source.MaxBindingStatelessOperations;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_MaxBindingStatelessOperations, true);
            }
            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_StatelessOperationExpirationMs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StatelessOperationExpirationMs) || OverWrite))
            {
                Destination.StatelessOperationExpirationMs = Source.StatelessOperationExpirationMs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_StatelessOperationExpirationMs, true);
            }

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_CongestionControlAlgorithm) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_CongestionControlAlgorithm) || OverWrite))
            {
                Destination.CongestionControlAlgorithm = Source.CongestionControlAlgorithm;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_CongestionControlAlgorithm, true);
            }

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_DestCidUpdateIdleTimeoutMs) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_DestCidUpdateIdleTimeoutMs) || OverWrite))
            {
                Destination.DestCidUpdateIdleTimeoutMs = Source.DestCidUpdateIdleTimeoutMs;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_DestCidUpdateIdleTimeoutMs, true);
            }

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_GreaseQuicBitEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_GreaseQuicBitEnabled) || OverWrite))
            {
                Destination.GreaseQuicBitEnabled = Source.GreaseQuicBitEnabled;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_GreaseQuicBitEnabled, true);
            }

            if (AllowMtuAndEcnChanges)
            {
                if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_EcnEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_EcnEnabled) || OverWrite))
                {
                    Destination.EcnEnabled = Source.EcnEnabled;
                    SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_EcnEnabled, true);
                }
            }
            else if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_EcnEnabled))
            {
                return false;
            }

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_EncryptionOffloadAllowed) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_EncryptionOffloadAllowed) || OverWrite))
            {
                Destination.EncryptionOffloadAllowed = Source.EncryptionOffloadAllowed;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_EncryptionOffloadAllowed, true);
            }

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_ReliableResetEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_ReliableResetEnabled) || OverWrite))
            {
                Destination.ReliableResetEnabled = Source.ReliableResetEnabled;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_ReliableResetEnabled, true);
            }

            //if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_XdpEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_XdpEnabled) || OverWrite))
            //{
            //    Destination.XdpEnabled = Source.XdpEnabled;
            //    SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_XdpEnabled, true);
            //}

            //if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_QTIPEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_QTIPEnabled) || OverWrite))
            //{
            //    Destination.QTIPEnabled = Source.QTIPEnabled;
            //    SetFlag(ref Destination.IsSetFlags, QTIPEnabled, true);
            //}

            //if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_RioEnabled && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_RioEnabled) || OverWrite)))
            //{
            //    Destination.RioEnabled = Source.RioEnabled;
            //    SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_RioEnabled, true);
            //}

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_OneWayDelayEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_OneWayDelayEnabled) || OverWrite))
            {
                Destination.OneWayDelayEnabled = Source.OneWayDelayEnabled;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_OneWayDelayEnabled, true);
            }

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_NetStatsEventEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_NetStatsEventEnabled) || OverWrite))
            {
                Destination.NetStatsEventEnabled = Source.NetStatsEventEnabled;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_NetStatsEventEnabled, true);
            }

            if (HasFlag(Source.IsSetFlags, E_SETTING_FLAG_StreamMultiReceiveEnabled) && (!HasFlag(Destination.IsSetFlags, E_SETTING_FLAG_StreamMultiReceiveEnabled) || OverWrite))
            {
                Destination.StreamMultiReceiveEnabled = Source.StreamMultiReceiveEnabled;
                SetFlag(ref Destination.IsSetFlags, E_SETTING_FLAG_StreamMultiReceiveEnabled, true);
            }
            return true;
        }

    }

}
