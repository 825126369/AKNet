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
    //icsk_mtup 存储了在路径 MTU 发现过程中确定的最大传输单元大小。
    //这有助于确保发送的数据包不会太大，以至于在网络路径上的某个点被分片，从而提高网络效率和可靠性。
    //路径 MTU 发现 (PMTUD):
    //PMTUD 是一种机制，用于动态发现从源到目的地路径上最小的 MTU。
    //通过这个过程，TCP 可以调整其 MSS（最大报文段大小），以避免数据包分片或被丢弃。
    //使用场景:
    //当一个新的 TCP 连接建立时，PMTUD 会尝试发送尽可能大的数据包，并监听 ICMP “需要拆分但 DF 标志设置” 的错误消息。
    //如果收到这样的消息，它会降低估计的路径 MTU 并相应地调整 MSS。
    public class icsk_mtup
    {
        public int search_high; // 定义了搜索范围的上限，即当前连接尝试的最大 MTU 大小。
        public int search_low;//定义了搜索范围的下限，即当前连接尝试的最小 MTU 大小。
        public uint probe_size;//当前正在探测的 MTU 大小。使用31位来存储这个值，允许表示非常大的 MTU 值。
        public bool enabled;//标志位，指示是否启用了 MTUP（Path MTU Discovery）功能。如果此标志为1，则表示该连接启用了路径 MTU 发现；否则未启用。
        public long probe_timestamp;//记录最后一次 MTU 探测的时间戳，通常是以 jiffies 或其他内核时间单位表示。这有助于跟踪探测活动的时间，并确保探测不会过于频繁。
    }

    internal class inet_connection_sock_af_ops
    {
        public ushort net_header_len;
        public ushort sockaddr_len;

        public Func<tcp_sock, sk_buff, flowi, int> queue_xmit;
        public Action<tcp_sock, sk_buff> send_check;
        public Func<tcp_sock, int> rebuild_header;
        public Action<tcp_sock, sk_buff> sk_rx_dst_set;
        public Func<tcp_sock, sk_buff, int> conn_request;
        public Action<tcp_sock> mtu_reduced;
    }

    internal class inet_connection_sock : inet_sock
    {
        public uint icsk_user_timeout;//这个成员用于设置一个用户定义的超时值，通常用于控制TCP连接在特定状态下的等待时间。当涉及到长时间未接收到数据或确认的情况时，这个超时值可以用来决定何时关闭连接。
        public long icsk_rto;
        public long icsk_rto_min;
        public int icsk_retransmits;//用于记录发生超时重传的次数

        //退避计数器:
        //icsk_backoff 记录了当前连接的退避次数。每次发生数据包重传时，TCP 协议会根据这个值来调整 RTO。
        //通常情况下，RTO 会随着退避次数的增加而按指数增长，以避免网络拥塞。
        //指数退避算法:
        //当数据包丢失或没有收到确认（ACK）时，TCP 会等待一段时间后重新发送该数据包。
        //如果再次丢失，则等待更长的时间重新发送，依次类推。
        //这个等待时间的增长是指数级的，直到达到最大值为止。
        //icsk_backoff 就是用来跟踪当前处于哪一次退避。
        public byte icsk_backoff;

        public icsk_ack icsk_ack;
        public TimerList icsk_delack_timer = null;
        public TimerList icsk_retransmit_timer = null;
        public int icsk_pending;

        public long icsk_timeout;
        public byte icsk_ca_state;
        public ushort inet_num;
        public ushort inet_dport;
        public tcp_congestion_ops icsk_ca_ops;
        public inet_connection_sock_af_ops icsk_af_ops;
        public icsk_mtup icsk_mtup;

        public ushort icsk_ext_hdr_len; //用于表示 TCP 段的扩展头部长度

        //它用于跟踪在没有收到确认的情况下发送的探测报文（probe packets）的数量。
        //这些探测报文主要用于检测连接是否仍然活跃，并尝试引发对端的 ACK 响应，特别是在怀疑有数据包丢失或连接处于半打开状态时。
        //icsk_probes_out 记录了已经发送但未被确认的探测报文数量。
        public byte icsk_probes_out = 0;
        //它用于记录最近一次发送探测报文的时间戳
        public long icsk_probes_tstamp = 0;

        public bool icsk_ca_initialized = false;

    }

    internal struct icsk_ack
    {
        public byte pending; //表示是否有待发送的 ACK。
        public byte quick;  //记录计划中的快速 ACK 数量。
        public byte pingpong; //短链接， 指示会话是否被认为是交互式的。当此标志被设置时，TCP 可能会启用乒乓模式（ping-pong mode），以优化交互式流量的处理。
        public byte retry;  //记录尝试发送 ACK 的次数。			   
        public uint ato; //表示当前的 ACK 超时时间（Acknowledgment Timeout），通常用于计算下一次 ACK 应该何时发送。
        public uint lrcv_flowlabel; //记录最近接收到的 IPv6 数据包的流标签（flow label）。

        public uint unused; //目前未使用的字段，可能为未来的扩展保留。
        public long timeout;  //表示当前调度的超时时间。 这个字段记录了下一个 ACK 或其他定时事件应该触发的时间点。
        public uint lrcvtime; //记录最近接收到的数据包的时间戳。这个时间戳可以帮助确定数据包的接收时间和计算延迟。
        public ushort last_seg_size; //记录最近接收到的数据段的大小。这个信息可以用于调整后续 ACK 的行为，例如决定是否需要快速 ACK。
        public ushort rcv_mss;   //表示接收方的最大分段大小（Maximum Segment Size, MSS）。MSS 用于确定每个 TCP 数据段的最大有效载荷大小，影响到延迟 ACK 的决策。
    }

    /*
    快速 ACK 的应用场景
    交互式应用：如 SSH、HTTP 请求、DNS 查询等，其中客户端和服务器之间频繁交换小块数据。快速 ACK 可以确保每个请求都能得到及时处理，从而减少延迟。
    实时通信：如 VoIP、视频会议等，这些应用对延迟非常敏感，快速 ACK 可以帮助保持较低的 RTT，提高通话质量。
    在线游戏：玩家之间的互动通常依赖于频繁的小数据包交换，快速 ACK 可以确保游戏状态的同步性和响应速度。
    */

    /*
    乒乓模式的工作原理
    立即确认：每当接收方接收到一个数据段时，它会立即发送一个 ACK 给发送方，而不等待更多的数据包或定时器到期。这种行为确保了发送方可以尽快知道数据已经成功送达。
    快速响应：发送方在接收到 ACK 后也会尽快发送下一个数据段，形成一种“你发我收，我发你收”的交替模式，类似于乒乓球比赛中的来回击球，因此得名“乒乓模式”。
    减少延迟：通过这种快速的来回确认和发送，乒乓模式可以显著减少往返时间（RTT），这对于需要低延迟的应用非常重要，如远程登录、即时通讯、在线游戏等。
    避免累积延迟：标准的延迟 ACK 模式下，接收方可能会等待一段时间（通常是 200 毫秒）或者等到接收到多个数据段后再发送 ACK。这虽然可以减少 ACK 的数量，但在某些情况下会导致不必要的延迟。乒乓模式通过立即确认避免了这种情况。
    适用于小数据包：乒乓模式特别适合处理频繁的小数据包交换，如 HTTP 请求、DNS 查询等，因为这些应用通常涉及较小的数据量但需要快速响应。
    */

    internal static partial class LinuxTcpFunc
    {
        static void inet_csk_schedule_ack(tcp_sock tp)
        {
	        tp.icsk_ack.pending |= (byte)inet_csk_ack_state_t.ICSK_ACK_SCHED;
        }

        public static bool inet_csk_ack_scheduled(tcp_sock tp)
        {
            return (tp.icsk_ack.pending & (byte)inet_csk_ack_state_t.ICSK_ACK_SCHED) > 0;
        }

        public static bool inet_csk_in_pingpong_mode(tcp_sock tp)
        {
            return tp.icsk_ack.pingpong >= sock_net(tp).ipv4.sysctl_tcp_pingpong_thresh;
        }

        public static void inet_csk_exit_pingpong_mode(tcp_sock tp)
        {
            tp.icsk_ack.pingpong = 0;
        }

        public static void inet_csk_reset_xmit_timer(tcp_sock tp, int what, long when, long max_when)
        {
            if (when > max_when)
            {
                NetLog.Log($"reset_xmit_timer: sk={tp} {what} when={when}, caller={tp}\n");
                when = max_when;
            }

            if (what == tcp_sock.ICSK_TIME_RETRANS || what == tcp_sock.ICSK_TIME_PROBE0 ||
                what == tcp_sock.ICSK_TIME_LOSS_PROBE || what == tcp_sock.ICSK_TIME_REO_TIMEOUT)
            {
                tp.icsk_pending = what;
                tp.icsk_timeout = tcp_jiffies32 + when;
                sk_reset_timer(tp, tp.icsk_retransmit_timer, tp.icsk_timeout);
            }
            else if (what == tcp_sock.ICSK_TIME_DACK)
            {
                tp.icsk_ack.pending = (byte)(tp.icsk_ack.pending | (byte)inet_csk_ack_state_t.ICSK_ACK_TIMER);
                tp.icsk_ack.timeout = tcp_jiffies32 + when;
                sk_reset_timer(tp, tp.icsk_delack_timer, tp.icsk_ack.timeout);
            }
            else
            {
                NetLog.LogError("inet_csk BUG: unknown timer value\n");
            }
        }

        static void inet_csk_clear_xmit_timer(tcp_sock tp, int what)
        {
            if (what == tcp_sock.ICSK_TIME_RETRANS || what == tcp_sock.ICSK_TIME_PROBE0)
            {
                tp.icsk_pending = 0;
                tp.icsk_retransmit_timer.Stop();
            }
            else if (what == tcp_sock.ICSK_TIME_DACK)
            {
                tp.icsk_ack.pending = 0;
                tp.icsk_ack.retry = 0;
                tp.icsk_delack_timer.Stop();
            }
            else
            {
                NetLog.LogError("inet_csk BUG: unknown timer value\n");
            }
        }

        static void inet_csk_clear_xmit_timers(tcp_sock tp)
        {
            tp.icsk_pending = 0;
            tp.icsk_ack.pending = 0;
            sk_stop_timer(tp, tp.icsk_retransmit_timer);
            sk_stop_timer(tp, tp.icsk_delack_timer);
            sk_stop_timer(tp, tp.sk_timer);
        }

        static void inet_csk_inc_pingpong_cnt(tcp_sock tp)
        {
            if (tp.icsk_ack.pingpong < byte.MaxValue)
            {
                tp.icsk_ack.pingpong++;
            }
        }

        static long inet_csk_rto_backoff(tcp_sock tp, long max_when)
        {
            long when = (long)tp.icsk_rto << tp.icsk_backoff;
            return (long)Math.Min(when, max_when);
        }

        static void inet_csk_reset_keepalive_timer(tcp_sock tp, long len)
        {
            sk_reset_timer(tp, tp.sk_timer, tcp_jiffies32 + len);
        }

        static void inet_csk_delete_keepalive_timer(tcp_sock tp)
        {
            sk_stop_timer(tp, tp.sk_timer);
        }

        static void inet_csk_init_xmit_timers(tcp_sock tp, Action<tcp_sock> retransmit_handler,
            Action<tcp_sock> delack_handler, Action<tcp_sock> keepalive_handler)
        {
            tp.icsk_retransmit_timer = new TimerList(1000, retransmit_handler, tp);
            tp.icsk_retransmit_timer.Start();
            tp.icsk_delack_timer = new TimerList(1000, delack_handler, tp);
            tp.icsk_delack_timer.Start();
            tp.sk_timer = new TimerList(1000, keepalive_handler, tp);
            tp.sk_timer.Start();

            tp.icsk_pending = tp.icsk_ack.pending = 0;
        }

    }

}
    