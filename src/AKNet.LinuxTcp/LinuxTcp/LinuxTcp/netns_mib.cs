/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:08
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.LinuxTcp.Common
{
    //管理信息库
    internal class netns_mib
    {
        public tcp_mib tcp_statistics = new tcp_mib();
    }

    internal enum MIB_LOG_TYPE
    {
        COUNT,
        AVERAGE,
    }

    internal class tcp_mib_cell
    {
        public MIB_LOG_TYPE nType = MIB_LOG_TYPE.COUNT;
        public long nMin = long.MaxValue;
        public long nMax;
        public long nCount;
        public long nValue;
    }

    internal class tcp_mib
    {
        public tcp_mib_cell[] mibs = new tcp_mib_cell[(int)TCPMIB.MAX];

    }

    internal enum TCPMIB:int
    {
        SEND_COUNT = 0, //总分发数量
        RECEIVE_COUNT,

        RTT_AVERAGE,
        RTO_AVERAGE,

        FAST_PATH, //击中FastPath的次数
        OFO_QUEUE, //击中乱序队列的次数

        MTUP,
        MTUP_SUCCESS,   //MTU探测成功
        MTUP_FAIL, //MTU 探测失败

        DELAYED_ACK_TIMER, //延迟ACK定时器
        REO_TIMEOUT_TIMER, //重排序超时 定时器
        LOSS_PROBE_TIMER, //尾丢失探测 定时器
        RETRANS_TIMER, //重传超时 定时器
        PROBE0_TIMER, //零窗口探测 定时器
        KEEPALIVE_TIMER, //心跳 定时器
        PACING_TIMER, //发送速率 定时器
        COMPRESSED_ACK_TIMER, //压缩ACK 定时器

        QUICK_ACK,
        DELAYED_ACK,
        COMPRESSED_ACK,
        
        sk_pacing_rate,
        seq_rtt_us,
        sack_rtt_us,
        ca_rtt_us,
        sacked,
        send_sack_count,
        receive_sack_count,

        sp_count,
        send_dsack_count,
        receive_dsack_count,

        sk_sndbuf,
        sk_rcvbuf,
        snd_wnd,
        rcv_wnd,
        snd_cwnd,

        FLAG_SND_UNA_ADVANCED,
        __skb_tstamp_tx,
        tcp_shift_skb_data,

        TCP_DSACK_RECV,
        TCP_DSACK_OFO_RECV,
        TCP_DSACK_IGNORED_DUBIOUS, // DSACK忽略可疑的
        TCP_DSACK_RECV_SEGS,

        TCP_DSACK_IGNORED_NO_UNDO,
        TCP_DSACK_IGNORED_OLD,
        TCP_SACK_DISCARD,
        MAX, //统计数量
    }

    internal static class TcpMibMgr
    {
        public static readonly string[] mMitDesList = new string[(int)TCPMIB.MAX]
        {
            "发包数量",
            "收包数量",
            "平均RTT",
            "平均RTO",
            "快速路径 击中次数",
            "乱序队列击中次数",
            "MTU 探测次数",
            "MTU 探测成功次数",
            "MTU 探测失败次数",

            "延迟ACK 定时器 触发次数",
            "重排序超时 定时器 触发次数",
            "尾丢失探测 定时器 触发次数",
            "重传超时 定时器 触发次数",
            "零窗口探测 定时器 触发次数",
            "心跳 定时器 触发次数",
            "发送速率 定时器 触发次数",
            "压缩ACK 定时器 触发次数",

            "快速ACK 触发次数",
            "延迟ACK 触发次数",
            "压缩ACK 触发次数",

            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            ""
        };

        //统计状态
        public static void NET_ADD_STATS(net net, TCPMIB mMib)
        {
            if (LinuxTcpFunc.init_net.mib.tcp_statistics.mibs[(int)mMib] == null)
            {
                LinuxTcpFunc.init_net.mib.tcp_statistics.mibs[(int)mMib] = new tcp_mib_cell();
            }
            tcp_mib_cell mCell = LinuxTcpFunc.init_net.mib.tcp_statistics.mibs[(int)mMib];

            mCell.nType = MIB_LOG_TYPE.COUNT;
            mCell.nCount++;
        }

        public static void NET_ADD_AVERAGE_STATS(net net, TCPMIB mMib, long nValue)
        {
            if (LinuxTcpFunc.init_net.mib.tcp_statistics.mibs[(int)mMib] == null)
            {
                LinuxTcpFunc.init_net.mib.tcp_statistics.mibs[(int)mMib] = new tcp_mib_cell();
            }
            tcp_mib_cell mCell = LinuxTcpFunc.init_net.mib.tcp_statistics.mibs[(int)mMib];

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

        public static void PRINT_NET_STATS()
        {
            for (int i = 0; i < (int)TCPMIB.MAX; i++)
            {
                tcp_mib_cell mCell = LinuxTcpFunc.init_net.mib.tcp_statistics.mibs[i];
                string mibDes = mMitDesList[i];
                if(string.IsNullOrWhiteSpace(mibDes))
                {
                    mibDes = ((TCPMIB)i).ToString();
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
}
