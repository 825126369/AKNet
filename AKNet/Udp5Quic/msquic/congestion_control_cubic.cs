using System;

namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_CUBIC_HYSTART_STATE
    {
        HYSTART_NOT_STARTED = 0,
        HYSTART_ACTIVE = 1,
        HYSTART_DONE = 2
    }

    internal class QUIC_CONGESTION_CONTROL_CUBIC
    {
        public bool HasHadCongestionEvent;
        public bool IsInRecovery;
        public bool IsInPersistentCongestion;
        public bool TimeOfLastAckValid;
        public uint InitialWindowPackets;
        public uint SendIdleTimeoutMs;

        public uint CongestionWindow; // bytes
        public uint PrevCongestionWindow; // bytes
        public uint SlowStartThreshold; // bytes
        public uint PrevSlowStartThreshold; // bytes
        public uint AimdWindow; // bytes
        public uint PrevAimdWindow; // bytes
        public uint AimdAccumulator; // bytes

        public uint BytesInFlight;
        public uint BytesInFlightMax;
        public uint LastSendAllowance; // bytes

        public byte Exemptions;

        public ulong TimeOfLastAck; // microseconds
        public ulong TimeOfCongAvoidStart; // microseconds
        public uint KCubic; // millisec
        public uint PrevKCubic; // millisec
        public uint WindowPrior; // bytes (prior_cwnd from rfc8312bis)
        public uint PrevWindowPrior; // bytes
        public uint WindowMax; // bytes (W_max from rfc8312bis)
        public uint PrevWindowMax; // bytes
        public uint WindowLastMax; // bytes (W_last_max from rfc8312bis)
        public uint PrevWindowLastMax; // bytes

        public QUIC_CUBIC_HYSTART_STATE HyStartState;
        public uint HyStartAckCount;
        public ulong MinRttInLastRound; // microseconds
        public ulong MinRttInCurrentRound; // microseconds
        public ulong CssBaselineMinRtt; // microseconds
        public ulong HyStartRoundEnd; // Packet Number
        public uint CWndSlowStartGrowthDivisor;
        public uint ConservativeSlowStartRounds;
        public ulong RecoverySentPacketNumber;
    }

    internal static partial class MSQuicFunc
    {
        static readonly QUIC_CONGESTION_CONTROL QuicCongestionControlCubic = new QUIC_CONGESTION_CONTROL
        {
            Name = "Cubic",
            QuicCongestionControlCanSend = CubicCongestionControlCanSend,
            QuicCongestionControlSetExemption = CubicCongestionControlSetExemption,
            QuicCongestionControlReset = CubicCongestionControlReset,
            QuicCongestionControlGetSendAllowance = CubicCongestionControlGetSendAllowance,
            QuicCongestionControlOnDataSent = CubicCongestionControlOnDataSent,
            QuicCongestionControlOnDataInvalidated = CubicCongestionControlOnDataInvalidated,
            QuicCongestionControlOnDataAcknowledged = CubicCongestionControlOnDataAcknowledged,
            QuicCongestionControlOnDataLost = CubicCongestionControlOnDataLost,
            QuicCongestionControlOnEcn = CubicCongestionControlOnEcn,
            QuicCongestionControlOnSpuriousCongestionEvent = CubicCongestionControlOnSpuriousCongestionEvent,
            QuicCongestionControlLogOutFlowStatus = CubicCongestionControlLogOutFlowStatus,
            QuicCongestionControlGetExemptions = CubicCongestionControlGetExemptions,
            QuicCongestionControlGetBytesInFlightMax = CubicCongestionControlGetBytesInFlightMax,
            QuicCongestionControlIsAppLimited = CubicCongestionControlIsAppLimited,
            QuicCongestionControlSetAppLimited = CubicCongestionControlSetAppLimited,
            QuicCongestionControlGetCongestionWindow = CubicCongestionControlGetCongestionWindow,
        };

        static void CubicCongestionControlInitialize(QUIC_CONGESTION_CONTROL Cc, QUIC_SETTINGS_INTERNAL Settings)
        {
            Cc = QuicCongestionControlCubic;
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;

            QUIC_CONNECTION Connection = Cc.mConnection;
            ushort DatagramPayloadLength = QuicPathGetDatagramPayloadSize(Connection.Paths[0]);

            Cubic.SlowStartThreshold = uint.MaxValue;
            Cubic.SendIdleTimeoutMs = Settings.SendIdleTimeoutMs;
            Cubic.InitialWindowPackets = Settings.InitialWindowPackets;
            Cubic.CongestionWindow = DatagramPayloadLength * Cubic.InitialWindowPackets;
            Cubic.BytesInFlightMax = Cubic.CongestionWindow / 2;
            Cubic.MinRttInCurrentRound = ulong.MaxValue;
            Cubic.HyStartRoundEnd = Connection.Send.NextPacketNumber;
            Cubic.HyStartState = QUIC_CUBIC_HYSTART_STATE.HYSTART_NOT_STARTED;
            Cubic.CWndSlowStartGrowthDivisor = 1;
            CubicCongestionHyStartResetPerRttRound(Cubic);

            QuicConnLogOutFlowStats(Connection);
            QuicConnLogCubic(Connection);
        }

        static void CubicCongestionHyStartResetPerRttRound(QUIC_CONGESTION_CONTROL_CUBIC Cubic)
        {
            Cubic.HyStartAckCount = 0;
            Cubic.MinRttInLastRound = Cubic.MinRttInCurrentRound;
            Cubic.MinRttInCurrentRound = long.MaxValue;
        }
    }
}
