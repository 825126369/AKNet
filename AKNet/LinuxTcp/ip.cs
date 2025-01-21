/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

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
        public readonly ip_options opt = new ip_options();
        public ushort flags; //这是一个位域，用于存储多个标志位，每个标志位代表一个特定的状态或属性。
    }

    public class iphdr
    {
        public byte version;//用途：IP 协议版本，通常为 4，表示 IPv4。
        public byte ihl;//用途：IP 头部长度，单位是 32 位字（4 字节）。标准 IP 头部长度为 20 字节，因此 ihl 通常为 5。

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
        public void WriteTo(Span<byte> mBuffer)
        {
            mBuffer[0] = (byte)(version << 4 | ihl);
            mBuffer[1] = tos;
            EndianBitConverter.SetBytes(mBuffer, 2, tot_len);
            EndianBitConverter.SetBytes(mBuffer, 4, id);
            EndianBitConverter.SetBytes(mBuffer, 6, frag_off);
            mBuffer[8] = ttl;
            mBuffer[9] = protocol;
            EndianBitConverter.SetBytes(mBuffer, 10, check);
            EndianBitConverter.SetBytes(mBuffer, 12, saddr);
            EndianBitConverter.SetBytes(mBuffer, 16, daddr);
        }

        public void WriteFrom(ReadOnlySpan<byte> mBuffer)
        {
            version = (byte)(mBuffer[0] >> 4);
            ihl = (byte)((byte)(mBuffer[0] << 4) >> 4);
            tos = mBuffer[1];
            tot_len = EndianBitConverter.ToUInt16(mBuffer, 2);
            id = EndianBitConverter.ToUInt16(mBuffer, 4);
            frag_off = EndianBitConverter.ToUInt16(mBuffer, 6);
            ttl = mBuffer[8];
            protocol = mBuffer[9];
            check = EndianBitConverter.ToUInt16(mBuffer, 10);
            saddr = EndianBitConverter.ToUInt32(mBuffer, 12);
            daddr = EndianBitConverter.ToUInt32(mBuffer, 16);
        }
    }

    public class ip_options
    {
        public int faddr; //描述：最终目的地址（Final Address）。当使用源路由选项时，这个字段表示数据包的最终目标地址。
        public int nexthop; //描述：下一跳地址。当使用源路由选项时，这个字段表示下一个中间节点的地址。
        public byte optlen; //描述：选项部分的总长度（以字节为单位），包括所有选项和填充字节。
        public byte srr; //描述：源路由记录（Source Route Record）的长度。如果设置了源路由选项，则此字段表示源路由记录的长度。
        public byte rr; //描述：记录路由（Record Route）选项的长度。如果设置了记录路由选项，则此字段表示记录路由的长度。
        public byte ts; //描述：时间戳（Timestamp）选项的长度。如果设置了时间戳选项，则此字段表示时间戳的长度。
        public byte is_strictroute;//位域，表示是否使用严格源路由（Strict Source Route）。如果设置为 1，则必须按照指定路径中的每个路由器进行转发；如果为 0，则允许某些路由器绕过。
        public byte srr_is_hit; //位域，表示当前节点是否是源路由中的一个点。如果是，则设置为 1。
        public byte is_changed;//位域，表示 IP 选项是否被修改过。这对于确保选项的一致性和安全性非常重要
        public byte rr_needaddr; //位域，表示记录路由选项是否需要添加当前节点的地址。
        public byte ts_needtime;//描述：位域，表示时间戳选项是否需要添加当前时间。
        public byte ts_needaddr;//位域，表示时间戳选项是否需要添加当前节点的地址。
        public byte router_alert;//描述：路由器告警（Router Alert）选项的值。这个选项用于通知沿途的路由器对特定的数据包进行特殊处理。
        public byte cipso;//描述：CIPSO（Commercial IP Security Option）选项的长度。CIPSO 提供了一种方法来标记 IP 数据包的安全级别。
        public byte __pad2;//描述：填充字段，用于对齐或保留未来扩展。
        public byte[] __data;//描述：可变长度数组，用于存储实际的 IP 选项数据。不同的选项类型有不同的格式和长度。
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

        public static iphdr ip_hdr(sk_buff skb)
        {
            if(skb.iphdr_cache == null)
            {
                var mData = skb_network_header(skb);
                skb.iphdr_cache = new iphdr();
                skb.iphdr_cache.WriteFrom(mData);
            }
            return skb.iphdr_cache;
        }

        // Linux 内核中用于计算伪头部校验和的函数。
        // 它在处理 TCP 和 UDP 数据包时被调用，用于计算伪头部校验和，确保数据包的完整性和正确性。
        static uint inet_compute_pseudo(sk_buff skb, byte proto)
        {
	        return csum_tcpudp_nofold(ip_hdr(skb).saddr, ip_hdr(skb).daddr, skb.len, proto, 0);
        }

    }

}
