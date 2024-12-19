using AKNet.LinuxTcp;
using System;
using System.Security.Cryptography;

namespace AKNet.LinuxTcp
{
	internal static partial class LinuxTcpFunc
	{
		public static long tcp_jiffies32
		{
			get { return mStopwatch.ElapsedMilliseconds; }
		}

		public static void tcp_chrono_stop(tcp_sock tp, tcp_chrono type)
		{
			if (tcp_rtx_and_write_queues_empty(tp))
			{
				tcp_chrono_set(tp, tcp_chrono.TCP_CHRONO_UNSPEC);
			}
			else if (type == tp.chrono_type)
			{
				tcp_chrono_set(tp, tcp_chrono.TCP_CHRONO_BUSY);
			}
		}

		public static void tcp_chrono_set(tcp_sock tp, tcp_chrono newType)
		{
			long now = tcp_jiffies32;
			tcp_chrono old = tp.chrono_type;

			if (old > tcp_chrono.TCP_CHRONO_UNSPEC)
			{
				tp.chrono_stat[(int)old - 1] += now - tp.chrono_start;
			}

			tp.chrono_start = now;
			tp.chrono_type = newType;
		}

		public static void tcp_mstamp_refresh(tcp_sock tp)
		{
			tp.tcp_mstamp = tcp_jiffies32;
		}

		public static void tcp_send_ack(tcp_sock tp)
		{
			tcp_send_ack(tp, tp.rcv_nxt);
		}

		public static void tcp_send_ack(tcp_sock tp, uint rcv_nxt)
		{
			
		}

		public static int tcp_retransmit_skb(tcp_sock tp, sk_buff skb, int segs)
		{
			int err = __tcp_retransmit_skb(tp, skb, segs);

			if (err == 0)
			{
				TCP_SKB_CB(skb).sacked |= (byte)tcp_skb_cb_sacked_flags.TCPCB_RETRANS;
				tp.retrans_out += (uint)tcp_skb_pcount(skb);
			}

			if (tp.retrans_stamp == 0)
			{
				tp.retrans_stamp = tcp_skb_timestamp_ts(tp.tcp_usec_ts, skb);
			}
			if (tp.undo_retrans < 0)
			{
				tp.undo_retrans = 0;
			}
			tp.undo_retrans += tcp_skb_pcount(skb);
			return err;
		}
		
        public static int __tcp_retransmit_skb(tcp_sock tp, sk_buff skb, int segs)
		{
			uint cur_mss;
			int diff, len, err;
			int avail_wnd;

			if (tp.icsk_mtup.probe_size > 0)
			{
				tp.icsk_mtup.probe_size = 0;
			}

			if (skb_still_in_host_queue(tp, skb))
			{
				return -ErrorCode.EBUSY;
			}

		start:
			if (before(TCP_SKB_CB(skb).seq, tp.snd_una))
			{
				if ((TCP_SKB_CB(skb).tcp_flags & tcp_sock.TCPHDR_SYN) > 0) 
				{
					TCP_SKB_CB(skb).tcp_flags = (byte)(TCP_SKB_CB(skb).tcp_flags & ~tcp_sock.TCPHDR_SYN);
					TCP_SKB_CB(skb).seq++;
					goto start;
				}
				if (before(TCP_SKB_CB(skb).end_seq, tp.snd_una)) 
				{
					return -ErrorCode.EINVAL;
				}
				if (tcp_trim_head(sk, skb, tp.snd_una - TCP_SKB_CB(skb).seq))
				{
					return -ErrorCode.ENOMEM;
				}
			}
			
			cur_mss = tcp_current_mss(tp);
			avail_wnd = tcp_wnd_end(tp) - TCP_SKB_CB(skb).seq;
			if (avail_wnd <= 0)
			{
				if (TCP_SKB_CB(skb).seq != tp.snd_una)
				{
					return -ErrorCode.EAGAIN;
				}
				avail_wnd = cur_mss;
			}

			len = cur_mss * segs;
			if (len > avail_wnd)
			{
				len = rounddown(avail_wnd, (int)cur_mss);
				if (len == 0)
				{
					len = avail_wnd;
				}
			}

			if (skb.len > len)
			{
				if (tcp_fragment(sk, TCP_FRAG_IN_RTX_QUEUE, skb, len, cur_mss, GFP_ATOMIC))
				{
					return -ErrorCode.ENOMEM;
				}
			}
			else
			{
				if (skb_unclone_keeptruesize(skb, GFP_ATOMIC))
					return -ENOMEM;

				diff = tcp_skb_pcount(skb);
				tcp_set_skb_tso_segs(skb, cur_mss);
				diff -= tcp_skb_pcount(skb);
				if (diff)
					tcp_adjust_pcount(sk, skb, diff);
				avail_wnd = min_t(int, avail_wnd, cur_mss);
				if (skb->len < avail_wnd)
					tcp_retrans_try_collapse(sk, skb, avail_wnd);
			}

			/* RFC3168, section 6.1.1.1. ECN fallback */
			if ((TCP_SKB_CB(skb)->tcp_flags & TCPHDR_SYN_ECN) == TCPHDR_SYN_ECN)
				tcp_ecn_clear_syn(sk, skb);

			/* Update global and local TCP statistics. */
			segs = tcp_skb_pcount(skb);
			TCP_ADD_STATS(sock_net(sk), TCP_MIB_RETRANSSEGS, segs);
			if (TCP_SKB_CB(skb)->tcp_flags & TCPHDR_SYN)
				__NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPSYNRETRANS);
			tp->total_retrans += segs;
			tp->bytes_retrans += skb->len;

