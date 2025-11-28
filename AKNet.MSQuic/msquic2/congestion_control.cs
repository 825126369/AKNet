/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:50
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace MSQuic2
{
    internal enum QUIC_CONGESTION_CONTROL_ALGORITHM
    {
        QUIC_CONGESTION_CONTROL_ALGORITHM_CUBIC,
        QUIC_CONGESTION_CONTROL_ALGORITHM_BBR,
        QUIC_CONGESTION_CONTROL_ALGORITHM_MAX,
    }

    internal struct QUIC_ECN_EVENT
    {
        public ulong LargestPacketNumberAcked;
        public ulong LargestSentPacketNumber;
    }

    internal struct QUIC_LOSS_EVENT
    {
        public ulong LargestPacketNumberLost;
        public ulong LargestSentPacketNumber;
        public long NumRetransmittableBytes;
        public bool PersistentCongestion;
    }

    internal struct QUIC_ACK_EVENT
    {
        public long TimeNow;
        public ulong LargestAck;
        public ulong LargestSentPacketNumber;
        public long NumTotalAckedRetransmittableBytes;
        public int NumRetransmittableBytes;
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
        public Action<QUIC_CONGESTION_CONTROL, int> QuicCongestionControlOnDataSent;
        public Func<QUIC_CONGESTION_CONTROL, int, bool> QuicCongestionControlOnDataInvalidated;
        public Func<QUIC_CONGESTION_CONTROL, QUIC_ACK_EVENT, bool> QuicCongestionControlOnDataAcknowledged;
        public Action<QUIC_CONGESTION_CONTROL, QUIC_LOSS_EVENT> QuicCongestionControlOnDataLost;
        public Action<QUIC_CONGESTION_CONTROL, QUIC_ECN_EVENT> QuicCongestionControlOnEcn;
        public Func<QUIC_CONGESTION_CONTROL, bool> QuicCongestionControlOnSpuriousCongestionEvent;
        public Action<QUIC_CONGESTION_CONTROL> QuicCongestionControlLogOutFlowStatus;
        public Func<QUIC_CONGESTION_CONTROL, byte> QuicCongestionControlGetExemptions;
        public Func<QUIC_CONGESTION_CONTROL, int> QuicCongestionControlGetBytesInFlightMax;
        public Func<QUIC_CONGESTION_CONTROL, uint> QuicCongestionControlGetCongestionWindow;
        public Func<QUIC_CONGESTION_CONTROL, bool> QuicCongestionControlIsAppLimited;
        public Action<QUIC_CONGESTION_CONTROL> QuicCongestionControlSetAppLimited;

        public readonly QUIC_CONGESTION_CONTROL_CUBIC Cubic = new QUIC_CONGESTION_CONTROL_CUBIC();
        //public QUIC_CONGESTION_CONTROL_BBR Bbr;
        public QUIC_CONNECTION mConnection;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicCongestionControlInitialize(out QUIC_CONGESTION_CONTROL Cc, QUIC_CONNECTION Connection)
        {
            Cc = null;
            switch (Connection.Settings.CongestionControlAlgorithm)
            {
                default:
                case QUIC_CONGESTION_CONTROL_ALGORITHM.QUIC_CONGESTION_CONTROL_ALGORITHM_CUBIC:
                    CubicCongestionControlInitialize(out Cc, Connection);
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

        static uint QuicCongestionControlGetSendAllowance(QUIC_CONGESTION_CONTROL Cc, long TimeSinceLastSend, bool TimeSinceLastSendValid)
        {
            return Cc.QuicCongestionControlGetSendAllowance(Cc, TimeSinceLastSend, TimeSinceLastSendValid);
        }

        static byte QuicCongestionControlGetExemptions(QUIC_CONGESTION_CONTROL Cc)
        {
            return Cc.QuicCongestionControlGetExemptions(Cc);
        }

        static void QuicCongestionControlOnDataLost(QUIC_CONGESTION_CONTROL Cc, QUIC_LOSS_EVENT LossEvent)
        {
            Cc.QuicCongestionControlOnDataLost(Cc, LossEvent);
        }

        static bool QuicCongestionControlOnDataAcknowledged(QUIC_CONGESTION_CONTROL Cc, QUIC_ACK_EVENT AckEvent)
        {
            return Cc.QuicCongestionControlOnDataAcknowledged(Cc, AckEvent);
        }

        static bool QuicCongestionControlOnDataInvalidated(QUIC_CONGESTION_CONTROL Cc, int NumRetransmittableBytes)
        {
            return Cc.QuicCongestionControlOnDataInvalidated(Cc, NumRetransmittableBytes);
        }

        static void QuicCongestionControlReset(QUIC_CONGESTION_CONTROL Cc, bool FullReset)
        {
            Cc.QuicCongestionControlReset(Cc, FullReset);
        }

        static bool QuicCongestionControlOnSpuriousCongestionEvent(QUIC_CONGESTION_CONTROL Cc)
        {
            return Cc.QuicCongestionControlOnSpuriousCongestionEvent(Cc);
        }

        static int QuicCongestionControlGetBytesInFlightMax(QUIC_CONGESTION_CONTROL Cc)
        {
            return Cc.QuicCongestionControlGetBytesInFlightMax(Cc);
        }

        static void QuicCongestionControlOnEcn(QUIC_CONGESTION_CONTROL Cc, QUIC_ECN_EVENT EcnEvent)
        {
            if (Cc.QuicCongestionControlOnEcn != null)
            {
                Cc.QuicCongestionControlOnEcn(Cc, EcnEvent);
            }
        }

        static void QuicCongestionControlSetAppLimited(QUIC_CONGESTION_CONTROL Cc)
        {
            Cc.QuicCongestionControlSetAppLimited(Cc);
        }

        static bool QuicCongestionControlIsAppLimited(QUIC_CONGESTION_CONTROL Cc)
        {
            return Cc.QuicCongestionControlIsAppLimited(Cc);
        }

        static void QuicCongestionControlOnDataSent(QUIC_CONGESTION_CONTROL Cc, int NumRetransmittableBytes)
        {
            Cc.QuicCongestionControlOnDataSent(Cc, NumRetransmittableBytes);
        }

        static void QuicCongestionControlSetExemption( QUIC_CONGESTION_CONTROL Cc, int NumPackets)
        {
            Cc.QuicCongestionControlSetExemption(Cc, (byte)NumPackets);
        }
    }

}
