/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    internal class inet_skb_parm
    {
        public int iif; //表示数据包进入系统的接口索引（Incoming Interface）。这对于路由决策和策略路由非常重要
        public ip_options opt; //IP 选项可以携带额外的信息，如记录路由（Record Route）、时间戳（Timestamp）等。这些选项在数据包头部中以特定格式编码，并在此字段中解析为更易于处理的形式。

        public ushort flags; //这是一个位域，用于存储多个标志位，每个标志位代表一个特定的状态或属性。

        public const ushort IPSKB_FORWARDED = 1; //数据包已经被转发。
        public const ushort IPSKB_XFRM_TUNNEL_SIZE = 1 << 1;//数据包的隧道大小已由 XFRM（Transform）框架处理。
        public const ushort IPSKB_XFRM_TRANSFORMED = 1 << 2; //数据包已经过 XFRM 框架转换。
        public const ushort IPSKB_FRAG_COMPLETE = 1 << 3; //数据包的所有片段都已到达并重组完成。
        public const ushort IPSKB_REROUTED = 1 << 4; //数据包已被重新路由。
        public const ushort IPSKB_DOREDIRECT = 1 << 5; //允许进行重定向。
        public const ushort IPSKB_FRAG_PMTU = 1 << 6; //路径 MTU 发现影响了分片操作。
        public const ushort IPSKB_L3SLAVE = 1 << 7; //数据包是三层从属设备上的。
        public const ushort IPSKB_NOPOLICY = 1 << 8; //忽略安全策略。
        public const ushort IPSKB_MULTIPATH = 1 << 9; //多路径路由被使用。

        public ushort frag_max_size;
    }

    public class iphdr
    {
        public byte ihl;
        public byte version;

        public byte tos;
        public ushort tot_len;
        public ushort id;
        public ushort frag_off;
        public byte ttl;
        public byte protocol;
        public ushort check;

        public int saddr;
        public int daddr;
    }

    internal static partial class LinuxTcpFunc
    {
        //它用于确保 TCP 的重传超时（RTO, Retransmission Timeout）不会超过用户设定的连接超时时间。
        public static void NET_ADD_STATS(net net, LINUXMIB mMib, int nAddCount)
        {
            net.mib.net_statistics.mibs[(int)mMib] += nAddCount;
        }

        public static inet_skb_parm IPCB(sk_buff skb)
        {
            if(skb.inet_skb_parm_cb_cache == null)
            {

            }
            return skb.inet_skb_parm_cb_cache;
        }
    }

}
