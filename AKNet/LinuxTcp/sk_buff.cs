/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.LinuxTcp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace AKNet.LinuxTcp
{
    internal enum SKB_FCLONE
    {
        SKB_FCLONE_UNAVAILABLE, /* skb has no fclone (from head_cache) */
        SKB_FCLONE_ORIG,    /* orig skb (from fclone_cache) */
        SKB_FCLONE_CLONE,   /* companion fclone skb (from fclone_cache) */
    }

    internal enum skb_tstamp_type
    {
        SKB_CLOCK_REALTIME, //基于实时时间:使用系统的实时时间（wall-clock time），即从1970年1月1日以来的时间（Unix纪元）。这种时间戳反映了当前的日期和时间，但容易受到系统时间调整的影响（如NTP同步）。
        SKB_CLOCK_MONOTONIC,//基于单调时钟:使用一个单调递增的时钟，该时钟从系统启动开始计时，不会受到系统时间调整的影响。适合用于测量持续时间和间隔，因为它保证了时间总是向前推进。
        SKB_CLOCK_TAI,//基于国际原子时（TAI）:TAI 是一种高精度的时间标准，与 UTC 相比不包含闰秒。这意味着 TAI 时间是连续的，没有跳跃。它适用于需要高精度时间戳的应用场景，尤其是在科学计算或网络协议中。
        __SKB_CLOCK_MAX = SKB_CLOCK_TAI,
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

    public class skb_frag
    {
        public byte[] netmem;
        public int len;
        public int offset;
    }

    //skb_shared_info 是 Linux 内核中 struct sk_buff（套接字缓冲区）的一部分，用于存储与数据包共享的额外信息。
    //这个结构体包含了有关数据包分片、GSO（Generic Segmentation Offload）、TSO（TCP Segmentation Offload）等高级网络特性的重要信息。
    //它在处理大尺寸数据包或需要硬件加速的情况下特别有用。
    //使用场景
    //分片处理:
    //当数据包过大无法一次性传输时，可以将其拆分为多个分片。nr_frags 和 frags[] 字段帮助管理这些分片。
    //分段卸载(GSO/TSO) :
    //支持硬件加速的大数据包传输，减少内核处理负担。gso_size、gso_segs 和 gso_type 字段用于配置和管理分段卸载。
    //时间戳记录:
    //在需要高精度时间测量的应用中，如 PTP（精确时间协议）或网络监控工具，hwtstamps 字段提供了必要的基础设施。
    //多播和广播:
    //当数据包需要复制到多个目的地时，dataref 字段确保所有副本都能正确访问共享数据。
    //XDP 和 eBPF:
    //xdp_frags_size 和 frag_list 字段支持 XDP 和 eBPF 程序，提供高效的用户空间数据处理能力。
    internal class skb_shared_info
    {
        public const int MAX_SKB_FRAGS = 17;

        public byte flags; //包含各种标志位，用于标记 skb_shared_info 的状态或特性。
        public byte meta_len;//表示元数据的长度，用于某些特定场景下的元数据处理。
        public byte nr_frags;//表示数据包包含的分片数量。每个分片通常对应于一个物理内存页。
        public byte tx_flags;//发送标志，用于控制发送路径上的行为。

        public ushort gso_size; //每个分段的大小，用于通用分段卸载（GSO）。
        public ushort gso_segs;//总分段数量，用于通用分段卸载（GSO）。

        public sk_buff frag_list; //指向分片链表的指针，用于管理多个 sk_buff 形式的分片。
        public skb_shared_hwtstamps hwtstamps; //存储硬件时间戳信息，支持精确的时间测量。
        public xsk_tx_metadata_compl xsk_meta; //存储 XSK（eXpress Data Path）传输元数据，用于加速用户空间传输。
        public uint gso_type; //指定 GSO 类型，例如 TCPv4、TCPv6、UDP 等。
        public uint tskey; //时间戳键，用于关联时间戳信息。
        public uint xdp_frags_size; //XDP 分片的总大小，用于 XDP（eXpress Data Path）框架中的分片管理。
        //void* destructor_arg; //销毁函数参数，确保在 sk_buff 被释放时执行特定清理操作所需的参数。
        public skb_frag[] frags = new skb_frag[MAX_SKB_FRAGS]; //存储分片信息的数组，每个元素是一个 skb_frag_t，包含指向实际数据页的指针和其他元数据。该字段必须是结构体的最后一个成员，以便动态扩展分片数量。
        public int dataref;//原子类型的引用计数器，用于跟踪有多少个 sk_buff 共享相同的数据。这对于内存管理和避免过早释放数据非常重要。
    }

    internal class sk_buff:sk_buff_list
    {
        public const int SKB_DATAREF_SHIFT = 16;
        public const int SKB_DATAREF_MASK = (1 << SKB_DATAREF_SHIFT) - 1;

        public tcp_sack_block_wire[] sp_wire = new tcp_sack_block_wire[5];

        public tcp_word_hdr hdr;
        public long skb_mstamp_ns;
        public readonly tcp_skb_cb[] cb = new tcp_skb_cb[48];
        public byte cloned;
        public byte nohdr;
        public byte fclone;
        public sock sk;
        public sk_buff_fclones container_of;
        public int len;
        public int data_len;
        public bool decrypted;

        public int tail;
        public int end;
        public int head;
        public byte[] data;
        public int nDataBeginIndex;

        public skb_shared_info skb_shared_info;

        //skb->ooo_okay 是一个标志位，用于指示该 sk_buff 是否可以被作为乱序数据段接收并处理。
        //如果设置为 true，则表示可以安全地接收和处理该乱序段；
        //如果为 false，则可能需要等待直到有更多空间或重传队列清理完毕。
        public bool ooo_okay;
        public list_head<sk_buff> tcp_tsorted_anchor;

        //skb->tstamp 是 Linux 内核中 struct sk_buff（套接字缓冲区）结构体的一个成员，用于存储与数据包相关的时间戳。
        //这个时间戳通常在数据包到达或发送时记录，以提供关于网络性能、延迟和其他时间敏感信息的统计数据
        public long tstamp;
        public byte tstamp_type;

        public bool unreadable;

        public rb_node rbnode;

        public LinkedListNode<sk_buff> NextNode;
        public LinkedListNode<sk_buff> PrevNode;
        public object dev = null;

        public bool dst_pending_confirm;
        public uint hash;
        public bool l4_hash = false;
        public int truesize; //总长度

        //ip_summed 是 Linux 内核网络栈中的一个字段，存在于 struct sk_buff（也称为 skb）结构体中。
        //这个字段用于指示 IP 数据包校验和的计算状态，帮助内核决定是否需要计算或验证数据包的校验和。
        //这在高性能网络处理中非常重要，因为它可以优化校验和的计算，减少不必要的 CPU 开销。
        public byte ip_summed;
        public bool csum_valid; //如果 csum_valid 为 1，表示校验和有效；如果为 0，表示校验和无效或未验证
        public uint csum;
        public ushort csum_start;   //这个值告诉硬件从哪里开始计算校验和
        public ushort csum_offset;  //这个值告诉硬件将计算出的校验和存储在哪个位置。
        public bool csum_complete_sw;
    }

    internal class sk_buff_fclones
    {
        public sk_buff skb1;
        public sk_buff skb2;
        public int fclone_ref;
    }

    public class skb_checksum_ops
    {
        public Func<byte[], int, uint, uint> update;
        public Func<uint, uint, int, int, uint> combine;
    }

    internal static partial class LinuxTcpFunc
    {
        public static sk_buff skb_peek(sk_buff_head list_)
        {
            sk_buff skb = list_.next;
            return skb;
        }

        public static sk_buff __skb_dequeue(LinkedList<sk_buff> list)
        {
            sk_buff skb = list.First.Value;
            list.RemoveFirst();
            return skb;
        }

        public static sk_buff rb_to_skb(rb_node node)
        {
            return node.value;
        }

        public static sk_buff skb_rb_first(rb_root root)
        {
            return rb_to_skb(rb_first(root));
        }

        public static sk_buff skb_rb_last(rb_root root)
        {
            return rb_to_skb(rb_last(root));
        }

        public static sk_buff skb_rb_next(sk_buff skb)
        {
            return rb_to_skb(rb_next(skb.rbnode));
        }

        public static sk_buff skb_rb_prev(sk_buff skb)
        {
            return rb_to_skb(rb_prev(skb.rbnode));
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

        public static void skb_split(sk_buff skb, sk_buff skb1, int len)
        {
            int pos = skb_headlen(skb);
            byte zc_flags = (byte)(SKBFL_SHARED_FRAG | SKBFL_PURE_ZEROCOPY);
            skb_shinfo(skb1).flags = (byte)(skb_shinfo(skb1).flags | skb_shinfo(skb).flags & zc_flags);

            //skb_zerocopy_clone(skb1, skb, 0);
            //if (len < pos)
            //{
            //    skb_split_inside_header(skb, skb1, len, pos);
            //}
            //else
            //{
            //    skb_split_no_header(skb, skb1, len, pos);
            //}
        }

        public static void skb_set_delivery_time(sk_buff skb, long kt, skb_tstamp_type tstamp_type)
        {
            skb.tstamp = kt;

            if (kt > 0)
            {
                skb.tstamp_type = (byte)tstamp_type;
            }
            else
            {
                skb.tstamp_type = (byte)skb_tstamp_type.SKB_CLOCK_REALTIME;
            }
        }

        public static bool skb_cloned(sk_buff skb)
        {
            return skb.cloned > 0 && (skb_shinfo(skb).dataref & sk_buff.SKB_DATAREF_MASK) != 1;
        }

        public static bool skb_frags_readable(sk_buff skb)
        {
            return !skb.unreadable;
        }

        public static int skb_shift(sk_buff tgt, sk_buff skb, int shiftlen)
        {
            return 0;
        }

        public static void skb_set_dst_pending_confirm(sk_buff skb, bool val)
        {
            skb.dst_pending_confirm = val;
        }

        static int skb_orphan_frags(sk_buff skb)
        {
            return 0;
        }

        public static sk_buff skb_clone(sk_buff skb)
        {
            sk_buff fclones = new sk_buff();
            return fclones;
        }

        static void __skb_header_release(sk_buff skb)
        {
            skb.nohdr = 1;
        }

        static void __skb_unlink(sk_buff skb, sk_buff_head list)
        {
            sk_buff next, prev;
            list.qlen--;
            next = skb.next;
            prev = skb.prev;

            skb.next = null;
            skb.prev = null;

            next.prev = prev;
            prev.next = next;
        }

        //来预先分配一定量的内存，以便后续添加元素时不需要频繁重新分配内存。
        static void skb_reserve(sk_buff skb, int len)
        {

        }

        static uint skb_frag_size(skb_frag frag)
        {
            return frag.len;
        }

        static uint skb_frag_off(skb_frag frag)
        {
            return frag.offset;
        }

        static ReadOnlySpan<byte> skb_frag_page(skb_frag frag)
        {
            return frag.netmem.AsSpan().Slice(frag.offset, frag.len);
        }

        static void skb_frag_size_add(skb_frag frag, int delta)
        {
            frag.len += (uint)delta;
        }

        static void skb_frag_page_copy(skb_frag fragto, skb_frag fragfrom)
        {
            fragto.netmem = fragfrom.netmem;
        }

        static void skb_frag_off_copy(skb_frag fragto, skb_frag fragfrom)
        {
            fragto.offset = fragfrom.offset;
        }

        static void skb_frag_size_set(skb_frag frag, uint size)
        {
            frag.len = size;
        }


        static void kfree_skb(sk_buff skb)
        {

        }

        static void consume_skb(sk_buff skb)
        {
            kfree_skb(skb);
        }

        static void sk_mem_charge(sock sk, int size)
        {
            if (!sk_has_account(sk))
            {
                return;
            }
            sk_forward_alloc_add(sk, -size);
        }

        static void __skb_insert(sk_buff newsk, sk_buff prev, sk_buff next, sk_buff_head list)
        {
            newsk.next = next;
            newsk.prev = prev;
            ((sk_buff_list)next).prev = newsk;
            ((sk_buff_list)prev).next = newsk;
            list.qlen++;
        }

        static void __skb_queue_before(sk_buff_head list, sk_buff next, sk_buff newsk)
        {
            __skb_insert(newsk, ((sk_buff_list)next).prev, next, list);
        }

        static void __skb_queue_tail(sk_buff_head list, sk_buff newsk)
        {
            __skb_queue_before(list, (sk_buff)list, newsk);
        }

        static bool skb_zcopy_pure(sk_buff skb)
        {
            return BoolOk(skb_shinfo(skb).flags & (byte)SKBFL_PURE_ZEROCOPY);
        }

        static void skb_frag_off_add(skb_frag frag, int delta)
        {
            frag.offset += (uint)delta;
        }

        static void skb_frag_size_sub(skb_frag frag, int delta)
        {
            frag.len -= (uint)delta;
        }

        static bool skb_queue_is_last(sk_buff_head list, sk_buff skb)
        {
            return skb.next == null;
        }

        static sk_buff skb_peek_tail(sk_buff_head list_)
        {
            sk_buff skb = list_.prev;
            return skb;
        }

        static uint skb_end_offset(sk_buff skb)
        {
            return (uint)skb.end;
        }

        static int SKB_TRUESIZE(int X)
        {
            return X;
        }

        static void __skb_queue_after(sk_buff_head list, sk_buff prev, sk_buff newsk)
        {
            __skb_insert(newsk, prev, ((sk_buff_list)prev).next, list);
        }

        static bool skb_can_coalesce(sk_buff skb, int i, int off)
        {
            if (i > 0)
            {
                skb_frag frag = skb_shinfo(skb).frags[i - 1];
                return off == skb_frag_off(frag) + skb_frag_size(frag);
            }
            return false;
        }

        //static void net_zcopy_put(ubuf_info uarg)
        //{
        // if (uarg != null)
        //    {
        //        uarg.ops.complete(null, uarg, true);
        //    }
        //}

        static void skb_len_add(sk_buff skb, int delta)
        {
            skb.len += delta;
            skb.data_len += delta;
            skb.truesize += delta;
        }

        static void skb_frag_fill_netmem_desc(skb_frag frag, int netmem, int off, int size)
        {
            frag.netmem = netmem;
            frag.offset = (uint)off;
            skb_frag_size_set(frag, (uint)size);
        }

        static void __skb_fill_netmem_desc_noacc(skb_shared_info shinfo, int i, int netmem, int off, int size)
        {
            skb_frag frag = shinfo.frags[i];
            skb_frag_fill_netmem_desc(frag, netmem, off, size);
        }

        static void __skb_fill_netmem_desc(sk_buff skb, int i, int netmem, int off, int size)
        {
            //   page page;
            //__skb_fill_netmem_desc_noacc(skb_shinfo(skb), i, netmem, off, size);

            //if (netmem_is_net_iov(netmem)) 
            //   {
            // skb.unreadable = true;
            // return;
            //}

            //   page = netmem_to_page(netmem);
            //   page = compound_head(page);
            //   if (page_is_pfmemalloc(page))
            //   {
            //       skb.pfmemalloc = true;
            //   }
        }

        static void skb_fill_netmem_desc(sk_buff skb, int i, int netmem, int off, int size)
        {
            __skb_fill_netmem_desc(skb, i, netmem, off, size);
            skb_shinfo(skb).nr_frags = (byte)(i + 1);
        }

        static void skb_fill_page_desc(sk_buff skb, int i, page page, int off, int size)
        {
            //skb_fill_netmem_desc(skb, i, page_to_netmem(page), off, size);
        }

        static int skb_copy_datagram_msg(sk_buff from, int offset, ReadOnlySpan<byte> msg, int size)
        {
            //return skb_copy_datagram_iter(from, offset, &msg->msg_iter, size);
            return 0;
        }

        static void __kfree_skb(sk_buff skb)
        {

        }

        static void __skb_pull(sk_buff skb, int len)
        {
            skb.len -= len;
            //skb.data += len;
        }

        static uint skb_queue_len(sk_buff_head list_)
        {
            return list_.qlen;
        }

        static skb_shared_hwtstamps skb_hwtstamps(sk_buff skb)
        {
            return skb_shinfo(skb).hwtstamps;
        }

        static int skb_tailroom(sk_buff skb)
        {
            return skb_is_nonlinear(skb) ? 0 : skb.end - skb.tail;
        }

        static bool skb_try_coalesce(sk_buff to, sk_buff from)
        {
            skb_shared_info to_shinfo, from_shinfo;
            int i, delta, len = from.len;

            if (skb_cloned(to))
            {
                return false;
            }

            if (skb_frags_readable(from) != skb_frags_readable(to))
            {
                return false;
            }

            if (len <= skb_tailroom(to) && skb_frags_readable(from))
            {
                return true;
            }

            to_shinfo = skb_shinfo(to);
            from_shinfo = skb_shinfo(from);
            if (to_shinfo.frag_list != null || from_shinfo.frag_list != null)
            {
                return false;
            }

            Array.Copy(from_shinfo.frags, 0, to_shinfo.frags, to_shinfo.nr_frags, from_shinfo.nr_frags);
            to_shinfo.nr_frags += from_shinfo.nr_frags;

            if (!skb_cloned(from))
            {
                from_shinfo.nr_frags = 0;
            }

            to.len += len;
            to.data_len += len;
            return true;
        }

        static void __net_timestamp(sk_buff skb)
        {
            skb.tstamp = tcp_jiffies32;
            skb.tstamp_type = (byte)skb_tstamp_type.SKB_CLOCK_REALTIME;
        }

        //__skb_tstamp_tx 是 Linux 内核中用于处理套接字缓冲区（SKB, socket buffer）时间戳的一个函数。
        //它主要用于记录数据包发送的时间戳信息，这对于网络性能监控、延迟测量和某些协议特性（如TCP的精确往返时间RTT计算）非常重要。
        //功能与使用
        //__skb_tstamp_tx 函数的主要作用是为即将发送的数据包设置一个高精度的时间戳.
        //这个时间戳通常是在数据包被实际提交给网络接口卡（NIC）进行发送时获取的，确保了时间戳的准确性。
        //通过这种方式，Linux内核能够提供关于数据包发送时间的详细信息，这对于分析网络性能和调试网络问题非常有用。
        static void __skb_tstamp_tx(sk_buff orig_skb, sk_buff ack_skb, skb_shared_hwtstamps hwtstamps, tcp_sock tp, int tstype)
        {
            sk_buff skb = null;
            bool tsonly, opt_stats = false;
            uint tsflags;

            if (tp == null)
            {
                return;
            }

            tsflags = tp.sk_tsflags;
            if (hwtstamps == null && !BoolOk(tsflags & SOF_TIMESTAMPING_OPT_TX_SWHW) &&
                BoolOk(skb_shinfo(orig_skb).tx_flags & SKBTX_IN_PROGRESS))
            {
                return;
            }

            tsonly = BoolOk(tsflags & SOF_TIMESTAMPING_OPT_TSONLY);
            if (tsonly)
            {
                skb_shinfo(skb).tx_flags |= (byte)(skb_shinfo(orig_skb).tx_flags & SKBTX_ANY_TSTAMP);
                skb_shinfo(skb).tskey = skb_shinfo(orig_skb).tskey;
            }

            if (hwtstamps != null)
            {
                skb_shinfo(skb).hwtstamps = hwtstamps;
            }
            else
            {
                __net_timestamp(skb);
            }
        }

        static bool skb_is_nonlinear(sk_buff skb)
        {
            return skb.data_len > 0;
        }

        static bool skb_can_shift(sk_buff skb)
        {
            return skb_headlen(skb) == 0 && skb_is_nonlinear(skb);
        }

        //skb_headroom 是一个用于获取 sk_buff（网络数据包缓冲区）头部空间大小的函数。它返回从 skb->head 到 skb->data 之间的空闲字节数。
        static uint skb_headroom(sk_buff skb)
        {
            return (uint)skb.nDataBeginIndex;
        }

        static int skb_checksum_start_offset(sk_buff skb)
        {
            return (int)(skb.csum_start - skb_headroom(skb));
        }

        static bool skb_csum_unnecessary(sk_buff skb)
        {
            return (skb.ip_summed == CHECKSUM_UNNECESSARY || skb.csum_valid ||
                (skb.ip_summed == CHECKSUM_PARTIAL && skb_checksum_start_offset(skb) >= 0));
        }

        static uint __skb_checksum(sk_buff skb, int offset, int len, uint csum, skb_checksum_ops ops)
        {
            int start = skb_headlen(skb);
            int i, copy = start - offset;
            sk_buff frag_iter;
            int pos = 0;

            if (copy > 0)
            {
                if (copy > len)
                {
                    copy = len;
                }

                csum = csum_partial_ext(skb.data.AsSpan().Slice(offset), copy, csum);

                if ((len -= copy) == 0)
                {
                    return csum;
                }
                offset += copy;
                pos = copy;
            }

            if (!skb_frags_readable(skb))
            {
                return 0;
            }

            for (i = 0; i < skb_shinfo(skb).nr_frags; i++)
            {
                int end;
                skb_frag frag = skb_shinfo(skb).frags[i];

                end = start + (int)skb_frag_size(frag);
                if ((copy = end - offset) > 0)
                {
                    uint p_off, p_len, copied;
                    int pIndex = 0;

                    uint csum2;
                    if (copy > len)
                    {
                        copy = len;
                    }

                    //skb_frag_foreach_page(frag,
                    //                       skb_frag_off(frag) + offset - start,
                    //                       copy, p, p_off, p_len, copied)

                    //#define skb_frag_foreach_page(f, f_off, f_len, p, p_off, p_len, copied)	
                    for (pIndex = 0,
                         p_off = (f_off) & (PAGE_SIZE - 1),
                         p_len = skb_frag_must_loop(p) ?
                         Math.Min(copy, PAGE_SIZE - p_off) : f_len,
                         copied = 0;
                         copied < f_len;
                         copied += p_len, p++, p_off = 0,
                         p_len = min_t(u32, f_len - copied, PAGE_SIZE))
                    {
                        var vaddr = skb_frag_page(frag);
                        csum2 = csum_partial_ext(vaddr + p_off, p_len, 0);
                        csum = csum_block_add_ext(csum, csum2, pos, p_len);
                        pos += p_len;
                    }

                    if ((len -= copy) == 0)
                    {
                        return csum;
                    }
                    offset += copy;
                }
                start = end;
            }

	        skb_walk_frags(skb, frag_iter) {
            int end;

            WARN_ON(start > offset + len);

            end = start + frag_iter->len;
            if ((copy = end - offset) > 0)
            {
                __wsum csum2;
                if (copy > len)
                    copy = len;
                csum2 = __skb_checksum(frag_iter, offset - start,
                               copy, 0, ops);
                csum = INDIRECT_CALL_1(ops->combine, csum_block_add_ext,
                               csum, csum2, pos, copy);
                if ((len -= copy) == 0)
                    return csum;
                offset += copy;
                pos += copy;
            }
            start = end;
        }
        BUG_ON(len);

        return csum;
       }

        static ushort skb_checksum(sk_buff skb, int offset, int len, ushort csum)
        {
            skb_checksum_ops ops = new skb_checksum_ops() 
            {
		        update = csum_partial_ext,
		        combine = csum_block_add_ext,
            };

	        return __skb_checksum(skb, offset, len, csum, ops);
        }

        static ushort __skb_checksum_complete(sk_buff skb)
        {
            ushort csum;
            ushort sum;

            csum = skb_checksum(skb, 0, skb.len, 0);
            sum = csum_fold(csum_add(skb.csum, csum));
            if (sum == 0)
            {
                if (skb.ip_summed == CHECKSUM_COMPLETE && !skb.csum_complete_sw)
                {
                    //netdev_rx_csum_fault(skb->dev, skb);
                }
            }

            skb.csum = csum;
            skb.ip_summed = CHECKSUM_COMPLETE;
            skb.csum_complete_sw = true;
            skb.csum_valid = !BoolOk(sum);
            return sum;
        }

        static void sk_skb_reason_drop(tcp_sock tp, sk_buff skb, skb_drop_reason reason)
        {
	        
        }

    }

}
