namespace AKNet.LinuxTcp
{
    internal class sk_buff_head
    {
        public sk_buff next;
        public sk_buff prev;
        public uint qlen;
    }

    internal class sk_buff
    {
        public long skb_mstamp_ns; //用于记录与该数据包相关的高精度时间戳（以纳秒为单位
        public tcp_skb_cb[] cb = new tcp_skb_cb[48];
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

        public static sk_buff skb_rb_first(AkRBTree<sk_buff> root)
        {
            return root.FirstValue();
        }

        public static sk_buff skb_rb_last(AkRBTree<sk_buff> root)
        {
            return root.LastValue();
        }

        public static sk_buff skb_rb_next(AkRBTree<sk_buff> mTree, RedBlackTreeNode<sk_buff> skbNode)
        {
            return mTree.NextValue(skbNode);
        }

        public static sk_buff skb_rb_prev(AkRBTree<sk_buff> mTree, RedBlackTreeNode<sk_buff> skbNode)
        {
            return mTree.PrevValue(skbNode);
        }
    }

}
