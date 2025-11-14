/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:47
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace MSQuic1
{
    internal enum UDP_STATISTIC_TYPE : int
    {
        LOSS_DETECTION_TIME_AVERAGE,

        //计时器
        QUIC_PERF_COUNTER_TIMER_PACING,
        QUIC_PERF_COUNTER_TIMER_ACK_DELAY,
        QUIC_PERF_COUNTER_TIMER_LOSS_DETECTION,
        QUIC_PERF_COUNTER_TIMER_KEEP_ALIVE,
        QUIC_PERF_COUNTER_TIMER_IDLE,
        QUIC_PERF_COUNTER_TIMER_SHUTDOWN,
            
        MAX, //统计数量
    }

    internal class quic_partition_udp_statistic
    {
        internal enum MIB_LOG_TYPE
        {
            COUNT,
            AVERAGE,
        }

        internal class udp_statistic_cell
        {
            public MIB_LOG_TYPE nType = MIB_LOG_TYPE.COUNT;
            public long nMin = long.MaxValue;
            public long nMax;
            public long nCount;
            public long nValue;

            public void Combine(udp_statistic_cell other)
            {
                if (other == null) return;

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
        public static readonly string[] mMitDesList = new string[]
        {
            "LOSS_DETECTION 触发次数 平均时间",

            "QUIC_CONN_TIMER_PACING 触发次数",
            "QUIC_CONN_TIMER_ACK_DELAY 触发次数",
            "QUIC_CONN_TIMER_LOSS_DETECTION 触发次数",
            "QUIC_CONN_TIMER_KEEP_ALIVE 触发次数",
            "QUIC_CONN_TIMER_IDLE 触发次数",
            "QUIC_CONN_TIMER_SHUTDOWN 触发次数",
        };

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

        public void NET_ADD_AVERAGE_STATS(UDP_STATISTIC_TYPE mMib, long nValue)
        {
            if (mibs[(int)mMib] == null)
            {
                mibs[(int)mMib] = new udp_statistic_cell();
            }
            udp_statistic_cell mCell = mibs[(int)mMib];

            mCell.nCount++;
            mCell.nType = MIB_LOG_TYPE.AVERAGE;
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

        public void PRINT_NET_STATS()
        {
            for (int i = 0; i < (int)UDP_STATISTIC_TYPE.MAX; i++)
            {
                udp_statistic_cell mCell = mibs[i];
                string mibDes = mMitDesList[i];
                if (string.IsNullOrWhiteSpace(mibDes))
                {
                    mibDes = ((UDP_STATISTIC_TYPE)i).ToString();
                }

                if (mCell == null)
                {
                    NetLog.Log($"{mibDes} : null");
                }
                else
                {
                    if (mCell.nType == MIB_LOG_TYPE.AVERAGE)
                    {
                        NetLog.Log($"{mibDes} : {mCell.nCount}: {mCell.nValue / mCell.nCount}, {mCell.nMin}, {mCell.nMax}");
                    }
                    else
                    {
                        NetLog.Log($"{mibDes} : {mCell.nCount}");
                    }
                }
            }
        }
    }

    internal static partial class MSQuicFunc
    {
        public static void NET_ADD_STATS(QUIC_PARTITION Partition, UDP_STATISTIC_TYPE mMib)
        {
            Partition.udp_statistic.NET_ADD_STATS(mMib);
        }

        public static void NET_ADD_AVERAGE_STATS(QUIC_PARTITION Partition, UDP_STATISTIC_TYPE mMib, long nValue)
        {
            Partition.udp_statistic.NET_ADD_AVERAGE_STATS(mMib, nValue);
        }

        public static void udp_statistic_printInfo()
        {
            quic_partition_udp_statistic mm = new quic_partition_udp_statistic();
            for (int ProcIndex = 0; ProcIndex < MsQuicLib.PartitionCount; ++ProcIndex)
            {
                mm.Combine(MsQuicLib.Partitions[ProcIndex].udp_statistic);
            }

            mm.PRINT_NET_STATS();
        }
    }
}
