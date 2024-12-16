namespace AKNet.LinuxTcp
{
    internal class netns_ipv4
    {
        public int sysctl_tcp_retries1 = 3; //默认最大重传次数
        public int sysctl_tcp_retries2 = 5;

        //当 sysctl_tcp_pingpong_thresh 设置为非零值时，内核会在每个时间窗口（通常是几百毫秒）内统计接收到的数据包数量。
        //如果在这个时间窗口内接收到的数据包数量超过了设定的阈值，内核可能会认为这是一个交互式的连接，并启用乒乓模式来优化 ACK 和数据传输行为。
        public int sysctl_tcp_pingpong_thresh = 1;
    }
}
