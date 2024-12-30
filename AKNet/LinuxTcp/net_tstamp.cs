
internal static partial class LinuxTcpFunc
{
    //是 Linux 内核网络栈中用于时间戳标记（timestamping）的一个选项，特别与发送（transmit, TX）数据包的时间戳有关
    public const int SOF_TIMESTAMPING_TX_HARDWARE = (1 << 0);
    public const int SOF_TIMESTAMPING_TX_SOFTWARE = (1 << 1);
    public const int SOF_TIMESTAMPING_RX_HARDWARE = (1 << 2);
    public const int SOF_TIMESTAMPING_RX_SOFTWARE = (1 << 3);
    public const int SOF_TIMESTAMPING_SOFTWARE = (1 << 4);
    public const int SOF_TIMESTAMPING_SYS_HARDWARE = (1 << 5);
    public const int SOF_TIMESTAMPING_RAW_HARDWARE = (1 << 6);
    public const int SOF_TIMESTAMPING_OPT_ID = (1 << 7);
    public const int SOF_TIMESTAMPING_TX_SCHED = (1 << 8);
    public const int SOF_TIMESTAMPING_TX_ACK = (1 << 9);
    public const int SOF_TIMESTAMPING_OPT_CMSG = (1 << 10);
    public const int SOF_TIMESTAMPING_OPT_TSONLY = (1 << 11);
    public const int SOF_TIMESTAMPING_OPT_STATS = (1 << 12);
    public const int SOF_TIMESTAMPING_OPT_PKTINFO = (1 << 13);
    public const int SOF_TIMESTAMPING_OPT_TX_SWHW = (1 << 14);
    public const int SOF_TIMESTAMPING_BIND_PHC = (1 << 15);
    public const int SOF_TIMESTAMPING_OPT_ID_TCP = (1 << 16);
    public const int SOF_TIMESTAMPING_OPT_RX_FILTER = (1 << 17);
    public const int SOF_TIMESTAMPING_LAST = SOF_TIMESTAMPING_OPT_RX_FILTER;
    public const int SOF_TIMESTAMPING_MASK = (SOF_TIMESTAMPING_LAST - 1) | SOF_TIMESTAMPING_LAST;
}
