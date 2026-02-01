/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp.Common
{
    internal class netns_ipv4
    {
        public byte sysctl_tcp_ecn;
        public int sysctl_tcp_retries1; //默认最大重传次数
        public int sysctl_tcp_retries2;

        //当 sysctl_tcp_pingpong_thresh 设置为非零值时，内核会在每个时间窗口（通常是几百毫秒）内统计接收到的数据包数量。
        //如果在这个时间窗口内接收到的数据包数量超过了设定的阈值，内核可能会认为这是一个交互式的连接，并启用乒乓模式来优化 ACK 和数据传输行为。
        public int sysctl_tcp_pingpong_thresh = 1;

        ////是一个内核参数，用于设置 TCP 协议栈对乱序包（out-of-order packets）的容忍度。
        ///具体来说，它定义了在没有收到确认的情况下，TCP 可以接受的最大段重排序数。 <summary>
        /// 具体来说，它定义了在没有收到确认的情况下，TCP 可以接受的最大段重排序数。
        //这个值影响了 TCP 如何快速地检测丢包并触发快速重传。
        public int sysctl_tcp_reordering;

        //0：表示禁用F-RTO。
        //1：表示启用F-RTO，并且它是默认选项。
        //2：表示仅当接收到至少一个重复ACK时才启用F-RTO。
        public byte sysctl_tcp_frto; //

        //sysctl_tcp_recovery 是 Linux 内核中的一个参数，它与 TCP 的拥塞控制和恢复机制有关。
        //具体来说，这个参数控制了一些高级的 TCP 恢复特性，如 RACK（Reordering-Aware Packet Loss Detection, 重组感知的丢包检测）和 Forward RTO-Recovery(FRR)。
        public byte sysctl_tcp_recovery;

        //启用 ECN 回退:
        //当 sysctl_tcp_ecn_fallback 设置为 1 时，
        //如果 TCP 连接尝试使用 ECN 但检测到路径上的某个设备不支持 ECN 或者发生了其他问题导致 ECN 标记被清除，
        //则该连接会自动回退到传统的拥塞控制机制（如 Reno 或 Cubic），以确保连接能够继续正常工作。
        //禁用 ECN 回退:
        //如果 sysctl_tcp_ecn_fallback 设置为 0，
        //则即使检测到 ECN 不被支持或出现问题，TCP 连接也不会回退到传统机制，而是继续尝试使用 ECN。
        //这可能导致在某些情况下性能下降或连接问题，但如果路径确实支持 ECN，则可以获得更好的性能。
        public byte sysctl_tcp_ecn_fallback;

        //sysctl_tcp_thin_linear_timeouts 是 Linux 内核中的一个系统控制（sysctl）变量，
        //用于配置 TCP 协议栈中针对“thin streams”（即低流量或小数据量的 TCP 连接）的超时行为。
        //具体来说，这个变量决定了这些连接是否使用线性超时机制而不是传统的指数退避算法。
        //0 (默认):禁用线性超时机制，使用传统的指数退避算法。这是大多数高流量连接的标准行为。
        //1:启用线性超时机制，适用于低流量或小数据量的连接。每次重传超时后，超时时间以固定增量增加，而不是按指数增长。
        public byte sysctl_tcp_thin_linear_timeouts;

        //0 (默认):禁用线性超时机制，使用传统的指数退避算法。这是大多数情况下的默认行为。
        //1:启用线性超时机制，适用于 SYN 数据包的重传。每次重传 SYN 数据包后，超时时间以固定增量增加，而不是按指数增长。
        public byte sysctl_tcp_syn_linear_timeouts;

        //sysctl_tcp_retrans_collapse 是 Linux 内核中的一个 TCP 参数，
        //用于控制在重传队列中是否尝试合并（collapse）多个小的数据包成一个较大的数据包。
        //这个特性旨在减少网络拥塞和提高传输效率，特别是在处理大量小数据包的情况下。
        //当启用 tcp_retrans_collapse（设置为 1）时，TCP 协议栈会在重传队列中尝试将多个小的数据包合并成一个较大的数据包进行重传。
        //这可以减少网络上的分片数量，从而可能降低网络拥塞并提高传输效率。
        //然而，这也可能导致一些额外的延迟，因为内核需要额外的时间来合并数据包。
        public byte sysctl_tcp_retrans_collapse;
        //它允许内核在某些情况下缩小 TCP 接收窗口，以避免内存使用过多
        public byte sysctl_tcp_shrink_window;

        public byte sysctl_tcp_min_tso_segs;
        //sysctl_tcp_tso_rtt_log 是一个Linux内核参数，它用于控制TSO（TCP Segmentation Offload）机制下的RTT（Round-Trip Time，往返时间）测量精度。
        //具体来说，这个参数决定了在启用TSO的情况下，内核用来计算最小RTT的时间单位的对数形式。
        //默认情况下，该值被设置为9，这意味着最小RTT是以512微秒（即 微秒）作为基本单位来衡量的14。
        public byte sysctl_tcp_tso_rtt_log;
        public int sysctl_tcp_limit_output_bytes;

        public int sysctl_tcp_min_snd_mss;

        //sysctl_tcp_probe_threshold 设置了一个阈值，表示在当前拥塞窗口中未被确认的数据包数量达到多少时，TCP 应该开始考虑发送探测报文。具体来说：
        //如果从最后一次接收到 ACK 后发送出去的数据包数量达到了 sysctl_tcp_probe_threshold，并且这些数据包还没有被确认，那么 TCP 将考虑发送一个探测报文。
        //这个参数帮助平衡探测的频率和网络资源的使用。
        //较小的值可能导致更频繁的探测，从而更快地响应潜在的丢失，但也可能增加不必要的流量；
        //较大的值则相反，可能会延迟对丢失的反应。
        public int sysctl_tcp_probe_threshold;

        //sysctl_tcp_probe_interval 是 Linux 内核中与 TCP 探测机制相关的一个参数，
        //它定义了在发送 Tail Loss Probe (TLP) 或 Retransmission Timeout (RTO) probe 时的最小时间间隔。
        //这个参数确保了探针报文不会过于频繁地被发送，从而避免对网络造成不必要的负载，并防止潜在的拥塞加剧。
        public uint sysctl_tcp_probe_interval;

        //sysctl_tcp_orphan_retries 是 Linux 内核中的一个参数，用于控制当 TCP 连接成为孤儿（即连接的套接字没有对应的用户进程）时，
        //内核尝试重试发送数据或确认信息的最大次数。这个参数对于管理系统的资源非常重要，因为它直接影响到系统处理无响应或异常终止的 TCP 连接的方式。
        public byte sysctl_tcp_orphan_retries;

        //sysctl_tcp_keepalive_time 具体指定了在没有任何数据传输的情况下，TCP 连接保持空闲多长时间后开始发送 keepalive 探测报文。
        //这个时间间隔是以秒为单位的，默认值通常是 7200 秒（即 2 小时）。
        public long sysctl_tcp_keepalive_time;

        public byte sysctl_tcp_keepalive_probes;

        //定义了在没有收到对方确认时，重新发送保活探测包的时间间隔
        public int sysctl_tcp_keepalive_intvl;

        //在启用 TSO 时，TCP 窗口大小如何被调整以确定每个 TSO 数据包的大小。具体来说，它指定了计算 TSO 数据包大小时使用的除数
        public byte sysctl_tcp_tso_win_divisor;

        public bool sysctl_tcp_slow_start_after_idle;

        //用于配置 TCP（传输控制协议）的早期重传机制。早期重传（Early Retransmit）是一种优化措施，旨在更快地检测和重传丢失的数据包，
        //从而提高网络性能，尤其是在高延迟或高丢包率的环境中。
        public byte sysctl_tcp_early_retrans;

        //sysctl_tcp_autocorking 是 Linux 内核中的一个参数，用于控制 TCP 自动软木塞（autocorking）机制的行为。
        //TCP 软木塞是一种优化技术，旨在减少网络流量中的小数据包数量，从而提高效率并降低延迟。
        //自动软木塞则是在特定条件下自动启用和禁用这种行为的机制。
        //主要功能
        //减少小数据包：当启用了自动软木塞时，内核会在某些情况下暂时阻止小数据包的立即发送，而是尝试将多个小的数据块合并成更大的数据包再一起发送出去。
        //这有助于减少网络上的数据包数量，进而可能改善整体网络性能。
        //智能触发：自动软木塞不是一直生效的，而是在检测到连续的小写操作或者在一定时间内有多个小数据块待发送的情况下才会激活。
        //这样可以避免不必要的延迟，同时仍然获得软木塞带来的好处。
        //提升应用性能：对于一些应用程序来说，特别是那些频繁发送少量数据的应用程序，如 HTTP/HTTPS 服务器、数据库客户端等，自动软木塞可以帮助提高吞吐量并减少延迟。
        //默认开启：在较新的 Linux 内核版本中，默认情况下 tcp_autocorking 是启用的。
        //这是因为大多数现代应用都能从这项优化中受益，尤其是在高带宽低延迟的网络环境中。
        public byte sysctl_tcp_autocorking;

        //sysctl_tcp_moderate_rcvbuf 是一个 Linux 内核参数，用于控制是否启用 TCP 接收缓冲区自动调整功能。
        //当这个选项被启用时，内核会根据网络条件和连接的 RTT（往返时间）动态调整每个 TCP 连接的接收缓冲区大小。
        //这有助于优化网络性能，尤其是在高带宽和高延迟的环境中。
        public byte sysctl_tcp_moderate_rcvbuf;

        //参数的作用
        //窗口长度：tcp_min_rtt_wlen 定义了一个滑动窗口的时间长度，在这个时间段内收集到的所有 RTT 样本都会被用来计算最小 RTT。
        //最小 RTT 计算：通过限制用于计算最小 RTT 的样本数量，可以确保估计值反映的是最近一段时间内的网络状况，而不是历史数据。
        //这对于快速适应网络条件的变化非常有用。
        //默认值：默认情况下，tcp_min_rtt_wlen 的值通常设置为 300 秒（5 分钟），但这可以根据具体需求进行调整
        public int sysctl_tcp_min_rtt_wlen;

        //sysctl_tcp_mtu_probing 是 Linux 内核中的一个参数，
        //用于控制 TCP 的路径 MTU（Maximum Transmission Unit）发现机制，即 TCP MTU Probing。
        //这项技术旨在动态地确定从发送方到接收方之间的网络路径上能够传输的最大数据包大小，从而优化数据传输效率并避免分片.
        //0：禁用 TCP MTU Probing。
        //1：仅对新建立的连接启用 MTU 探测。
        //2：对所有连接启用 MTU 探测，包括已存在的连接。
        public byte sysctl_tcp_mtu_probing;

        //定义了 MTU 探测的最小值
        public int sysctl_tcp_mtu_probe_floor;

        //sysctl_tcp_base_mss 是 Linux 内核中的一个参数，用于设置 TCP 连接的基本最大段大小（Maximum Segment Size, MSS）。
        //MSS 定义了 TCP 段中数据部分的最大长度，它直接影响到每个数据包的有效载荷大小。
        //合理配置 MSS 对于优化网络性能、避免分片以及确保数据传输的效率至关重要。
        //参数的作用
        //默认 MSS：tcp_base_mss 设置了一个基础值，内核会基于这个值来计算实际使用的 MSS。
        //这个值通常在建立新连接时作为起点，并根据路径 MTU 发现（PMTUD）或其他机制进行调整。
        //避免分片：通过适当地设置 MSS，可以确保数据包不会超过路径上的最小 MTU，从而避免在网络层发生分片，减少传输延迟和提高整体性能。
        //适应不同网络环境：不同的网络链路可能有不同的 MTU 限制。
        //通过设置合适的 tcp_base_mss，可以在各种网络环境中实现最佳的数据传输效率。
        //默认情况下，tcp_base_mss 的值通常是 536 字节，这是考虑到以太网 MTU（1500 字节）减去 IP 和 TCP 头部的大小后的一个安全值。
        //然而，具体数值可能会因内核版本或发行版的不同而有所差异。
        public int sysctl_tcp_base_mss;

        //sysctl_tcp_no_ssthresh_metrics_save 是 Linux 内核中的一个参数，用于控制 TCP 是否保存慢启动阈值（Slow Start Threshold, ssthresh）的度量信息。
        //这个参数影响了 TCP 拥塞控制算法的行为，特别是在连接关闭或重新打开时如何处理拥塞窗口（Congestion Window, cwnd）和慢启动阈值。
        //参数的作用
        //保存度量信息：当 tcp_no_ssthresh_metrics_save 设置为 0 时，内核会在连接关闭时保存当前的慢启动阈值和其他拥塞控制相关的度量信息。
        //这些信息可以在后续重新建立连接时被恢复，以帮助更快地适应之前的网络条件。
        //不保存度量信息：当设置为 1 时，内核不会保存这些度量信息。
        //每次连接重新建立时，TCP 拥塞控制将从默认状态开始，即使用初始的慢启动阈值和拥塞窗口。
        public byte sysctl_tcp_no_ssthresh_metrics_save;

        public byte sysctl_tcp_nometrics_save;

        //sysctl_tcp_app_win 是 Linux 内核中的一个参数，用于控制 TCP 协议栈如何处理应用程序窗口（Application Window）。
        //这个参数影响了内核在计算接收窗口（Receive Window, rwnd）时考虑的应用程序缓冲区大小。
        //具体来说，它定义了在计算接收窗口时，TCP 协议栈应该为应用程序保留的额外空间量。
        //参数的作用
        //应用层窗口：tcp_app_win 设置了一个因子，用以确定内核在报告接收窗口大小时为应用程序预留的空间。
        //这有助于确保应用程序有足够的缓冲区来存储接收到的数据，而不会因为接收窗口过小而导致数据包被丢弃或连接性能下降。
        //默认行为：当 tcp_app_win 设置为 0 时，表示不为应用程序保留额外空间，接收窗口将根据实际可用的接收缓冲区大小进行调整。
        //非零值：当设置为非零值时，tcp_app_win 定义了一个比例因子，用于增加接收窗口大小，以便为应用程序提供更多的缓冲空间。
        //例如，如果设置为 1，则接收窗口可能会比实际可用的接收缓冲区大一些，从而允许应用程序有更多时间处理接收到的数据。
        //默认情况下，tcp_app_win 的值通常是 0，这意味着不为应用程序预留额外的接收窗口空间。
        public byte sysctl_tcp_app_win;

        //sysctl_tcp_wmem 是 Linux 内核中的一个参数，用于配置 TCP 发送缓冲区（send buffer）的内存分配策略。
        //这个参数定义了每个 TCP 连接在发送数据时可以使用的最小、默认和最大内存大小。
        //合理配置 tcp_wmem 可以显著影响网络性能，特别是在高带宽或高延迟的环境中。
        //tcp_wmem 是一个由三个整数值组成的数组，分别表示：
        //最小值：每个 TCP 连接的发送缓冲区的最小大小（单位为字节）。即使系统资源紧张，每个连接至少会获得这么多的缓冲空间。
        //默认值：新创建的 TCP 连接的发送缓冲区的初始大小。这是大多数连接将使用的默认值。
        //最大值：每个 TCP 连接的发送缓冲区的最大大小。超过这个限制，TCP 将不再增加发送缓冲区的大小。
        //这些值直接影响到 TCP 协议栈如何管理发送窗口（Send Window），进而影响到数据传输的效率和吞吐量。
        //默认情况下，tcp_wmem 的值通常是[4096, 16384, 4194304]，但具体的默认值可能会因内核版本和发行版的不同而有所差异。
        public int[] sysctl_tcp_wmem = new int[3];

        //sysctl_tcp_pacing_ss_ratio 是 Linux 内核中的一个参数，
        //用于控制 TCP 发送方在慢启动（Slow Start）阶段的流量整形行为。
        //具体来说，它影响了内核如何调整发送速率以优化网络性能和减少拥塞。
        //慢启动阶段：TCP 慢启动是一种算法，旨在快速增加发送方的拥塞窗口（cwnd），直到检测到网络拥塞为止。
        //在此期间，发送速率会逐渐提高，以便更好地利用可用带宽。
        //流量整形：为了防止发送速率增长过快而导致网络拥塞或丢包，
        //Linux 内核可以通过 tcp_pacing_ss_ratio 来限制发送速率的增长速度。
        //这个参数定义了在慢启动阶段应用的流量整形比例。
        //tcp_pacing_ss_ratio 的值是一个百分比，表示相对于当前拥塞窗口大小的发送速率
        public int sysctl_tcp_pacing_ss_ratio;

        //sysctl_tcp_pacing_ca_ratio 是 Linux 内核中的一个参数，
        //用于控制 TCP 发送方在拥塞避免（Congestion Avoidance, CA）阶段的流量整形行为。
        //具体来说，它影响了内核如何调整发送速率以优化网络性能和减少拥塞，特别是在拥塞避免阶段。
        //拥塞避免阶段：TCP 拥塞避免是一种算法，在检测到网络拥塞后，旨在缓慢增加发送方的拥塞窗口（cwnd），
        //以避免进一步的拥塞。在此期间，发送速率会以线性方式逐渐增加。
        //流量整形：为了防止发送速率增长过快而导致网络拥塞或丢包，
        //Linux 内核可以通过 tcp_pacing_ca_ratio 来限制发送速率的增长速度。
        //这个参数定义了在拥塞避免阶段应用的流量整形比例。
        //tcp_pacing_ca_ratio 的值是一个百分比，表示相对于当前拥塞窗口大小的发送速率
        public int sysctl_tcp_pacing_ca_ratio;

        public int[] sysctl_tcp_rmem = new int[3];

        //sysctl_tcp_dsack 是一个 Linux 内核参数，用于控制是否启用 TCP 的 DSACK（Duplicate SACK）特性。
        //DSACK 是 SACK（Selective Acknowledgment）的扩展，允许接收方在 ACK 中包含重复接收的数据包信息
        //启用 DSACK：当启用 DSACK 时，TCP 可以在 ACK 中包含重复接收的数据包信息。
        //这有助于发送方更好地了解哪些数据包被重复接收，从而优化重传策略和拥塞控制
        //性能优化：DSACK 可以帮助发送方判断是数据包丢失还是 ACK 丢失，从而避免不必要的重传
        //默认值为 1：表示 DSACK 特性默认是开启的
        public byte sysctl_tcp_dsack;

        //sysctl_tcp_invalid_ratelimit 是 Linux 内核中的一个参数，用于控制对无效 TCP 数据包的响应频率。
        //当接收到诸如具有错误校验和、序列号超出预期范围等不符合TCP协议规范的数据包时，这些数据包通常被认为是无效的。
        //为了防止恶意攻击者利用大量无效的TCP数据包来耗尽系统资源（例如进行DoS攻击），内核会对响应这些无效数据包的行为进行限制。
        //具体来说，sysctl_tcp_invalid_ratelimit 参数定义了在一定时间间隔内可以发送多少个关于无效TCP数据包的通知或日志记录。
        //这有助于减少因处理无效数据包而产生的负载，并且可以保护系统免受某些类型的拒绝服务攻击
        public int sysctl_tcp_invalid_ratelimit;

        public int sysctl_tcp_challenge_ack_limit;
        public long tcp_challenge_timestamp;
        public uint tcp_challenge_count;

        public int sysctl_tcp_max_reordering;

        //sysctl_tcp_backlog_ack_defer 是一个 Linux 内核参数，用于控制 TCP 发送端在处理套接字 backlog 时是否延迟发送 ACK。
        //其主要目的是减少在处理大量数据时的延迟，提高性能
        public byte sysctl_tcp_backlog_ack_defer;

        //sysctl_tcp_comp_sack_nr 是一个 Linux 内核参数，用于控制 TCP 协议栈中 SACK（Selective Acknowledgment）报文的压缩行为。
        //具体来说，它允许设置被压缩的最大 SACK 报文数。如果设置为 0，则关闭 SACK 压缩功能。
        //功能
        //SACK 压缩：当设置为非零值时，TCP 协议栈会尝试将多个 SACK 块合并为更少的 SACK 块，以减少 SACK 报文的数量，从而减少网络流量和处理开销。
        //性能优化：通过减少 SACK 报文的数量，可以提高网络传输的效率，特别是在高延迟或高丢包率的网络环境中。
        public byte sysctl_tcp_comp_sack_nr;
        public long sysctl_tcp_comp_sack_delay_ns;

        public byte sysctl_tcp_window_scaling;
        public byte sysctl_tcp_timestamps;
        public byte sysctl_tcp_sack;

        public long sysctl_tcp_rto_min_us;//用于设置 TCP 重传超时（RTO）的最小值，单位为微秒（us)

        public tcp_congestion_ops tcp_congestion_control;
        public byte sysctl_tcp_syn_retries;
        public byte sysctl_tcp_synack_retries;
        public byte sysctl_tcp_syncookies;
        public int sysctl_tcp_fin_timeout;
        public uint sysctl_tcp_notsent_lowat;
        public byte sysctl_tcp_tw_reuse;

        public uint ip_rt_min_pmtu;
        public long ip_rt_mtu_expires;
        public int ip_rt_min_advmss;
    }
}
