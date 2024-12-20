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
    }
}
