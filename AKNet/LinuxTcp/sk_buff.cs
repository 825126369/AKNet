namespace AKNet.LinuxTcp
{
    internal struct sk_buff_head
    {
        public sk_buff next;
        public sk_buff prev;
        public uint qlen;
    }

    internal class sk_buff
    {
        public long skb_mstamp_ns; //用于记录与该数据包相关的高精度时间戳（以纳秒为单位）
    }
    
    internal static partial class LinuxTcpFunc
    {
        public static sk_buff skb_peek(sk_buff_head list_)
        {
	        return null;
        }

        public static sk_buff __skb_dequeue(sk_buff_head list)
        {
	        return null;
        }
    }

}
