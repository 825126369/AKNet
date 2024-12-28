/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        //RACK（Recent ACKnowledgment），即最近确认，是一种用于TCP协议的丢包检测算法，它通过时间维度来判断数据包是否丢失。
        //与传统的基于重复ACK或SACK（选择性确认）的方法不同，RACK利用了最新的ACK信息，记录被该ACK确认的数据包的发送时间，并以此为基础推断哪些之前发送的数据包可能已经丢失。
        //这种方法不仅能够更准确地识别丢包，而且还能有效地处理重传后的再次丢失问题以及尾部丢包的情况1。
        //RACK的基本思想
        //RACK的核心理念是基于时间而非序列号来进行丢包检测。
        //每当接收到一个新的ACK或SACK时，RACK会更新一个全局变量 rack.xmit_time，该变量表示接收方已经确认的最晚发送的数据包的时间戳。
        //对于每个未被确认的数据包，如果其发送时间加上一个预设的时间窗口（reo_wnd）早于 rack.xmit_time，那么这个数据包就被认为是丢失了

        
        //tcp_rack_reo_wnd 函数是Linux内核中TCP RACK（Recent ACKnowledgment）算法的一部分，用于计算乱序窗口（reordering window）。
        //该函数根据当前套接字的状态以及网络环境来动态调整这个窗口大小，从而帮助RACK更准确地检测丢包。
        //乱序窗口的作用在于允许一定范围内的数据包乱序到达而不立即触发重传机制，这对于提高TCP在复杂网络条件下的性能非常重要
        static uint tcp_rack_reo_wnd(tcp_sock tp)
        {
	        if (tp.reord_seen == 0) 
            {
                if (tp.icsk_ca_state >= (byte)tcp_ca_state.TCP_CA_Recovery)
                {
                    return 0;
                }

                if (tp.sacked_out >= tp.reordering && !BoolOk(sock_net(tp).ipv4.sysctl_tcp_recovery & tcp_sock.TCP_RACK_NO_DUPTHRESH))
                {
                    return 0;
                }
	        }

	        return (uint)Math.Min((tcp_min_rtt(tp) >> 2) * tp.rack.reo_wnd_steps, tp.srtt_us >> 3);
        }

        public static int tcp_rack_skb_timeout(tcp_sock tp, sk_buff skb, uint reo_wnd)
        {
            return (int)(tp.rack.rtt_us + reo_wnd - tcp_stamp_us_delta(tp.tcp_mstamp, tcp_skb_timestamp_us(skb)));
        }

        static void tcp_rack_reo_timeout(tcp_sock tp)
        {
            long prior_inflight;
            uint lost = tp.lost;

            prior_inflight = tcp_packets_in_flight(tp);
            tcp_rack_detect_loss(tp);
            if (prior_inflight != tcp_packets_in_flight(tp)) 
            {
                if (tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_Recovery) 
                {
                    tcp_enter_recovery(tp, false);
                    if (tp.icsk_ca_ops.cong_control == null)
                    {
                        tcp_cwnd_reduction(tp, 1, (int)(tp.lost - lost), 0);
                    }
                }
                tcp_xmit_retransmit_queue(tp);
            }
            if (tp.icsk_pending != tcp_sock.ICSK_TIME_RETRANS)
            {
                tcp_rearm_rto(tp);
            }
        }

        static void tcp_rack_detect_loss(tcp_sock tp)
        {
            sk_buff skb, n;
            uint reo_wnd;

            long reo_timeout = 0;
            reo_wnd = tcp_rack_reo_wnd(tp);
            
            LinkedListNode<sk_buff> skbNode = null;
            LinkedListNode<sk_buff> nNext = null;
            for (skbNode = tp.tsorted_sent_queue.First, nNext = skbNode.Next; skbNode != tp.tsorted_sent_queue.First; skbNode = nNext, nNext = list_next_entry(n, member))
            {
                skb = skbNode.Value;
                tcp_skb_cb scb = TCP_SKB_CB(skb);
                int remaining;

                if (BoolOk(scb.sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_LOST) && !BoolOk(scb.sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS))
                {
                    continue;
                }

                if (!tcp_skb_sent_after(tp->rack.mstamp, tcp_skb_timestamp_us(skb), tp.rack.end_seq, scb.end_seq))
                {
                    break;
                }

                remaining = tcp_rack_skb_timeout(tp, skb, reo_wnd);
                if (remaining <= 0)
                {
                    tcp_mark_skb_lost(sk, skb);

                    list_del_init(&skb->tcp_tsorted_anchor);
                }
                else
                {
                    /* Record maximum wait time */
                    *reo_timeout = max_t(u32, *reo_timeout, remaining);
                }
            }
        }

    }
}
