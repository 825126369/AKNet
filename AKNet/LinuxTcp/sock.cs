/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.LinuxTcp;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.LinuxTcp
{
    internal class sock_common
    {
        public int skc_daddr;
        public int skc_rcv_saddr;
        public ushort skc_dport;
        public ushort skc_num;
        public ushort skc_tx_queue_mapping; //传输队列编号
        public byte skc_state;
        //public proto  skc_prot;
    }

    public class sockcm_cookie
    {
        public long transmit_time;
        public uint mark;
        public uint tsflags;
        public uint ts_opt_id;
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
        //主要用于记录那些不会立即导致连接关闭或终止的临时性错误
        public int sk_err_soft;

        public LinkedList<sk_buff> sk_send_head;
        public sk_buff_head sk_write_queue;
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

        //sk_forward_alloc 字段表示已经承诺但尚未实际分配给该套接字的数据量。
        //这是一种预先分配机制，旨在优化性能和资源管理。
        //当应用程序调用 send() 或类似函数发送数据时，这些数据可能不会立即写入到网络中，而是先存储在套接字的发送缓冲区中。
        //此时，sk_forward_alloc 会增加相应的值来反映已承诺将要使用的额外缓冲区空间。
        public int sk_forward_alloc;
        public ulong sk_tsq_flags;
        public uint sk_tsflags;
        //它表示套接字发送操作的超时时间。
        //这个超时值用于确定当套接字处于阻塞模式时，发送操作（如 send(), sendto(), sendmsg() 等）等待完成的最大时间。
        public long sk_sndtimeo;
        public TimerList sk_timer;

        public socket_wq sk_wq;

        public long sk_zckey;
        public long sk_tskey;

        //public byte sk_prot
        //{
        //    get { return skc_prot; }
        //    set { skc_prot = value; }
        //}

        public byte sk_state
        {
            get { return skc_state; }
            set { skc_state = value; }
        }

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

        public static void sk_reset_timer(sock sk, TimerList timer, long expires)
        {
            timer.ModTimer(expires);
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

        static bool sk_has_account(sock sk)
        {
            return true;
        }

        static int sk_mem_pages(int amt)
        {
            return (amt + PAGE_SIZE - 1) >> PAGE_SHIFT;
        }

        static void sk_forward_alloc_add(sock sk, int val)
        {
            sk.sk_forward_alloc = sk.sk_forward_alloc + val;
        }

        static int __sk_mem_raise_allocated(sock sk, int size, int amt, int kind)
        {
            return 0;
        }

        static int __sk_mem_schedule(sock sk, int size, int kind)
        {
            int ret, amt = sk_mem_pages(size);

            sk_forward_alloc_add(sk, amt << PAGE_SHIFT);
            ret = __sk_mem_raise_allocated(sk, size, amt, kind);
            if (ret == 0)
            {
                sk_forward_alloc_add(sk, -(amt << PAGE_SHIFT));
            }
            return ret;
        }

        static bool sk_wmem_schedule(sock sk, int size)
        {
            int delta;
            if (!sk_has_account(sk))
            {
                return true;
            }
            delta = size - sk.sk_forward_alloc;
            return delta <= 0 || __sk_mem_schedule(sk, delta, SK_MEM_SEND) > 0;
        }

        static void sk_stop_timer(sock sk, TimerList timer)
        {
            timer.Stop();
        }

        static bool sock_owned_by_user(sock sk)
        {
            return false;
        }

        static void sk_wmem_queued_add(sock sk, int val)
        {
            sk.sk_wmem_queued = sk.sk_wmem_queued + val;
        }

        static void sk_mem_uncharge(sock sk, int size)
        {
            if (!sk_has_account(sk))
                return;

            sk_forward_alloc_add(sk, size);
        }

        static dst_entry __sk_dst_get(sock sk)
        {
            return sk.sk_dst_cache;
        }

        static void sk_stream_moderate_sndbuf(sock sk)
        {
            uint val;
            val = (uint)Math.Min(sk.sk_sndbuf, sk.sk_wmem_queued >> 1);
            val = (uint)Math.Max(val, sk_unused_reserved_mem(sk));

            sk.sk_sndbuf = (int)Math.Max(val, SOCK_MIN_SNDBUF);
        }

        static long sock_sndtimeo(sock sk, bool noblock)
        {
            return noblock ? 0 : sk.sk_sndtimeo;
        }

        static long sk_wmem_alloc_get(sock sk)
        {
            return sk.sk_wmem_alloc - 1;
        }

        static sockcm_cookie sockcm_init(sock sk)
        {
            var sockc = new sockcm_cookie();
            sockc.tsflags = sk.sk_tsflags;
            return sockc;
        }

        static void sk_clear_bit(int nr, sock sk)
        {
            if ((nr == SOCKWQ_ASYNC_NOSPACE || nr == SOCKWQ_ASYNC_WAITDATA) && !sock_flag(sk, sock_flags.SOCK_FASYNC))
            {
                return;
            }

            sk.sk_wq.flags &= (ulong)1 << nr;
        }

        static bool sk_stream_memory_free(sock sk)
        {
            return true;
        }

        static void __sk_flush_backlog(sock sk)
        { 
            tcp_release_cb(sk as tcp_sock);
        }

        static bool sk_flush_backlog(sock sk)
        {
	        if (sk.sk_backlog.mQueue.Count > 0)) 
            {
		        __sk_flush_backlog(sk);
		        return true;
	        }
	        return false;
        }

        static int skb_do_copy_data_nocache(sock sk, sk_buff skb, iov_iter from, byte[] to, int copy, int offset)
        {
            if (skb.ip_summed == CHECKSUM_NONE)
            {
                long csum = 0;
                if (!csum_and_copy_from_iter_full(to, copy, &csum, from))
                {
                    return -EFAULT;
                }
                skb.csum = csum_block_add(skb.csum, csum, offset);
            }
            else if (sk->sk_route_caps & NETIF_F_NOCACHE_COPY)
            {
                if (!copy_from_iter_full_nocache(to, copy, from))
                    return -EFAULT;
            }
            else if (!copy_from_iter_full(to, copy, from))
            {
                return -EFAULT;
            }

            return 0;
        }

        static int skb_copy_to_page_nocache(sock sk, ReadOnlySpan<byte> from, sk_buff skb, int off, int copy)
        {
            int err;
            err = skb_do_copy_data_nocache(sk, skb, from, page_address(page) + off, copy, skb.len);
            if (err > 0)
            {
                return err;
            }

            skb_len_add(skb, copy);
            sk_wmem_queued_add(sk, copy);
            sk_mem_charge(sk, copy);
            return 0;
        }

        static void __sock_tx_timestamp(uint tsflags, out byte tx_flags)
        {
            byte flags = 0;

            if (BoolOk(tsflags & SOF_TIMESTAMPING_TX_HARDWARE))
            {
                flags |= SKBTX_HW_TSTAMP;
                if (BoolOk(tsflags & SOF_TIMESTAMPING_BIND_PHC))
                {
                    flags |= SKBTX_HW_TSTAMP_USE_CYCLES;
                }
            }

            if (BoolOk(tsflags & SOF_TIMESTAMPING_TX_SOFTWARE))
            {
                flags |= SKBTX_SW_TSTAMP;
            }

            if (BoolOk(tsflags & SOF_TIMESTAMPING_TX_SCHED))
            {
                flags |= SKBTX_SCHED_TSTAMP;
            }

            tx_flags = flags;
        }

        static void _sock_tx_timestamp(sock sk, sockcm_cookie sockc, out byte tx_flags, out uint tskey)
        {
            tx_flags = 0;
            tskey = 0;

            uint tsflags = sockc.tsflags;
            if (tsflags > 0)
            {
                __sock_tx_timestamp(tsflags, tx_flags);
                if (BoolOk(tsflags & SOF_TIMESTAMPING_OPT_ID) && tskey > 0 && BoolOk(tsflags & SOF_TIMESTAMPING_TX_RECORD_MASK))
                {
                    if (BoolOk(tsflags & SOCKCM_FLAG_TS_OPT_ID))
                    {
                        tskey = sockc.ts_opt_id;
                    }
                    else
                    {
                        tskey = sk.sk_tskey - 1;
                    }
                }
            }

            if (sock_flag(sk, SOCK_WIFI_STATUS))
            {
                tx_flags |= SKBTX_WIFI_STATUS;
            }
        }

        static void sock_tx_timestamp(sock sk, sockcm_cookie sockc, out byte tx_flags)
        {
            _sock_tx_timestamp(sk, sockc, out tx_flags, out _);
        }


    }
}
