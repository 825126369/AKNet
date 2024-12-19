namespace AKNet.LinuxTcp
{
    internal enum SKB_FCLONE
    {
        SKB_FCLONE_UNAVAILABLE, /* skb has no fclone (from head_cache) */
        SKB_FCLONE_ORIG,    /* orig skb (from fclone_cache) */
        SKB_FCLONE_CLONE,   /* companion fclone skb (from fclone_cache) */
    }


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
        public byte cloned;
        public byte nohdr;
        public byte fclone;
        public sock sk;
        public sk_buff_fclones container_of;
        public int len;
        public byte decrypted;
    }

    internal class sk_buff_fclones
    {
        public sk_buff  skb1;
	    public sk_buff  skb2;
	    public int fclone_ref;
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

        public static bool skb_fclone_busy(tcp_sock tp, sk_buff skb)
        {
            sk_buff_fclones fclones = skb.container_of;
            return skb.fclone == (byte)SKB_FCLONE.SKB_FCLONE_ORIG && fclones.fclone_ref > 1 && fclones.skb2.sk == tp;
        }

        public static void skb_copy_decrypted(sk_buff to, sk_buff from)
        {
            to.decrypted = from.decrypted;
        }
}

}
