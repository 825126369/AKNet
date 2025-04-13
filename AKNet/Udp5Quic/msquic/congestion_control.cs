using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_CONGESTION_CONTROL_ALGORITHM
    {
        QUIC_CONGESTION_CONTROL_ALGORITHM_CUBIC,
        QUIC_CONGESTION_CONTROL_ALGORITHM_BBR,
        QUIC_CONGESTION_CONTROL_ALGORITHM_MAX,
    }

    internal class QUIC_ECN_EVENT
    {
        public ulong LargestPacketNumberAcked;
        public ulong LargestSentPacketNumber;
    }

    internal class QUIC_LOSS_EVENT
    {
        public long LargestPacketNumberLost;
        public long LargestSentPacketNumber;
        public long NumRetransmittableBytes;
        public bool PersistentCongestion;
    }

    internal class QUIC_ACK_EVENT
    {
        public long TimeNow;
        public ulong LargestAck;
        public long LargestSentPacketNumber;
        public long NumTotalAckedRetransmittableBytes;
        public uint NumRetransmittableBytes;
        public QUIC_SENT_PACKET_METADATA AckedPackets;
        public long SmoothedRtt;
        public long MinRtt;
        public long OneWayDelay;
        public long AdjustedAckTime;
        public bool IsImplicit;
        public bool HasLoss;
        public bool IsLargestAckedPacketAppLimited;
        public bool MinRttValid;
    }

    internal class QUIC_CONGESTION_CONTROL
    {
        public string Name;
        public Func<QUIC_CONGESTION_CONTROL, bool> QuicCongestionControlCanSend;
        public Action<QUIC_CONGESTION_CONTROL, byte> QuicCongestionControlSetExemption;
        public Action<QUIC_CONGESTION_CONTROL, bool> QuicCongestionControlReset;
        public Func<QUIC_CONGESTION_CONTROL, long, bool, uint> QuicCongestionControlGetSendAllowance;
        public Action<QUIC_CONGESTION_CONTROL, uint> QuicCongestionControlOnDataSent;
        public Func<QUIC_CONGESTION_CONTROL, uint, bool> QuicCongestionControlOnDataInvalidated;
        public Func<QUIC_CONGESTION_CONTROL, QUIC_ACK_EVENT, bool> QuicCongestionControlOnDataAcknowledged;
        public Action<QUIC_CONGESTION_CONTROL, QUIC_LOSS_EVENT> QuicCongestionControlOnDataLost;
        public Action<QUIC_CONGESTION_CONTROL, QUIC_ECN_EVENT> QuicCongestionControlOnEcn;
        public Action<QUIC_CONGESTION_CONTROL> QuicCongestionControlOnSpuriousCongestionEvent;
        public Action<QUIC_CONGESTION_CONTROL> QuicCongestionControlLogOutFlowStatus;
        public Func<QUIC_CONGESTION_CONTROL, byte> QuicCongestionControlGetExemptions;
        public Func<QUIC_CONGESTION_CONTROL, uint> QuicCongestionControlGetBytesInFlightMax;
        public Func<QUIC_CONGESTION_CONTROL, uint> QuicCongestionControlGetCongestionWindow;
        public Func<QUIC_CONGESTION_CONTROL, bool> QuicCongestionControlIsAppLimited;
        public Action<QUIC_CONGESTION_CONTROL> QuicCongestionControlSetAppLimited;

        public QUIC_CONGESTION_CONTROL_CUBIC Cubic;
        public QUIC_CONGESTION_CONTROL_BBR Bbr;

        public QUIC_CONNECTION mConnection;

    }

    internal static partial class MSQuicFunc
    {
        static void QuicCongestionControlInitialize(QUIC_CONGESTION_CONTROL Cc, QUIC_SETTINGS_INTERNAL Settings)
        {
            switch (Settings.CongestionControlAlgorithm)
            {
                default:
                case QUIC_CONGESTION_CONTROL_ALGORITHM.QUIC_CONGESTION_CONTROL_ALGORITHM_CUBIC:
                    CubicCongestionControlInitialize(Cc, Settings);
                    break;
                case QUIC_CONGESTION_CONTROL_ALGORITHM.QUIC_CONGESTION_CONTROL_ALGORITHM_BBR:
                    //BbrCongestionControlInitialize(Cc, Settings);
                    break;
            }
        }

        static bool QuicCongestionControlCanSend(QUIC_CONGESTION_CONTROL Cc)
        {
            return Cc.QuicCongestionControlCanSend(Cc);
        }

        static uint QuicCongestionControlGetSendAllowance(QUIC_CONGESTION_CONTROL Cc,long TimeSinceLastSend, bool TimeSinceLastSendValid)
        {
            return Cc.QuicCongestionControlGetSendAllowance(Cc, TimeSinceLastSend, TimeSinceLastSendValid);
        }


    }

}
