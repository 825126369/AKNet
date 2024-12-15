namespace AKNet.LinuxTcp
{
    struct sk_buff_head
    {
        public sk_buff next;
        public sk_buff prev;
        public uint qlen;
    }

    internal class sk_buff
    {

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
