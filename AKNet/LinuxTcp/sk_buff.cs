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
    internal enum SKB_FCLONE
    {
        SKB_FCLONE_UNAVAILABLE, /* skb has no fclone (from head_cache) */
        SKB_FCLONE_ORIG,    /* orig skb (from fclone_cache) */
        SKB_FCLONE_CLONE,   /* companion fclone skb (from fclone_cache) */
    }

    internal enum SKBFL
    {
        /* use zcopy routines */
        SKBFL_ZEROCOPY_ENABLE = (byte)LinuxTcpFunc.BIT(0),

        /* This indicates at least one fragment might be overwritten
         * (as in vmsplice(), sendfile() ...)
         * If we need to compute a TX checksum, we'll need to copy
         * all frags to avoid possible bad checksum
         */
        SKBFL_SHARED_FRAG = (byte)LinuxTcpFunc.BIT(1),

        /* segment contains only zerocopy data and should not be
         * charged to the kernel memory.
         */
        SKBFL_PURE_ZEROCOPY = (byte)LinuxTcpFunc.BIT(2),

        SKBFL_DONT_ORPHAN = (byte)LinuxTcpFunc.BIT(3),

        /* page references are managed by the ubuf_info, so it's safe to
         * use frags only up until ubuf_info is released
         */
        SKBFL_MANAGED_FRAG_REFS = (byte)LinuxTcpFunc.BIT(4),
    }

    internal class sk_buff_head
    {
        public sk_buff next;
        public sk_buff prev;
        public uint qlen;
    }

    public class skb_shared_hwtstamps
    {
        public long hwtstamp;
        public byte[] netdev_data;
    }

    public class xsk_tx_metadata_compl
    {
       public long tx_timestamp;
    }

    public class skb_shared_info
    {
        public const int MAX_SKB_FRAGS = 17;

        public byte flags;
        public byte meta_len;
        public byte nr_frags;
        public byte tx_flags;
        public ushort gso_size;

        public ushort gso_segs;
        public sk_buff frag_list;
        public skb_shared_hwtstamps hwtstamps;
        public xsk_tx_metadata_compl xsk_meta;
        public uint gso_type;
        public uint tskey;
        public uint xdp_frags_size;
        //void* destructor_arg;
        public int[] frags = new int[MAX_SKB_FRAGS];
    }

internal class sk_buff
    {
        public long skb_mstamp_ns; //用于记录与该数据包相关的高精度时间戳（以纳秒为单位
        public readonly tcp_skb_cb[] cb = new tcp_skb_cb[48];
        public byte cloned;
        public byte nohdr;
        public byte fclone;
        public sock sk;
        public sk_buff_fclones container_of;
        public int len;
        public int data_len;
        public byte decrypted;

        public int tail;
        public int end;
        public int head;
        public byte[] data;
        public skb_shared_info skb_shared_info;
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

        public static int skb_headlen(sk_buff skb)
        {
            return skb.len - skb.data_len;
        }

        public static skb_shared_info skb_shinfo(sk_buff skb)
        {
            return skb.skb_shared_info;
        }

        public static void skb_split(sk_buff skb, sk_buff skb1, uint len)
        {
            int pos = skb_headlen(skb);
            int zc_flags = SKBFL.SKBFL_SHARED_FRAG | SKBFL.SKBFL_PURE_ZEROCOPY;

            skb_zcopy_downgrade_managed(skb);

            skb_shinfo(skb1).flags |= skb_shinfo(skb).flags & zc_flags;
            skb_zerocopy_clone(skb1, skb, 0);
            if (len < pos)    /* Split line is inside header. */
            {
                skb_split_inside_header(skb, skb1, len, pos);
            }
            else        /* Second chunk has no header, nothing to copy. */
            {
                skb_split_no_header(skb, skb1, len, pos);
            }
        }
    }

}
