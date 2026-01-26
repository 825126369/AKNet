/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Diagnostics;

namespace MSQuic1
{
    internal enum UDP_STATISTIC_TYPE : int
    {
        LOSS_DETECTION_TIME_AVERAGE,
        FirstSendCount,
        ReSendCount,
        SmoothedRtt,
        MinRtt,
        MaxRtt,
        AckDelay,
        TwoPto,
        SendPackets_To_LostPackets,
        LostPackets_DiscardCount,

        QuicLossDetectionDetectAndHandleLostPackets_0000,
        QuicLossDetectionDetectAndHandleLostPackets_1111,
        QuicLossDetectionDetectAndHandleLostPackets_2222,

        QuicLossDetectionScheduleProbe_000,
        QuicLossDetectionScheduleProbe_111,
        QuicLossDetectionScheduleProbe_222,
        QuicLossDetectionScheduleProbe_333,
        //计时器
        QUIC_PERF_COUNTER_TIMER_PACING,
        QUIC_PERF_COUNTER_TIMER_ACK_DELAY,
        QUIC_PERF_COUNTER_TIMER_LOSS_DETECTION,
        QUIC_PERF_COUNTER_TIMER_KEEP_ALIVE,
        QUIC_PERF_COUNTER_TIMER_IDLE,
        QUIC_PERF_COUNTER_TIMER_SHUTDOWN,
            
        MAX, //统计数量
    }

    internal class udp_socket_statistic
    {
        public long[] mibs = new long[byte.MaxValue];

        //统计状态
        public void NET_ADD_STATS(int nSocketIndex)
        {
            mibs[nSocketIndex]++;
        }

        public void PRINT_NET_STATS()
        {
            for (int i = 0; i < mibs.Length; i++)
            {
                if (mibs[i] != 0)
                {
                    NetLog.Log($"接收数据的Socket: {i} : {mibs[i]}");
                }
            }
        }
    }

    internal class quic_partition_udp_statistic
    {
        internal enum MIB_LOG_TYPE
        {
            NONE,
            COUNT,
            AVERAGE,
        }

        internal class udp_statistic_cell
        {
            public MIB_LOG_TYPE nType = MIB_LOG_TYPE.NONE;
            public double nMin = double.MaxValue;
            public double nMax;
            public long nCount;
            public double nValue;

            public void Combine(udp_statistic_cell other)
            {
                if (other == null) return;

                if (nType == MIB_LOG_TYPE.NONE)
                {
                    nType = other.nType;
                }
                else if(nType != other.nType)
                {
                    throw new Exception();
                }

                if (this.nMin > other.nMin)
                {
                    this.nMin = other.nMin;
                }

                if (this.nMax < other.nMax)
                {
                    this.nMax = other.nMax;
                }

                this.nCount += other.nCount;
                this.nValue += other.nValue;
            }
        }

        public udp_statistic_cell[] mibs = new udp_statistic_cell[(int)UDP_STATISTIC_TYPE.MAX];

        //统计状态
        public void NET_ADD_STATS(UDP_STATISTIC_TYPE mMib)
        {
            if (mibs[(int)mMib] == null)
            {
                mibs[(int)mMib] = new udp_statistic_cell();
            }
            udp_statistic_cell mCell = mibs[(int)mMib];

            mCell.nType = MIB_LOG_TYPE.COUNT;
            mCell.nCount++;
        }

        public void NET_ADD_AVERAGE_STATS(UDP_STATISTIC_TYPE mMib, double nValue)
        {
            if (mibs[(int)mMib] == null)
            {
                mibs[(int)mMib] = new udp_statistic_cell();
            }
            udp_statistic_cell mCell = mibs[(int)mMib];

            mCell.nType = MIB_LOG_TYPE.AVERAGE;
            mCell.nCount++;
            mCell.nValue += nValue;
            if (nValue < mCell.nMin)
            {
                mCell.nMin = nValue;
            }

            if (nValue > mCell.nMax)
            {
                mCell.nMax = nValue;
            }
        }

        public void Combine(quic_partition_udp_statistic other)
        {
            for (int i = 0; i < (int)UDP_STATISTIC_TYPE.MAX; i++)
            {
                if (mibs[i] == null)
                {
                    mibs[i] = new udp_statistic_cell();
                }
                mibs[i].Combine(other.mibs[i]);
            }
        }

        private double BaoLiuXiaoShu(double A)
        {
            return Math.Floor(A * 1000000) / 1000000;
        }

        public void PRINT_NET_STATS()
        {
            for (int i = 0; i < (int)UDP_STATISTIC_TYPE.MAX; i++)
            {
                udp_statistic_cell mCell = mibs[i];
                string mibDes = ((UDP_STATISTIC_TYPE)i).ToString();
                if (mCell == null)
                {
                    NetLog.Log($"{mibDes} : null");
                }
                else
                {
                    if (mCell.nType == MIB_LOG_TYPE.AVERAGE)
                    {
                        NetLog.Log($"{mibDes} : {mCell.nCount}: {BaoLiuXiaoShu(mCell.nValue / mCell.nCount)}, {mCell.nMin}, {mCell.nMax}");
                    }
                    else
                    {
                        NetLog.Log($"{mibDes} : {mCell.nCount}");
                    }
                }
            }
            
            if (mibs[(int)UDP_STATISTIC_TYPE.FirstSendCount].nCount > 0)
            {
                NetLog.Log($"重发率: {mibs[(int)UDP_STATISTIC_TYPE.ReSendCount].nCount / mibs[(int)UDP_STATISTIC_TYPE.FirstSendCount].nCount}");
            }
            else
            {
                NetLog.Log($"重发率: N");
            }
        }
    }

    internal static partial class MSQuicFunc
    {
        public static readonly udp_socket_statistic mSocketStatistic = new udp_socket_statistic();

        [Conditional("DEBUG")]
        public static void NET_ADD_STATS(QUIC_PARTITION Partition, UDP_STATISTIC_TYPE mMib)
        {
            Partition.udp_statistic.NET_ADD_STATS(mMib);
        }

        [Conditional("DEBUG")]
        public static void NET_ADD_AVERAGE_STATS(QUIC_PARTITION Partition, UDP_STATISTIC_TYPE mMib, double nValue)
        {
            Partition.udp_statistic.NET_ADD_AVERAGE_STATS(mMib, nValue);
        }

        [Conditional("DEBUG")]
        public static void udp_statistic_printInfo()
        {
            quic_partition_udp_statistic mm = new quic_partition_udp_statistic();
            for (int ProcIndex = 0; ProcIndex < MsQuicLib.PartitionCount; ++ProcIndex)
            {
                mm.Combine(MsQuicLib.Partitions[ProcIndex].udp_statistic);
            }

            mm.PRINT_NET_STATS();
            mSocketStatistic.PRINT_NET_STATS();
        }
    }
}
