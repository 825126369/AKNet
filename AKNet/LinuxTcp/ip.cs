/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AKNet.LinuxTcp
{
    internal class inet_skb_parm
    {
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
        //表示数据包进入系统的接口索引（Incoming Interface）。这对于路由决策和策略路由非常重要
        public int iif;
        //IP 选项可以携带额外的信息，如记录路由（Record Route）、时间戳（Timestamp）等。
        //这些选项在数据包头部中以特定格式编码，并在此字段中解析为更易于处理的形式。
        public ip_options opt;
        public ushort flags; //这是一个位域，用于存储多个标志位，每个标志位代表一个特定的状态或属性。
    }

    public class iphdr
    {
        public byte ihl;//用途：IP 头部长度，单位是 32 位字（4 字节）。标准 IP 头部长度为 20 字节，因此 ihl 通常为 5。
        public byte version;//用途：IP 协议版本，通常为 4，表示 IPv4。

        public byte tos;//Type of Service（服务类型），用于 QoS（Quality of Service）标记。
        public ushort tot_len;//IP 数据包的总长度，包括 IP 头部和数据部分。
        public ushort id;//IP 数据包的标识符，用于标识数据包的片段。
        public ushort frag_off;//片段偏移，表示数据包片段在原始数据包中的位置。
        public byte ttl;//Time to Live（生存时间），表示数据包可以经过的跳数。
        public byte protocol;//上层协议类型，例如 TCP (6)、UDP (17)。
        public ushort check;//IP 头部的校验和，用于验证 IP 头部的完整性。

        public uint saddr;//用途：源 IP 地址。
        public uint daddr;//目的 IP 地址。

        //options：类型：可变长度 用途：IP 选项，用于扩展 IP 头部的功能。示例：IP 选项可以包括记录路由、时间戳等。
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
            if (skb.inet_skb_parm_cb_cache == null)
            {
                skb.inet_skb_parm_cb_cache = new inet_skb_parm();
                
            }
            return skb.inet_skb_parm_cb_cache;
        }

        static iphdr ip_hdr(sk_buff skb)
        {
            if(skb.iphdr_cache == null)
            {
                var mData = skb_network_header(skb);
            }
            return skb.iphdr_cache;
        }

        static uint inet_compute_pseudo(sk_buff skb, byte proto)
        {
	        return csum_tcpudp_nofold(ip_hdr(skb).saddr, ip_hdr(skb).daddr, skb.len, proto, 0);
        }

    }

}
