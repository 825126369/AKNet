/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal enum skb_tstamp_type
    {
        SKB_CLOCK_REALTIME, //基于实时时间:使用系统的实时时间（wall-clock time），即从1970年1月1日以来的时间（Unix纪元）。这种时间戳反映了当前的日期和时间，但容易受到系统时间调整的影响（如NTP同步）。
        SKB_CLOCK_MONOTONIC,//基于单调时钟:使用一个单调递增的时钟，该时钟从系统启动开始计时，不会受到系统时间调整的影响。适合用于测量持续时间和间隔，因为它保证了时间总是向前推进。
        SKB_CLOCK_TAI,//基于国际原子时（TAI）:TAI 是一种高精度的时间标准，与 UTC 相比不包含闰秒。这意味着 TAI 时间是连续的，没有跳跃。它适用于需要高精度时间戳的应用场景，尤其是在科学计算或网络协议中。
        __SKB_CLOCK_MAX = SKB_CLOCK_TAI,
    }

    internal class sk_buff : sk_buff_list, IPoolItemInterface
    {
        public tcp_sack_block_wire[] sp_wire_cache = null;
        public tcphdr tcphdr_cache = null;
        public tcp_skb_cb tcp_skb_cb_cache = null;

        //skb->ooo_okay 是一个标志位，用于指示该 sk_buff 是否可以被作为乱序数据段接收并处理。
        //如果设置为 true，则表示可以安全地接收和处理该乱序段；
        //如果为 false，则可能需要等待直到有更多空间或重传队列清理完毕。
        public bool ooo_okay;

        //skb->tstamp 是 Linux 内核中 struct sk_buff（套接字缓冲区）结构体的一个成员，用于存储与数据包相关的时间戳。
        //这个时间戳通常在数据包到达或发送时记录，以提供关于网络性能、延迟和其他时间敏感信息的统计数据
        public long tstamp; //这个是 纳秒
        public skb_tstamp_type tstamp_type;
        public long skb_mstamp_ns; //微秒
        public uint tskey;
        public byte tx_flags;

        public readonly list_head tcp_tsorted_anchor = null;
        public readonly rb_node rbnode = null;

        public readonly byte[] mBuffer = new byte[1500];
        public int nBufferLength;
        public int nBufferOffset;

        public sk_buff()
        {
            rbnode = new rb_node(this);
            tcp_tsorted_anchor = new list_head(this);
        }

        public void Reset()
        {
            Array.Clear(this.mBuffer, 0, LinuxTcpFunc.max_tcphdr_length);
            sp_wire_cache = null;
            tcphdr_cache = null;
            tcp_skb_cb_cache = null;
            ooo_okay = false;
            tstamp = 0;
            tstamp_type = 0;
            skb_mstamp_ns = 0;
            tskey = 0;
            tx_flags = 0;
            nBufferLength = 0;
            nBufferOffset = 0;
        }

        public Span<byte> GetSendBuffer()
        {
            return mBuffer.AsSpan().Slice(nBufferOffset, nBufferLength);
        }

        //用户调用这个方法的时候，此Buff长度已经移除头部长度了
        public Span<byte> GetTcpReceiveBufferSpan()
        {
            return mBuffer.AsSpan().Slice(nBufferOffset, nBufferLength);
        }

        public Span<byte> GetTailRoomSpan()
        {
            return mBuffer.AsSpan().Slice(nBufferOffset + nBufferLength);
        }
    }

    internal static partial class LinuxTcpFunc
    {
        static bool skb_queue_empty(sk_buff_head list)
        {
	        return list.next == list;
        }

        static uint skb_queue_len(sk_buff_head list_)
        {
            return list_.qlen;
        }

        static void __skb_insert(sk_buff newsk, sk_buff prev, sk_buff next, sk_buff_head list)
        {
            newsk.next = next;
            newsk.prev = prev;

            next.prev = newsk;
            prev.next = newsk;
            list.qlen++;
        }

        static void __skb_queue_before(sk_buff_head list, sk_buff next, sk_buff newsk)
        {
            __skb_insert(newsk, next.prev, next, list);
        }

        static void __skb_queue_tail(sk_buff_head list, sk_buff newsk)
        {
            __skb_queue_before(list, list, newsk);
        }

        static bool skb_queue_is_last(sk_buff_head list, sk_buff skb)
        {
            return skb.next == list;
        }

        //这是一个循环列表
        static sk_buff skb_peek_tail(sk_buff_head list_)
        {
            sk_buff skb = list_.prev;
            if (skb == list_)
            {
                skb = null;
            }
            return skb;
        }

        public static sk_buff skb_peek(sk_buff_head list_)
        {
            sk_buff skb = list_.next;
            if (skb == list_)
            {
                skb = null;
            }
            return skb;
        }

        static sk_buff __skb_dequeue(sk_buff_head list)
        {
	        sk_buff skb = skb_peek(list);
            if (skb != null)
            {
                __skb_unlink(skb, list);
            }
	        return skb;
        }

        static void __skb_unlink(sk_buff skb, sk_buff_head list)
        {
            list.qlen--;
            sk_buff next = skb.next;
            sk_buff prev = skb.prev;

            skb.next = null;
            skb.prev = null;

            next.prev = prev;
            prev.next = next;
        }

        public static sk_buff rb_to_skb(rb_node node)
        {
            return rb_entry(node);
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

        //计算线性部分长度
        //不是头部长度哦
        public static int skb_headlen(sk_buff skb)
        {
            return tcp_hdrlen(skb);
        }

        public static void skb_set_delivery_time(sk_buff skb, long kt, skb_tstamp_type tstamp_type)
        {
            skb.tstamp = kt;

            if (kt > 0)
            {
                skb.tstamp_type = tstamp_type;
            }
            else
            {
                skb.tstamp_type = skb_tstamp_type.SKB_CLOCK_REALTIME;
            }
        }

        public static int skb_shift(sk_buff tgt, sk_buff skb, int shiftlen)
        {
            return 0;
        }

        static void consume_skb(tcp_sock tp, sk_buff skb)
        {
            kfree_skb(tp, skb);
        }

        static void sk_mem_charge(sock sk, int size)
        {
            sk_forward_alloc_add(sk, -size);
        }

        static int skb_end_offset(sk_buff skb)
        {
            return skb.mBuffer.Length;
        }

        static int SKB_TRUESIZE(int X)
        {
            return X;
        }

        static void __skb_queue_after(sk_buff_head list, sk_buff prev, sk_buff newsk)
        {
            __skb_insert(newsk, prev, ((sk_buff_list)prev).next, list);
        }
        
        static void skb_len_add(sk_buff skb, int delta)
        {
            skb.nBufferLength += delta;
        }

        static int skb_copy_datagram_msg(sk_buff from, int offset, ReadOnlySpan<byte> msg, int size)
        {
            //return skb_copy_datagram_iter(from, offset, &msg->msg_iter, size);
            return 0;
        }

        static void kfree_skb(tcp_sock tp, sk_buff skb)
        {
            tp.mClientPeer.GetObjectPoolManager().Skb_Recycle(skb);
        }

        //用于计算 sk_buff 中尾部的可用空间。
        static int skb_tailroom(sk_buff skb)
        {
            return skb.mBuffer.Length - skb.nBufferLength - skb.nBufferOffset;
        }

        static bool skb_try_coalesce(sk_buff to, sk_buff from)
        {
            if(from.nBufferLength <= skb_tailroom(to))
            {
                from.GetTcpReceiveBufferSpan().CopyTo(to.GetTailRoomSpan());
                to.nBufferLength += from.nBufferLength;
                return true;
            }
            return false;
        }

        //__skb_tstamp_tx 是 Linux 内核中用于处理套接字缓冲区（SKB, socket buffer）时间戳的一个函数。
        //它主要用于记录数据包发送的时间戳信息，这对于网络性能监控、延迟测量和某些协议特性（如TCP的精确往返时间RTT计算）非常重要。
        //__skb_tstamp_tx 函数的主要作用是为即将发送的数据包设置一个高精度的时间戳.
        //这个时间戳通常是在数据包被实际提交给网络接口卡（NIC）进行发送时获取的，确保了时间戳的准确性。
        //通过这种方式，Linux内核能够提供关于数据包发送时间的详细信息，这对于分析网络性能和调试网络问题非常有用。
        static void __skb_tstamp_tx(sk_buff orig_skb, sk_buff ack_skb, tcp_sock tp, int tstype)
        {
            orig_skb.tstamp = tcp_jiffies32;
            orig_skb.tstamp_type = (byte)skb_tstamp_type.SKB_CLOCK_REALTIME;
        }

        static bool skb_can_shift(sk_buff skb)
        {
            return skb_headlen(skb) == 0;
        }

        static void sk_skb_reason_drop(tcp_sock tp, sk_buff skb, int reason)
        {
            kfree_skb(tp, skb);
        }

        static Span<byte> skb_transport_header(sk_buff skb)
        {
            return skb.mBuffer.AsSpan().Slice(skb.nBufferOffset);
        }

        static void __skb_queue_head_init(sk_buff_head list)
        {
            list.prev = list.next = list; //都指向链表头
            list.qlen = 0;
        }

        static void skb_queue_head_init(sk_buff_head list)
        {
            __skb_queue_head_init(list);
        }

        public static sk_buff build_skb(sk_buff skb, ReadOnlySpan<byte> data)
        {
            data.CopyTo(skb.mBuffer);
            skb.nBufferOffset = 0;
            skb.nBufferLength = data.Length;
            return skb;
        }

        //接收数据后，头部不需要
        static void skb_pull(sk_buff skb, int len)
        {
            skb.nBufferLength -= len;
            skb.nBufferOffset += len;
        }

        //发送数据后，头部需要
        static void skb_push(sk_buff skb, int len)
        {
            skb.nBufferLength += len;
            skb.nBufferOffset -= len;
        }
    }
}
