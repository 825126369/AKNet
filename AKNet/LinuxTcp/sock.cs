/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;

namespace AKNet.LinuxTcp
{
    internal class sock_common
    {
        public int skc_daddr;
        public int skc_rcv_saddr;
        public ushort skc_dport;
        public ushort skc_num;
        public ushort skc_tx_queue_mapping; //传输队列编号
    }

    internal class sk_buff_Comparer : IComparer<sk_buff>
    {
        public int Compare(sk_buff x, sk_buff y)
        {
            return (int)(LinuxTcpFunc.TCP_SKB_CB(x).seq - LinuxTcpFunc.TCP_SKB_CB(y).seq);
        }
    }

    internal class sock : sock_common
    {
        public int sk_err;
        public int sk_err_soft;

        public LinkedList<sk_buff> sk_send_head;
        public LinkedList<sk_buff> sk_write_queue;
        private static sk_buff_Comparer sk_Buff_Comparer = new sk_buff_Comparer();
        public AkRBTree<sk_buff> tcp_rtx_queue = new AkRBTree<sk_buff>(sk_Buff_Comparer);

        public net sk_net;
        public ulong sk_flags;
        public uint sk_txhash;
        public int sk_refcnt;

        public sk_family sk_family;

        //sk_sndbuf 是 Linux 内核中 struct sock（套接字结构体）的一个成员变量，用于定义套接字的发送缓冲区大小。
        //这个参数控制了应用程序可以一次性写入套接字的最大数据量，并且对 TCP 连接的性能和行为有重要影响。
        public int sk_sndbuf;
        public bool sk_dst_pending_confirm;

        //sk_gso_type
        //类型：u16（无符号16位整数）
        //作用：标识套接字上启用的GSO类型。
        //不同的协议或传输层可能会有不同的GSO实现方式，因此这个字段用于指定当前套接字应该使用哪种类型的GSO。
        //常见的GSO类型包括但不限于：
        //SKB_GSO_TCPV4：适用于IPv4上的TCP。
        //SKB_GSO_TCPV6：适用于IPv6上的TCP。
        //SKB_GSO_UDP：适用于UDP。
        //SKB_GSO_GRE：适用于GRE封装的数据包。
        public ushort sk_gso_type;
        public ushort sk_gso_max_segs;
        public uint sk_gso_max_size;

        public byte sk_pacing_shift;

        public sk_backlog sk_backlog;
        public int sk_rcvbuf;
        public uint sk_reserved_mem;

        //sk_wmem_queued 是Linux内核网络协议栈中的一个重要字段，用于跟踪已排队等待发送的数据量。
        //它位于 struct sock 结构体中，表示已经分配给套接字发送缓冲区但尚未实际发送到网络上的数据总量。
        //这个字段对于管理TCP连接的拥塞控制、流量控制和资源管理非常重要。
        //发送缓冲区管理：确保发送缓冲区的内存使用量在合理的范围内，避免过度消耗系统资源。
        //拥塞控制：通过动态调整拥塞窗口大小，防止发送方发送过多数据导致网络拥塞。
        //性能优化：合理设置发送缓冲区大小可以提高网络传输效率，减少延迟和丢包率。
        public int sk_wmem_queued;
        public dst_entry sk_dst_cache;

        //sk_wmem_alloc是 Linux 内核中sock结构体的一个成员变量，
        //用于统计已经提交到 IP 层，但还没有从本机发送出去的 skb（套接字缓冲区）占用空间大小
        public long sk_wmem_alloc;
        public ulong sk_tsq_flags;

        public long sk_rmem_alloc
        {
            get { return sk_backlog.rmem_alloc; }
            set { sk_backlog.rmem_alloc = value; }
        }

        public ushort sk_tx_queue_mapping
        {
            get
            {
                return skc_tx_queue_mapping;
            }

            set
            {
                skc_tx_queue_mapping = value;
            }
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
        public static net sock_net(sock sk)
        {
            return sk.sk_net;
        }

        public static bool sock_flag(sock sk, sock_flags flag)
        {
            return ((ulong)flag & sk.sk_flags) > 0;
        }

        public static void __sock_put(sock sk)
        {
            sk.sk_refcnt--;
        }

        public static void sk_reset_timer(sock sk, HRTimer timer, long expires)
        {
            timer.ModTimer(TimeSpan.FromMilliseconds(expires));
        }

        static int sk_unused_reserved_mem(sock sk)
        {
            if (sk.sk_reserved_mem == 0)
                return 0;

            int unused_mem = (int)(sk.sk_reserved_mem - sk.sk_wmem_queued - sk.sk_rmem_alloc);
            return unused_mem > 0 ? unused_mem : 0;
        }

        static void skb_set_hash_from_sk(sk_buff skb, sock sk)
        {
            /* This pairs with WRITE_ONCE() in sk_set_txhash() */
            uint txhash = sk.sk_txhash;

            if (txhash > 0)
            {
                skb.l4_hash = true;
                skb.hash = txhash;
            }
        }

        static void __sk_dst_reset(sock sk)
        {
            __sk_dst_set(sk, null);
        }

        static void __sk_dst_set(sock sk, dst_entry dst)
        {
            dst_entry old_dst;
            sk_tx_queue_clear(sk);
            sk.sk_dst_pending_confirm = false;
            old_dst = sk.sk_dst_cache;
            sk.sk_dst_cache = dst;
        }

        static void sk_tx_queue_clear(sock sk)
        {
            sk.sk_tx_queue_mapping = ushort.MaxValue;
        }

    }
}
