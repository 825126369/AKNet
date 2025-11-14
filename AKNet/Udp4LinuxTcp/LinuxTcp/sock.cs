/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Drawing;

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
        public int sk_err_soft;
        public readonly sk_buff_head sk_write_queue = new sk_buff_head();
        public readonly sk_buff_head sk_receive_queue = new sk_buff_head();
        public readonly sk_buff_head sk_error_queue = new sk_buff_head();
        public readonly rb_root tcp_rtx_queue = new rb_root();
        public net sk_net = null;

        //sk_sndbuf 是 Linux 内核中 struct sock（套接字结构体）的一个成员变量，用于定义套接字的发送缓冲区大小。
        //这个参数控制了应用程序可以一次性写入套接字的最大数据量，并且对 TCP 连接的性能和行为有重要影响。
        public int sk_sndbuf;
        public int sk_rcvbuf;
        public uint sk_reserved_mem;
        //sk_wmem_queued 是Linux内核网络协议栈中的一个重要字段，用于跟踪已排队等待发送的数据量。
        //它位于 struct sock 结构体中，表示已经分配给套接字发送缓冲区但尚未实际发送到网络上的数据总量。
        //这个字段对于管理TCP连接的拥塞控制、流量控制和资源管理非常重要。
        //发送缓冲区管理：确保发送缓冲区的内存使用量在合理的范围内，避免过度消耗系统资源。
        //拥塞控制：通过动态调整拥塞窗口大小，防止发送方发送过多数据导致网络拥塞。
        //性能优化：合理设置发送缓冲区大小可以提高网络传输效率，减少延迟和丢包率。
        public int sk_wmem_queued;
        //sk_wmem_alloc是 Linux 内核中sock结构体的一个成员变量，
        //用于统计已经提交到 IP 层，但还没有从本机发送出去的 skb（套接字缓冲区）占用空间大小
        public long sk_wmem_alloc;
        //sk_forward_alloc 字段表示已经承诺但尚未实际分配给该套接字的数据量。
        //这是一种预先分配机制，旨在优化性能和资源管理。
        //当应用程序调用 send() 或类似函数发送数据时，这些数据可能不会立即写入到网络中，而是先存储在套接字的发送缓冲区中。
        //此时，sk_forward_alloc 会增加相应的值来反映已承诺将要使用的额外缓冲区空间。
        public int sk_forward_alloc;
        //sk_rmem_alloc 是 Linux 内核中用于管理套接字接收缓存分配的一个原子计数器，它记录了当前套接字接收队列中已分配的内存总量。
        //这个计数器在 TCP 和其他协议栈中用于确保接收缓存不会超过套接字的接收缓冲区大小（sk_rcvbuf），从而避免内存过度使用
        public int sk_rmem_alloc;


        public ulong sk_flags;
        //TSQ 功能用于优化 TCP 数据包的发送，特别是在 自动软木塞 的场景中。sk_tsq_flags 包含多个标志位，用于跟踪和控制 TSQ 的状态。
        //用于控制 TCP 的小队列（TSQ）功能
        public ulong sk_tsq_flags;
        public uint sk_tsflags;
        public ulong sk_socket_flags;
        public byte sk_userlocks;


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
        
        public TimerList sk_timer;

        public long sk_zckey;//用于零拷贝操作的计数，确保通知的顺序和唯一性
        public long sk_tskey;//用于时间戳请求的计数，确保每个请求的唯一性

        public long sk_stamp;
        public byte sk_state;


        public long sk_max_pacing_rate;
        public uint sk_pacing_status; /* see enum sk_pacing */
        public long sk_pacing_rate; /* bytes per second */
        public byte sk_pacing_shift;
    }

    internal static partial class LinuxTcpFunc
    {
        static void sk_drops_add(sock sk, sk_buff skb)
        {
            sk.sk_drops++;
        }

        public static net sock_net(sock sk)
        {
            if (sk.sk_net != null)
            {
                return sk.sk_net;
            }
            return init_net;
        }

        static void sk_stop_timer(sock sk, TimerList timer)
        {
            timer.Stop();
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

        static bool sk_has_account(sock sk)
        {
            return false;
        }

        static bool sk_wmem_schedule(sock sk, int size)
        {
            if (!sk_has_account(sk))
            {
                return true;
            }

            if (sk.sk_forward_alloc >= size)
            {
                return true;
            }

            sk_forward_alloc_add(sk, size);
            return true;
        }
        
        //sk_mem_charge 函数的主要作用是为套接字分配内存，并更新套接字的内存使用计数。
        //它确保在发送数据时，内核能够正确地跟踪每个套接字的内存使用情况，从而避免内存泄漏或过度使用。
        static void sk_mem_charge(sock sk, int size)
        {
            if (!sk_has_account(sk))
            {
                return;
            }
            sk_forward_alloc_add(sk, -size);
        }

        static void sk_forward_alloc_add(sock sk, int val)
        {
            sk.sk_forward_alloc = sk.sk_forward_alloc + val;
        }

        static int __sk_mem_raise_allocated(sock sk, int size, int amt, int kind)
        {
            return 0;
        }

        static bool sock_owned_by_user(sock sk)
        {
            return false;
        }

        static void sk_wmem_queued_add(sock sk, int val)
        {
            sk.sk_wmem_queued += val;
        }

        static void sk_mem_uncharge(sock sk, int size)
        {
            if (!sk_has_account(sk))
                return;

            sk_forward_alloc_add(sk, size);
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

        static bool __sk_stream_memory_free(sock sk, int wake)
        {
            return sk.sk_wmem_queued < sk.sk_sndbuf;
        }

        static bool sk_stream_memory_free(sock sk)
        {
            return __sk_stream_memory_free(sk, 0);
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

        static void _sock_tx_timestamp(tcp_sock tp, sockcm_cookie sockc, out byte tx_flags, out uint tskey)
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
                        tskey = (uint)tp.sk_tskey - 1;
                    }
                }
            }

            if (sock_flag(tp, sock_flags.SOCK_WIFI_STATUS))
            {
                tx_flags |= SKBTX_WIFI_STATUS;
            }
        }

        static void sock_tx_timestamp(tcp_sock tp, sockcm_cookie sockc, out byte tx_flags)
        {
            _sock_tx_timestamp(tp, sockc, out tx_flags, out _);
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
            tp.sk_flags |= (uint)(1 << (byte)flag);
        }

        static bool sock_flag(tcp_sock tp, sock_flags flag)
        {
            return BoolOk(tp.sk_flags & (uint)(1 << (byte)flag));
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

            tp.sk_rcvbuf = 1024 * 16;
            tp.sk_sndbuf = 1024 * 16;
            tp.sk_state = TCP_CLOSE;

            sock_set_flag(tp, sock_flags.SOCK_ZAPPED);
            
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
