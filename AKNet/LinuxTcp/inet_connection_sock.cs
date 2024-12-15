namespace AKNet.LinuxTcp
{
    internal class inet_connection_sock:sock
    {
        public uint icsk_user_timeout;//这个成员用于设置一个用户定义的超时值，通常用于控制TCP连接在特定状态下的等待时间。当涉及到长时间未接收到数据或确认的情况时，这个超时值可以用来决定何时关闭连接。
        public uint icsk_rto;
        public int icsk_retransmits;//用于记录发生超时重传的次数
    }
}
