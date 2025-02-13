/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Runtime.CompilerServices;

namespace AKNet.Udp4LinuxTcp.Common
{
    //管理信息库
    internal class netns_mib
    {
        public tcp_mib tcp_statistics = new tcp_mib();
    }

    internal class tcp_mib_cell
    {
        public long nCount;
        public long nValue;
    }

    internal class tcp_mib
    {
        public tcp_mib_cell[] mibs = new tcp_mib_cell[(int)TCPMIB.MAX];
    }

    internal enum TCPMIB:int
    {
        DELIVERED = 0, //总分发数量
        RTO_AVERAGE,

        FAST_PATH, //击中FastPath的次数
        OFO_QUEUE, //击中乱序队列的次数

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

        MAX, //统计数量
    }

    internal static class TcpMibMgr
    {
        static long nRttCount = 0;
        static long nRttSumTime = 0;
        static long nRttMinTime = long.MaxValue;
        static long nRttMaxTime;

        static long nRTOCount = 0;
        static long nRTOSumTime = 0;
        static long nRTOMinTime = long.MaxValue;
        static long nRTOMaxTime = 0;

        public static readonly string[] mMitDesList = new string[(int)TCPMIB.MAX]
        {
            "总分发包数量",
            "平均RTO",
            "快速路径 击中次数",
            "乱序队列击中次数",
            "MTU探测成功次数",
            "MTU探测失败次数",

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
        };

        internal static void AddRTO(long nRTO)
        {
            nRTOCount++;
            nRTOSumTime += nRTO;
            if (nRTO < nRTOMinTime)
            {
                nRTOMinTime = nRTO;
            }
            else if (nRTO > nRTOMaxTime)
            {
                nRTOMaxTime = nRTO;
            }
        }

        internal static void AddRtt(long nRtt)
        {
            nRttCount++;
            nRttSumTime += nRtt;
            if (nRtt < nRttMinTime)
            {
                nRttMinTime = nRtt;
            }
            else if (nRtt > nRttMaxTime)
            {
                nRttMaxTime = nRtt;
            }
        }

        //统计状态
        public static void NET_ADD_STATS(net net, TCPMIB mMib, long nValue = 0)
        {
            if (net.mib.tcp_statistics.mibs[(int)mMib] == null)
            {
                net.mib.tcp_statistics.mibs[(int)mMib] = new tcp_mib_cell();
            }
            net.mib.tcp_statistics.mibs[(int)mMib].nCount++;
            net.mib.tcp_statistics.mibs[(int)mMib].nValue += nValue;
        }

        public static void PRINT_NET_STATS()
        {
            NetLog.Log($"最小的RTT: {nRttMinTime}");
            NetLog.Log($"最大的RTT: {nRttMaxTime}");
            NetLog.Log($"平均RTT: {nRttSumTime / nRttCount}");

            NetLog.Log($"最小的RTO: {nRTOMinTime}");
            NetLog.Log($"最大的RTO: {nRTOMaxTime}");
            NetLog.Log($"平均RTO: {nRTOSumTime / nRTOCount}");

            for (int i = 0; i < (int)TCPMIB.MAX; i++)
            {
                tcp_mib_cell mCell = LinuxTcpFunc.init_net.mib.tcp_statistics.mibs[i];
                if (mCell == null)
                {
                    NetLog.Log($"{mMitDesList[i]} : null");
                }
                else
                {
                    if (i == (int)TCPMIB.RTO_AVERAGE)
                    {
                        NetLog.Log($"{mMitDesList[i]} : {mCell.nValue / mCell.nCount}");
                    }
                    else
                    {
                        NetLog.Log($"{mMitDesList[i]} : {mCell.nCount}");
                    }
                }
            }
        }

    }
}