			/* make sure skb->data is aligned on arches that require it
			 * and check if ack-trimming & collapsing extended the headroom
			 * beyond what csum_start can cover.
			 */
			if (unlikely((NET_IP_ALIGN && ((unsigned long)skb->data & 3)) ||
					 skb_headroom(skb) >= 0xFFFF)) {

					struct sk_buff *nskb;

			tcp_skb_tsorted_save(skb) {
				nskb = __pskb_copy(skb, MAX_TCP_HEADER, GFP_ATOMIC);
				if (nskb)
				{
					nskb->dev = NULL;
					err = tcp_transmit_skb(sk, nskb, 0, GFP_ATOMIC);
				}
				else
				{
					err = -ENOBUFS;
				}
			}
			tcp_skb_tsorted_restore(skb);

			if (!err)
			{
				tcp_update_skb_after_send(sk, skb, tp->tcp_wstamp_ns);
				tcp_rate_skb_sent(sk, skb);
			}
				} else
			{
				err = tcp_transmit_skb(sk, skb, 1, GFP_ATOMIC);
			}

			if (BPF_SOCK_OPS_TEST_FLAG(tp, BPF_SOCK_OPS_RETRANS_CB_FLAG))
				tcp_call_bpf_3arg(sk, BPF_SOCK_OPS_RETRANS_CB,
						  TCP_SKB_CB(skb)->seq, segs, err);

			if (likely(!err))
			{
				trace_tcp_retransmit_skb(sk, skb);
			}
			else if (err != -EBUSY)
			{
				NET_ADD_STATS(sock_net(sk), LINUX_MIB_TCPRETRANSFAIL, segs);
			}
			
			TCP_SKB_CB(skb).sacked |= TCPCB_EVER_RETRANS;
			return err;
		}
			
