/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class sock_common
    {
       
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
        public readonly page_frag    sk_frag = new page_frag();
        public readonly sk_buff_head sk_write_queue = new sk_buff_head();
        public readonly sk_buff_head sk_receive_queue = new sk_buff_head();
        public readonly sk_buff_head sk_error_queue = new sk_buff_head();
        public readonly rb_root tcp_rtx_queue = new rb_root();
        public readonly net sk_net = new net();

        public uint sk_mark;
        public ulong sk_flags;
        public uint sk_txhash;
        public int sk_refcnt;

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
        //TSQ 功能用于优化 TCP 数据包的发送，特别是在 自动软木塞 的场景中。sk_tsq_flags 包含多个标志位，用于跟踪和控制 TSQ 的状态。
        //用于控制 TCP 的小队列（TSQ）功能
        public ulong sk_tsq_flags;
        public uint sk_tsflags;
        //它表示套接字发送操作的超时时间。
        //这个超时值用于确定当套接字处于阻塞模式时，发送操作（如 send(), sendto(), sendmsg() 等）等待完成的最大时间。
        public long sk_sndtimeo;
        //sk_rcvtimeo 是 Linux 内核中与套接字（socket）相关的内部变量，
        //它表示接收操作的超时时间。
        //这个超时时间用于确定当没有数据可读时，阻塞接收操作（如 recv, recvfrom, recvmsg 等）应该等待多久。
        //如果在指定的时间内没有数据到达，则接收调用将返回一个错误，通常带有 EAGAIN 或 EWOULDBLOCK 错误码，
        //这取决于具体的上下文和操作系统版本。
        public long sk_rcvtimeo;
        //sk_rcvlowat 是 Linux 内核中与套接字（socket）相关的内部变量，
        //它定义了接收操作的低水位标记（low-water mark）。
        //这个值决定了内核在调用如 recv, recvfrom, recvmsg 等接收函数时，
        //至少需要有多少数据可用才会唤醒阻塞的读取操作。
        //换句话说，当接收缓冲区中的数据量达到或超过 sk_rcvlowat 指定的字节数时，阻塞的读取操作会被唤醒并继续执行。
        public int sk_rcvlowat;
        public int sk_drops;

        public uint sk_pacing_status; /* see enum sk_pacing */
        public long sk_pacing_rate; /* bytes per second */
        public long sk_max_pacing_rate;

        public byte sk_shutdown;
        public TimerList sk_timer;

        public long sk_zckey;//用于零拷贝操作的计数，确保通知的顺序和唯一性
        public long sk_tskey;//用于时间戳请求的计数，确保每个请求的唯一性

        public byte sk_state;
        public long sk_rmem_alloc;
        public ushort sk_tx_queue_mapping;

        public int sk_write_pending;//检查套接字（socket）是否有未完成的写操作。
        public long  sk_stamp;
    }

    internal static partial class LinuxTcpFunc
    {
        static void sk_dst_confirm(sock sk)
        {
            if (!sk.sk_dst_pending_confirm)
            {
                sk.sk_dst_pending_confirm = true;
            }
        }

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
            {
                return 0;
            }

            int unused_mem = (int)(sk.sk_reserved_mem - sk.sk_wmem_queued - sk.sk_rmem_alloc);
            return unused_mem > 0 ? unused_mem : 0;
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

        static void sk_forward_alloc_add(sock sk, int val)
        {
            sk.sk_forward_alloc = sk.sk_forward_alloc + val;
        }

        static int __sk_mem_raise_allocated(sock sk, int size, int amt, int kind)
        {
            return 0;
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

        static dst_entry __sk_dst_get(tcp_sock tp)
        {
            if (tp.sk_dst_cache == null)
            {
                tp.sk_dst_cache = new dst_entry();
                tp.sk_dst_cache.net = tp.sk_net;
            }
            return tp.sk_dst_cache;
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

        static bool sk_stream_memory_free(sock sk)
        {
            return true;
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
                __sock_tx_timestamp(tsflags, out tx_flags);
                if (BoolOk(tsflags & SOF_TIMESTAMPING_OPT_ID) && tskey > 0 && BoolOk(tsflags & SOF_TIMESTAMPING_TX_RECORD_MASK))
                {
                    if (BoolOk(tsflags & SOCKCM_FLAG_TS_OPT_ID))
                    {
                        tskey = sockc.ts_opt_id;
                    }
                    else
                    {
                        tskey = (uint)sk.sk_tskey - 1;
                    }
                }
            }

            if (sock_flag(sk, sock_flags.SOCK_WIFI_STATUS))
            {
                tx_flags |= SKBTX_WIFI_STATUS;
            }
        }

        static void sock_tx_timestamp(sock sk, sockcm_cookie sockc, out byte tx_flags)
        {
            _sock_tx_timestamp(sk, sockc, out tx_flags, out _);
        }

        static long sock_rcvtimeo(sock sk, bool noblock)
        {
            return noblock ? 0 : sk.sk_rcvtimeo;
        }

        static int sock_rcvlowat(sock sk, bool waitall, int len)
        {
            int v = waitall ? len : Math.Min(sk.sk_rcvlowat, len);
            return v > 0 ? v : 1;
        }

        static void sock_set_flag(tcp_sock tp, sock_flags flag)
        {
	        set_bit((byte)flag, ref tp.sk_flags);
        }

        static void sk_rx_queue_clear(tcp_sock tp)
        {
            
        }

        static void sk_init_common(tcp_sock tp)
        {
            skb_queue_head_init(tp.sk_receive_queue);
            skb_queue_head_init(tp.sk_write_queue);
            skb_queue_head_init(tp.sk_error_queue);
        }

        static void sock_init_data_uid(tcp_sock tp)
        {
            sk_init_common(tp);
            // tp.sk_send_head	=	null;
            tp.sk_timer = new TimerList(0, null, tp);

            tp.sk_rcvbuf = 1024 * 16;
            tp.sk_sndbuf = 1024 * 16;
            tp.sk_state = TCP_CLOSE;

            sock_set_flag(tp, sock_flags.SOCK_ZAPPED);

            tp.sk_frag.page = null;
            tp.sk_frag.offset = 0;
            tp.sk_peek_off = -1;

            tp.sk_write_pending = 0;
            tp.sk_rcvlowat = 1;
            tp.sk_rcvtimeo = long.MaxValue;
            tp.sk_sndtimeo = long.MaxValue;

            tp.sk_stamp = SK_DEFAULT_STAMP;
            tp.sk_zckey = 0;

            tp.sk_max_pacing_rate = long.MaxValue;
            tp.sk_pacing_rate = long.MaxValue;
            tp.sk_pacing_shift = 10;

            sk_rx_queue_clear(tp);
            tp.sk_drops = 0;
        }

        static void sock_init_data(tcp_sock tp)
        {
            sock_init_data_uid(tp);
        }

    }
}
