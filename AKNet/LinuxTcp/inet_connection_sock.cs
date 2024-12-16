namespace AKNet.LinuxTcp
{
    internal class inet_connection_sock:sock
    {
        public uint icsk_user_timeout;//这个成员用于设置一个用户定义的超时值，通常用于控制TCP连接在特定状态下的等待时间。当涉及到长时间未接收到数据或确认的情况时，这个超时值可以用来决定何时关闭连接。
        public uint icsk_rto;
        public int icsk_retransmits;//用于记录发生超时重传的次数
        public icsk_ack icsk_ack;
    }

    internal struct icsk_ack
    {
        public byte pending;    /* ACK is pending			   */
        public byte quick;  /* Scheduled number of quick acks	   */
        public byte pingpong;   /* The session is interactive		   */
        public byte retry;  /* Number of attempts			   */
        public uint ato;
        public uint lrcv_flowlabel, /* last received ipv6 flowlabel	   */

        public uint unused;
        public ulong timeout;   /* Currently scheduled timeout		   */
        public uint lrcvtime;  /* timestamp of last received data packet */
        public ushort last_seg_size; /* Size of last incoming segment	   */
        public ushort rcv_mss;   /* MSS used for delayed ACK decisions	   */
    }
}