		public static bool skb_still_in_host_queue(tcp_sock tp, sk_buff skb)
		{
			if (skb_fclone_busy(tp, skb))
			{
				tp.sk_tsq_flags |= tsq_enum.TSQ_THROTTLED;
				if (skb_fclone_busy(tp, skb))
				{
					NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPSPURIOUS_RTX_HOSTQUEUES, 1);
					return true;
				}
			}
			return false;
		}
		
		public static int tcp_trim_head(tcp_sock tp, sk_buff skb, uint len)
		{
			TCP_SKB_CB(skb).seq += len;
			if (tcp_skb_pcount(skb) > 1)
			{
				tcp_set_skb_tso_segs(skb, tcp_skb_mss(skb));
			}
			return 0;
		}
		
		public static int tcp_set_skb_tso_segs(sk_buff skb, uint mss_now)
		{
			int tso_segs;

			if (skb.len <= mss_now)
			{
				TCP_SKB_CB(skb).tcp_gso_size = 0;
				tcp_skb_pcount_set(skb, 1);
				return 1;
			}

			TCP_SKB_CB(skb).tcp_gso_size = mss_now;
			tso_segs = (int)Math.Round(skb.len / (float)mss_now);
			tcp_skb_pcount_set(skb, tso_segs);
			return tso_segs;
		}
		
		public static uint tcp_current_mss(tcp_sock tp)
		{
			uint mss_now = tp.mss_cache;
			return mss_now;
		}

		public static int tcp_fragment(tcp_sock tp, tcp_queue tcp_queue,sk_buff skb, uint len, uint mss_now, uint gfp)
		{
			sk_buff buff;
			int old_factor;
			long limit;
			int nlen;
			byte flags;

			if (WARN_ON(len > skb.len))
			{
				return -ErrorCode.EINVAL;
			}
			
			limit = tp.sk_sndbuf + 2 * SKB_TRUESIZE(GSO_LEGACY_MAX_SIZE);
			if ((sk.sk_wmem_queued >> 1) > limit &&
					 tcp_queue != tcp_queue.TCP_FRAG_IN_WRITE_QUEUE &&
					 skb != tcp_rtx_queue_head(sk) &&
					 skb != tcp_rtx_queue_tail(sk)))
			{
				NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPWQUEUETOOBIG, 1);
				return -ErrorCode.ENOMEM;
			}

			if (skb_unclone_keeptruesize(skb, gfp))
				return -ENOMEM;

			/* Get a new skb... force flag on. */
			buff = tcp_stream_alloc_skb(sk, gfp, true);
			if (!buff)
				return -ENOMEM; /* We'll just try again later. */
			skb_copy_decrypted(buff, skb);
			mptcp_skb_ext_copy(buff, skb);

			sk_wmem_queued_add(sk, buff->truesize);
			sk_mem_charge(sk, buff->truesize);
			nlen = skb->len - len;
			buff->truesize += nlen;
			skb->truesize -= nlen;

			/* Correct the sequence numbers. */
			TCP_SKB_CB(buff)->seq = TCP_SKB_CB(skb)->seq + len;
			TCP_SKB_CB(buff)->end_seq = TCP_SKB_CB(skb)->end_seq;
			TCP_SKB_CB(skb)->end_seq = TCP_SKB_CB(buff)->seq;

			/* PSH and FIN should only be set in the second packet. */
			flags = TCP_SKB_CB(skb)->tcp_flags;
			TCP_SKB_CB(skb)->tcp_flags = flags & ~(TCPHDR_FIN | TCPHDR_PSH);
			TCP_SKB_CB(buff)->tcp_flags = flags;
			TCP_SKB_CB(buff)->sacked = TCP_SKB_CB(skb)->sacked;
			tcp_skb_fragment_eor(skb, buff);

			skb_split(skb, buff, len);

			skb_set_delivery_time(buff, skb->tstamp, SKB_CLOCK_MONOTONIC);
			tcp_fragment_tstamp(skb, buff);

			old_factor = tcp_skb_pcount(skb);

			/* Fix up tso_factor for both original and new SKB.  */
			tcp_set_skb_tso_segs(skb, mss_now);
			tcp_set_skb_tso_segs(buff, mss_now);

			/* Update delivered info for the new segment */
			TCP_SKB_CB(buff)->tx = TCP_SKB_CB(skb)->tx;

			/* If this packet has been sent out already, we must
			 * adjust the various packet counters.
			 */
			if (!before(tp->snd_nxt, TCP_SKB_CB(buff)->end_seq))
			{
				int diff = old_factor - tcp_skb_pcount(skb) -
					tcp_skb_pcount(buff);

				if (diff)
					tcp_adjust_pcount(sk, skb, diff);
			}

			/* Link BUFF into the send queue. */
			__skb_header_release(buff);
			tcp_insert_write_queue_after(skb, buff, sk, tcp_queue);
			if (tcp_queue == TCP_FRAG_IN_RTX_QUEUE)
				list_add(&buff->tcp_tsorted_anchor, &skb->tcp_tsorted_anchor);

			return 0;
		}
		
	}
}

