/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    internal class netns_ipv4
    {
        public int sysctl_tcp_retries1 = 3; //默认最大重传次数
        public int sysctl_tcp_retries2 = 5;

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
        public byte sysctl_tcp_shrink_window;

        public byte sysctl_tcp_min_tso_segs;
        //sysctl_tcp_tso_rtt_log 是一个Linux内核参数，它用于控制TSO（TCP Segmentation Offload）机制下的RTT（Round-Trip Time，往返时间）测量精度。
        //具体来说，这个参数决定了在启用TSO的情况下，内核用来计算最小RTT的时间单位的对数形式。
        //默认情况下，该值被设置为9，这意味着最小RTT是以512微秒（即 微秒）作为基本单位来衡量的14。
        public byte sysctl_tcp_tso_rtt_log;
        public int sysctl_tcp_limit_output_bytes;
    }
}
