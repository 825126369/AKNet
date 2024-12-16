namespace AKNet.LinuxTcp
{
    internal class inet_connection_sock : sock
    {
        public uint icsk_user_timeout;//这个成员用于设置一个用户定义的超时值，通常用于控制TCP连接在特定状态下的等待时间。当涉及到长时间未接收到数据或确认的情况时，这个超时值可以用来决定何时关闭连接。
        public uint icsk_rto;
        public int icsk_retransmits;//用于记录发生超时重传的次数
        public icsk_ack icsk_ack;
        public HRTimer icsk_delack_timer;
    }

    internal struct icsk_ack
    {
        public byte pending; //表示是否有待发送的 ACK。
        public byte quick;  //记录计划中的快速 ACK 数量。
        public byte pingpong; //短链接， 指示会话是否被认为是交互式的。当此标志被设置时，TCP 可能会启用乒乓模式（ping-pong mode），以优化交互式流量的处理。
        public byte retry;  //记录尝试发送 ACK 的次数。			   
        public uint ato; //表示当前的 ACK 超时时间（Acknowledgment Timeout），通常用于计算下一次 ACK 应该何时发送。
        public uint lrcv_flowlabel, //记录最近接收到的 IPv6 数据包的流标签（flow label）。

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
    }
}
