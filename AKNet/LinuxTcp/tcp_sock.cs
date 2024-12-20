/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.LinuxTcp
{
    //enum tsq_enum 是 Linux 内核 TCP 协议栈中用于表示不同类型的延迟（deferred）或节流（throttled）状态的枚举类型
    internal enum tsq_enum
    {
        TSQ_THROTTLED, //表示套接字已被节流（throttled）。当系统资源紧张时，TCP 可能会暂时停止发送数据以减轻负载。
        TSQ_QUEUED,//表示任务已经被排队等待处理。这通常意味着当前没有立即执行该任务的资源或时机，因此它被放入队列中稍后处理。
        TCP_TSQ_DEFERRED,//当 tcp_tasklet_func() 发现套接字正在被其他线程持有（owned by another thread），则将任务推迟到稍后再处理。这种情况可以防止并发访问冲突，并确保数据的一致性。
        TCP_WRITE_TIMER_DEFERRED, //当 tcp_write_timer() 发现套接字正在被其他线程持有，则将写操作推迟。这有助于避免在不适当的时间点进行写操作，从而提高性能和稳定性。
        TCP_DELACK_TIMER_DEFERRED, //当 tcp_delack_timer() 发现套接字正在被其他线程持有，则将延迟确认（delayed acknowledgment）的操作推迟。延迟确认是一种优化技术，通过减少确认的数量来降低网络流量
        TCP_MTU_REDUCED_DEFERRED, //当 tcp_v4_err() 或 tcp_v6_err() 无法立即调用 tcp_v4_mtu_reduced() 或 tcp_v6_mtu_reduced() 来响应 MTU 减少事件时，任务会被推迟。这通常发生在 ICMP 错误消息处理过程中，表明路径 MTU 已经改变。
        TCP_ACK_DEFERRED,  //表示纯确认（pure ACK）的发送被推迟。在某些情况下，为了避免不必要的小包传输，TCP 可能会选择推迟发送仅包含确认信息的数据包。
    }

    internal enum tcp_queue
    {
        TCP_FRAG_IN_WRITE_QUEUE,
        TCP_FRAG_IN_RTX_QUEUE,
    };

    internal class tcp_rack
    {
        public const int TCP_RACK_RECOVERY_THRESH = 16; //这个宏定义设置了一个阈值，用来确定进入恢复模式的条件。具体来说，当连续的丢失数量达到或超过此阈值时，RACK 可能会采取更激进的措施来恢复连接性能。
        public long mstamp; //记录了数据段（skb）被（重新）发送的时间戳
        public long rtt_us;  //关联的往返时间（RTT），以微秒为单位
        public uint end_seq; //数据段的结束序列号
        public uint last_delivered; //上次调整重排序窗口时的 tp->delivered 值。tp->delivered 是一个统计量，表示已成功传递给上层应用的数据量。这有助于评估重排序窗口的有效性。
        public byte reo_wnd_steps;  //允许的重排序窗口大小。重排序窗口定义了在认为数据段丢失之前可以容忍的最大乱序程度。
        public byte reo_wnd_persist; //自上次调整以来进入恢复状态的次数。这是一个位域，占用5位，因此可以表示0到31之间的值。它用于追踪重排序窗口调整后的恢复频率。
        public byte dsack_seen; //标志位，表示自从上次调整重排序窗口后是否看到了 DSACK（选择性确认重复数据段）。DSACK 提供了关于哪些数据段已经被重复确认的信息，这对改进 RACK 的行为非常有用。
        public byte advanced;   //标志位，表示自上次标记丢失以来 mstamp 是否已经前进。如果 mstamp 已经更新，则表明有新的数据段被发送或确认，这对于决定何时进行进一步的丢失检测是重要的.
    }

    internal class tcp_sock:inet_connection_sock
    {
        public const int TCP_INFINITE_SSTHRESH = 0x7fffffff;
        public const ushort TCP_MSS_DEFAULT = 536;
        public const int TCP_INIT_CWND = 10;

        public const ushort HZ = 1000;
        public const long TCP_RTO_MAX = 120 * HZ;
        public const long TCP_RTO_MIN = HZ / 5;
        public const long TCP_TIMEOUT_INIT = 1 * HZ;

        public const int TCP_FASTRETRANS_THRESH = 3;
        public const int sysctl_tcp_comp_sack_slack_ns = 100; //启动一个高分辨率定时器，用于管理TCP累积ACK的发送

        public const int TCP_ATO_MIN = HZ / 25;

        public const int TCP_ECN_OK = 1;
        public const int TCP_ECN_QUEUE_CWR = 2;
        public const int TCP_ECN_DEMAND_CWR = 4;
        public const int TCP_ECN_SEEN = 8;

        public const int TCP_RACK_LOSS_DETECTION = 0x1; //启用 RACK 来检测丢失的数据包。
        public const int TCP_RACK_STATIC_REO_WND = 0x2; //使用静态的 RACK 重排序窗口
        public const int TCP_RACK_NO_DUPTHRESH = 0x4; //在 RACK 中不使用重复确认（DUPACK）阈值。

        public const byte TCPHDR_FIN = 0x01;
        public const byte TCPHDR_SYN = 0x02;
        public const byte TCPHDR_RST = 0x04;
        public const byte TCPHDR_PSH = 0x08;
        public const byte TCPHDR_ACK = 0x10;
        public const byte TCPHDR_URG = 0x20;
        public const byte TCPHDR_ECE = 0x40;
        public const byte TCPHDR_CWR = 0x80;

        public const byte TCPHDR_SYN_ECN = (TCPHDR_SYN | TCPHDR_ECE | TCPHDR_CWR);

        /* TCP thin-stream limits */
        public const byte TCP_THIN_LINEAR_RETRIES = 6;       /* After 6 linear retries, do exp. backoff */

        //sk_wmem_queued 是 Linux 内核中 struct sock（套接字结构体）的一个成员变量，用于跟踪已排队但尚未发送的数据量。
        //这个计数器对于管理 TCP 连接的发送窗口和控制内存使用非常重要。
        //它帮助内核确保不会过度占用系统资源，并且能够有效地处理拥塞控制和流量控制。
        public int sk_wmem_queued;
        public int sk_forward_alloc;//这个字段主要用于跟踪当前套接字还可以分配多少额外的内存来存储数据包
        public uint max_window;//

        //这个字段用于跟踪已经通过套接字发送给应用层的数据序列号（sequence number）。具体来说，pushed_seq 表示最近一次调用 tcp_push() 或类似函数后，
        //TCP 层认为应该被“推送”到网络上的数据的最后一个字节的序列号加一。
        public uint pushed_seq;
        public uint write_seq;  //应用程序通过 send() 或 write() 系统调用写入到TCP套接字中的最后一个字节的序列号。
        public uint rtt_seq;
        public uint snd_nxt;    //Tcp层 下一个将要发送的数据段的第一个字节的序列号。
        public uint snd_una;//表示未被确认的数据段的第一个字节的序列号。
        public uint mss_cache;  //单个数据包的最大大小

        public uint snd_wnd;    //发送窗口的大小
        public uint snd_cwnd;   //拥塞窗口的大小, 表示当前允许发送方发送的最大数据量（以字节为单位)
        public uint copied_seq; //记录了应用程序已经从接收缓冲区读取的数据的最后一个字节的序列号（seq）加一，即下一个期待被用户空间读取的数据的起始序列号

        public uint snd_cwnd_cnt;	/* Linear increase counter		*/
        public long snd_cwnd_stamp; //通常用于 TCP 拥塞控制算法中，作为时间戳来记录某个特定事件的发生时刻。具体来说，它可以用来标记拥塞窗口 (snd_cwnd) 最后一次改变的时间

        //用于记录当前在网络中飞行的数据包数量。这些数据包已经发送出去但还未收到确认（ACK）
        public uint packets_out;  //记录已经发送但还没有收到 ACK 确认的数据包数量。这对于 TCP 拥塞控制算法（如 Reno、Cubic）以及重传逻辑至关重要。
        public uint sacked_out;//表示已经被选择性确认SACK的数据包数量。
        public uint lost_out; // 表示被认为已经丢失的数据包数量
        public uint app_limited;

        //用于记录已经成功传递给应用程序的数据包总数。这个字段包括了所有已传递的数据包，即使这些数据包可能因为重传而被多次传递。
        public uint delivered;

        public ushort gso_segs; //它用于表示通过 Generic Segmentation Offload(GSO) 分段的数据包的数量。GSO 是一种优化技术，允许操作系统将大的数据包交给网卡，然后由网卡硬件负责将这些大包分段成适合底层网络传输的小包

        public long srtt_us; //表示平滑后的往返时间，单位为微秒。
        public long rttvar_us;//表示往返时间变化的估计值，也称为均方差（mean deviation），单位为微秒。用来衡量RTT测量值的变化程度，帮助调整RTO以适应网络条件的变化。
        public long mdev_us;//是RTT变化的一个估计值，类似于rttvar_us，但可能有不同的更新逻辑或用途。
        public long mdev_max_us;//跟踪最大均方差，即mdev_us的最大值。可能用于调试目的或者特定的算法需求，比如设置RTO的上限。

        public bool tcp_usec_ts; //通常指的是在TCP（传输控制协议）中启用微秒级的时间戳选项
        public long tcp_mstamp;
        public long retrans_stamp; //重传时间时间戳
        public long rto_stamp;//时间戳记录：每当触发一次 RTO 事件时，rto_stamp 会被设置为当前的时间戳。这有助于后续计算从 RTO 触发到恢复完成所花费的时间。
        public ushort total_rto;	// Total number of RTO timeouts, including
        public long lsndtime;//上次发送的数据包的时间戳, 用于重启窗口
        
        public long chrono_start;
        public tcp_chrono chrono_type;
        public long[] chrono_stat = new long[3];

        public TCP_STATE sk_state;
        public ushort timeout_rehash;	/* Timeout-triggered rehash attempts */
        public byte compressed_ack;

        public ushort total_rto_recoveries;// Linux 内核 TCP 协议栈中的一个统计计数器，用于跟踪由于重传超时（RTO, Retransmission Timeout）而触发的恢复操作次数
        public long total_rto_time;

        public uint rcv_nxt;//用于表示接收方下一个期望接收到的字节序号

        public HRTimer compressed_ack_timer;
        public long rcv_tstamp;

        /*
         * high_seq 是 Linux 内核 TCP 协议栈中的一个重要变量，用于跟踪 TCP 连接中某些特定序列号的边界。具体来说，high_seq 通常用来表示在快速重传（Fast Retransmit）或拥塞控制算法中的一些关键序列号位置。
         * 然而，在不同的上下文中，high_seq 的确切含义和用途可能会有所不同。
         * high_seq 在 TCP 协议栈中的作用:
             快速重传：在快速重传算法中，high_seq 可以被用来记录最近一次发送的最大序列号。当收到三个重复的 ACK（即同一个序列号的 ACK 出现三次），TCP 协议会认为丢失了一个数据包，并触发快速重传机制。
                此时，high_seq 帮助确定哪些数据包需要被重传。
             拥塞控制：在拥塞控制算法中，如 Reno 或 CUBIC，high_seq 也可以用来标记连接中某个重要的序列号点，例如最后一次窗口完全打开时的最高序列号。这有助于算法根据网络状况调整发送窗口大小，避免过度拥塞。
            SACK（选择性确认）支持：对于支持 SACK 的 TCP 实现，high_seq 可能用于跟踪已经发送但未被确认的数据块的上界，以便更精确地管理哪些部分的数据需要重传。
            其他用途：在某些情况下，high_seq 也可能用于其他与 TCP 状态跟踪相关的功能，具体取决于内核版本和实现细节。
         * */
        public uint high_seq;	/* snd_nxt at onset of congestion	*/
        public uint snd_ssthresh;

        //// 是 Linux 内核 TCP 协议栈中用于拥塞控制的一个重要变量。
        ///它记录了在检测到网络拥塞（例如通过丢包或重复 ACK）之前，慢启动阈值（slow start threshold, ssthresh）的值。
        ///这个变量主要用于实现快速恢复（Fast Recovery）算法和帮助 TCP 连接从拥塞事件中更快地恢复。
        public uint prior_ssthresh;
        public uint prior_cwnd; //它通常指的是在某些特定事件发生之前的拥塞窗口（Congestion Window, cwnd）大小

        //描述：表示在新的恢复阶段（recovery episode）开始时的 snd_una 值（发送方未确认的数据包序列号）。
        //当进入一个新的恢复阶段时，undo_marker 会被设置为当前的 snd_una，这有助于确定哪些数据包是在恢复阶段之前发送的。
        //用途：
        //回滚支持：如果后续发现某些重传是不必要的（例如，因为延迟的 ACK 最终到达），TCP 可以使用 undo_marker 来回滚到恢复阶段之前的状态，从而避免不必要地减小拥塞窗口（CWND）。
        //拥塞控制调整：通过比较当前的 snd_una 和 undo_marker，可以判断是否应该撤销之前的拥塞控制决策。
        public uint undo_marker;
        //描述：表示可撤销（undoable）的重传次数。这个计数器记录了在当前恢复阶段内发生的、可能被撤销的重传数量。
        //用途：
        //追踪重传：帮助 TCP 跟踪哪些重传是可以撤销的，以便在接收到延迟的 ACK 或其他证据表明这些重传是不必要的时，能够正确地调整状态。
        //优化性能：通过允许撤销不必要的重传，TCP可以更智能地管理其发送速率，减少因误判导致的性能下降。
        public int undo_retrans;

        //描述：表示当前在网络中尚未被确认的重传数据包的数量。每当一个数据包被重传时，retrans_out 会增加；当接收到对这些重传数据包的确认（ACK）时，retrans_out 会减少。
        //用途：
        //拥塞控制：帮助 TCP 检测和响应网络状况的变化。例如，如果 retrans_out 数量增加，可能表明网络中存在丢包或拥塞，TCP 可以据此调整其发送速率和拥塞窗口（CWND）。
        //快速恢复：在快速恢复算法中，retrans_out 用于确定是否有未确认的重传数据包，并根据 ACK 反馈调整状态。
        //性能监控：通过监控 retrans_out 的变化，可以评估 TCP 连接的健康状况和性能，及时发现潜在的问题。
        public uint retrans_out;

        //是 Linux 内核 TCP 协议栈中用于管理 Tail Loss Probe (TLP) 机制的字段
        //TLP 是一种旨在更快速地检测和恢复尾部丢失（即连接末端的数据包丢失）的技术，它有助于减少不必要的延迟并提高传输效率。

        //表示在触发 TLP 时的 snd_nxt 值，即发送方下一个预期发送的数据包序列号。当 TLP 被触发时，这个值会被记录下来，以便后续评估 TLP 的效果。
        //用途：
        //跟踪 TLP 发送点：通过记录 snd_nxt 在 TLP 触发时的值，可以确定哪些数据包是在 TLP 触发之后发送的，从而更好地评估网络反馈。
        //确认 TLP 效果：如果接收到的 ACK 确认了比 tlp_high_seq 更高的序列号，说明 TLP 成功触发了新的 ACK，并可能揭示了之前未被发现的丢包
        public uint tlp_high_seq;   /* snd_nxt at the time of TLP */
        //指示 TLP 是否是一次重传操作。如果是重传，则该标志位会被设置为真（1 或 true），否则为假（0 或 false）。
        //用途：
        //区分 TLP 类型：帮助区分 TLP 是否是基于新数据还是重传旧数据包。这对于拥塞控制和恢复算法非常重要，因为不同类型的 TLP 可能需要不同的处理逻辑。
        //优化性能：通过了解 TLP 是否涉及重传，TCP 可以更智能地调整其行为，例如避免不必要的拥塞窗口减小。
        public byte tlp_retrans;    /* TLP is a retransmission */

        //这个变量用于表示当前连接对乱序包（out-of-order packets）的容忍度，即最大允许的数据段重排序数。
        //作用
        //乱序容忍度：当接收到的数据包不是按照发送顺序到达时，TCP 协议栈不会立即认为这些包是丢失的，而是等待一段时间看看是否能收到后续的包来填补空缺。
        //tp->reordering 就定义了在这种情况下可以接受的最大乱序程度。
        //快速重传触发：如果乱序超过了 tp->reordering 的值，TCP 可能会认为有数据包丢失，并触发快速重传机制以尽快恢复丢失的数据。
        public uint reordering;
        public byte ecn_flags;	/* ECN status bits.			*/
        public byte frto; /* F-RTO (RFC5682) activated in CA_Loss */
        public byte is_sack_reneg;    /* in recovery from loss with SACK reneg? */

        public tcp_options_received rx_opt;
        public tcp_rack rack;

        //这两个指针主要用于优化 TCP 丢失检测和重传机制。
        //用途: 这个指针通常用来标记或指示最近被认为丢失的数据包的 sk_buff。
        //它有助于快速定位可能需要进行 SACK 或 RACK 算法处理的数据段。
        //应用场景: 当 TCP 协议栈检测到数据包丢失时，它会使用这个指针来加快对丢失数据包的处理过程，比如决定哪些数据包需要被重传。
        //通过记住最后一个已知丢失的数据包的位置，可以减少遍历整个发送队列以查找丢失数据包所需的时间。
        public sk_buff lost_skb_hint;

        //用途: 这个指针指向最近一次尝试重传的数据包的 sk_buff。它帮助内核跟踪哪些数据包已经被重传，并且在某些情况下，可以帮助决定是否需要进一步重传其他数据包。
        //应用场景: 在执行快速重传或其他类型的重传策略时，retransmit_skb_hint 可以用来提高效率。
        //例如，当接收到 SACK 信息时，TCP 协议栈可以根据 retransmit_skb_hint 快速找到并评估哪些数据包还需要再次重传，而不需要重新扫描整个发送队列
        public sk_buff retransmit_skb_hint;
        public uint lost;//Linux 内核 TCP 协议栈中用于统计 TCP 连接上丢失的数据包总数的成员变量。

        public byte thin_lto; /* Use linear timeouts for thin streams */
    }


    internal static partial class LinuxTcpFunc
    {
        public static long tcp_time_stamp_ms(tcp_sock tp)
        {
            return tp.tcp_mstamp / 1000;
        }

        public static long tcp_time_stamp_ts(tcp_sock tp)
        {
            if (tp.tcp_usec_ts)
            {
                return tp.tcp_mstamp;
            }
            return tcp_time_stamp_ms(tp);
        }

    static void tcp_rtt_estimator(tcp_sock tp, long mrtt_us)
        {
            long m = mrtt_us;
            long srtt = tp.srtt_us;

            static bool after(uint seq1, uint seq2)
            {
                return (Int32)(seq1) - seq2 > 0;
            }
            static long tcp_rto_min_us()
            {
                return tcp_sock.TCP_RTO_MIN;
            }

            if (srtt != 0)
            {
                m -= (srtt >> 3);
                srtt += m;
                if (m < 0)
                {
                    m = -m;
                    m -= (tp.mdev_us >> 2);
                    if (m > 0)
                    {
                        m >>= 3;
                    }
                }
                else
                {
                    m -= (tp.mdev_us >> 2);
                }

                tp.mdev_us += m;
                if (tp.mdev_us > tp.mdev_max_us)
                {
                    tp.mdev_max_us = tp.mdev_us;
                    if (tp.mdev_max_us > tp.rttvar_us)
                    {
                        tp.rttvar_us = tp.mdev_max_us;
                    }
                }
                if (after(tp.snd_una, tp.rtt_seq))
                {
                    if (tp.mdev_max_us < tp.rttvar_us)
                    {
                        tp.rttvar_us -= (tp.rttvar_us - tp.mdev_max_us) >> 2;
                    }
                    tp.rtt_seq = tp.snd_nxt;
                    tp.mdev_max_us = tcp_rto_min_us();
                }
            }
            else
            {
                srtt = m << 3;
                tp.mdev_us = m << 1;
                tp.rttvar_us = Math.Max(tp.mdev_us, tcp_rto_min_us());
                tp.mdev_max_us = tp.rttvar_us;
                tp.rtt_seq = tp.snd_nxt;
            }
            tp.srtt_us = Math.Max(1U, srtt);
        }

        static void tcp_set_rto(tcp_sock tp)
        {
            long icsk_rto = 0;
            icsk_rto = (tp.srtt_us >> 3) + tp.rttvar_us;

            if (icsk_rto > tcp_sock.TCP_RTO_MAX)
            {
                icsk_rto = tcp_sock.TCP_RTO_MAX;
            }
        }
    }
}
