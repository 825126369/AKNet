namespace AKNet.LinuxTcp
{
    internal struct tcp_sock
    {
        public const ushort TCP_MSS_DEFAULT = 536;
        public const int TCP_INIT_CWND = 10;

        public int sk_wmem_queued;
        public int sk_forward_alloc;//这个字段主要用于跟踪当前套接字还可以分配多少额外的内存来存储数据包
        public uint max_window;//

        //这个字段用于跟踪已经通过套接字发送给应用层的数据序列号（sequence number）。具体来说，pushed_seq 表示最近一次调用 tcp_push() 或类似函数后，TCP 层认为应该被“推送”到网络上的数据的最后一个字节的序列号加一。
        public uint pushed_seq;
        public uint write_seq;  //应用程序通过 send() 或 write() 系统调用写入到TCP套接字中的最后一个字节的序列号。
        public uint snd_nxt;    //Tcp层 下一个将要发送的数据段的第一个字节的序列号。
        public uint snd_una;//表示未被确认的数据段的第一个字节的序列号。
        public uint mss_cache;  //单个数据包的最大大小

        public uint snd_cwnd;     //表示当前允许发送方发送的最大数据量（以字节为单位)
        public uint copied_seq; //记录了应用程序已经从接收缓冲区读取的数据的最后一个字节的序列号（seq）加一，即下一个期待被用户空间读取的数据的起始序列号

        //用于记录当前在网络中飞行的数据包数量。这些数据包已经发送出去但还未收到确认（ACK）
        public uint packets_out;  //当前飞行中的数据包数量
        public uint retrans_out;  //表示当前正在重传的数据包数量
        public uint sacked_out;//表示已经被选择性确认SACK的数据包数量。
        public uint lost_out; // 表示被认为已经丢失的数据包数量
        public uint app_limited;

        //用于记录已经成功传递给应用程序的数据包总数。这个字段包括了所有已传递的数据包，即使这些数据包可能因为重传而被多次传递。
        public uint delivered;

        public ushort gso_segs; //它用于表示通过 Generic Segmentation Offload(GSO) 分段的数据包的数量。GSO 是一种优化技术，允许操作系统将大的数据包交给网卡，然后由网卡硬件负责将这些大包分段成适合底层网络传输的小包

        //获取套接字已经分配的发送缓冲区大小
        int sk_wmem_alloc_get()
        {
            return 0;
        }

        uint tcp_snd_cwnd()
        {
            return snd_cwnd;
        }

        uint tcp_packets_in_flight()
        {
            return packets_out - tcp_left_out() + retrans_out;
        }

        uint tcp_left_out()
        {
            return sacked_out + lost_out;
        }

        void tcp_rate_check_app_limited()
        {
            var tp = this;
            if (tp.write_seq - tp.snd_nxt < tp.mss_cache &&
                tcp_packets_in_flight() < tcp_snd_cwnd() &&
                tp.lost_out <= tp.retrans_out)
            {
                tp.app_limited = (uint)(tp.delivered + (int)tcp_packets_in_flight() > 0 ? 0 : 1);
            }
        }
    }
}
