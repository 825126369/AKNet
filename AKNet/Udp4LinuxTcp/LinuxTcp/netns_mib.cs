/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp.Common
{
    //管理信息库
    internal class netns_mib
    {
        public tcp_mib tcp_statistics = new tcp_mib();
    }

    internal class tcp_mib
    {
        public long[] mibs = new long[(int)TCPMIB.MAX];
    }

    internal enum TCPMIB
    {
        DELIVERED, //总分发数量

        RTOMIN, //最小的RTO
        RTOMAX, //最大的RTO

        RENO_RECOVERY, //传统恢复
        SACK_RECOVERY, //SACK恢复

        LOSS_UNDO, //撤销因丢包导致的重传操作的次数
        FULL_UNDO, //完全撤销重传操作的次数

        OFO_QUEUE, //击中乱序队列的次数

        MTUP_SUCCESS,   //MTU探测成功
        MTUP_FAIL, //MTU 探测失败

        RENO_REORDER, //RENO 重排序 击中次数
        TS_REORDER,
        SACK_REORDER,

        LOSS_PROBE_RECOVERY,//尾部丢包探测恢复次数

        DELAYED_ACKS, //延迟ACK 定时器触发次数

        MAX, //统计数量
    }

    internal static partial class LinuxTcpFunc
    {
        //统计状态
        public static void NET_ADD_STATS(net net, TCPMIB mMib, int nAddCount)
        {
            net.mib.tcp_statistics.mibs[(int)mMib] += nAddCount;
        }

        public static void PRINT_NET_STATS()
        {
            
        }

    }
}
