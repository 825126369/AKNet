/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.LinuxTcp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace AKNet.LinuxTcp
{
    internal class tcp_out_options
    {
        public ushort options;        /* bit field of OPTION_* */
        public ushort mss;        /* 0 to disable */
        public byte ws;          /* window scale, 0 to disable */
        public byte num_sack_blocks; /* number of SACK blocks to include */
        public byte hash_size;       /* bytes in hash_location */
        public byte bpf_opt_len;     /* length of BPF hdr option */
        public byte[] hash_location;    /* temporary pointer, overloaded */
        public uint tsval, tsecr; /* need to include OPTION_TS */
	}

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
				if (tcp_trim_head(tp, skb, tp.snd_una - TCP_SKB_CB(skb).seq) > 0)
				{
					return -ErrorCode.ENOMEM;
				}
			}

			cur_mss = tcp_current_mss(tp);
			avail_wnd = (int)(tcp_wnd_end(tp) - TCP_SKB_CB(skb).seq);
			if (avail_wnd <= 0)
			{
				if (TCP_SKB_CB(skb).seq != tp.snd_una)
				{
					return -ErrorCode.EAGAIN;
				}
				avail_wnd = (int)cur_mss;
			}

			len = (int)cur_mss * segs;
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
				tcp_fragment(tp, tcp_queue.TCP_FRAG_IN_RTX_QUEUE, skb, len, cur_mss);
			}
			else
			{
				diff = tcp_skb_pcount(skb);
				tcp_set_skb_tso_segs(skb, cur_mss);
				diff -= tcp_skb_pcount(skb);
				if (diff > 0)
				{
					tcp_adjust_pcount(tp, skb, diff);
				}
				avail_wnd = Math.Min(avail_wnd, (int)cur_mss);
				if (skb.len < avail_wnd)
				{
					tcp_retrans_try_collapse(tp, skb, avail_wnd);
				}
			}

			if ((TCP_SKB_CB(skb).tcp_flags & tcp_sock.TCPHDR_SYN_ECN) == tcp_sock.TCPHDR_SYN_ECN)
			{
				tcp_ecn_clear_syn(tp, skb);
			}

			/* Update global and local TCP statistics. */
			segs = tcp_skb_pcount(skb);
			TCP_ADD_STATS(sock_net(tp), TCPMIB.TCP_MIB_RETRANSSEGS, segs);
			if ((TCP_SKB_CB(skb).tcp_flags & tcp_sock.TCPHDR_SYN) > 0)
			{
				NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPSYNRETRANS, 1);
			}
			tp.total_retrans += segs;
			tp.bytes_retrans += skb.len;
			err = tcp_transmit_skb(tp, skb, 1);
			if (err == 0)
			{

			}
			else if (err != -ErrorCode.EBUSY)
			{
				NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPRETRANSFAIL, segs);
			}

			TCP_SKB_CB(skb).sacked = (byte)(TCP_SKB_CB(skb).sacked | (byte)tcp_skb_cb_sacked_flags.TCPCB_EVER_RETRANS);
			return err;
		}

		public static int tcp_transmit_skb(tcp_sock tp, sk_buff skb, int clone_it)
		{
			return __tcp_transmit_skb(tp, skb, clone_it, tp.rcv_nxt);
		}


		static int __tcp_transmit_skb(tcp_sock tp, sk_buff skb, int clone_it, uint rcv_nxt)
		{
			tcp_skb_cb tcb;
			sk_buff oskb = null;
			tcphdr th;
			long prior_wstamp;
			int err;
			uint tcp_options_size = 0;
			uint tcp_header_size;
			tcp_out_options opts = null;

			BUG_ON(skb == null || tcp_skb_pcount(skb) == 0);
			prior_wstamp = tp.tcp_wstamp_ns;

			tp.tcp_wstamp_ns = Math.Max(tp.tcp_wstamp_ns, tp.tcp_clock_cache);
			skb_set_delivery_time(skb, tp.tcp_wstamp_ns, skb_tstamp_type.SKB_CLOCK_MONOTONIC);
			if (clone_it > 0)
			{
				oskb = skb;
				skb = skb_clone(oskb);
				if (skb == null)
				{
					return -ErrorCode.ENOBUFS;
				}
				skb.dev = null;
			}
			tcb = TCP_SKB_CB(skb);


			if ((tcb.tcp_flags & (byte)tcp_sock.TCPHDR_SYN) > 0)
			{
			}
			else
			{
				tcp_options_size = tcp_established_options(tp, skb, opts);
				if (tcp_skb_pcount(skb) > 1)
				{
					tcb.tcp_flags |= tcp_sock.TCPHDR_PSH;
				}
			}
			tcp_header_size = tcp_options_size + sizeof_tcphdr;

			skb.ooo_okay = tcp_rtx_queue_empty(tp);
			skb.sk = tp;

			skb_set_dst_pending_confirm(skb, tp.sk_dst_pending_confirm);

			th = new tcphdr();
			th.source = tp.inet_sport;
			th.dest = tp.inet_dport;
			th.seq = htons(tcb.seq);
			th.ack_seq = htons(rcv_nxt);
			//*(((__be16*)th) + 6) = htons(((tcp_header_size >> 2) << 12) | tcb.tcp_flags);
			//th.mBuff[2 * 6] = htons(((tcp_header_size >> 2) << 12) | tcb.tcp_flags);

			th.check = 0;
			th.urg_ptr = 0;

			if (tcp_urg_mode(tp) && before(tcb.seq, tp.snd_up))
			{
				if (before(tp.snd_up, tcb.seq + 0x10000))
				{
					th.urg_ptr = (ushort)htons(tp.snd_up - tcb.seq);
					th.urg = 1;
				}
				else if (after(tcb.seq + 0xFFFF, tp.snd_nxt))
				{
					th.urg_ptr = (ushort)htons(0xFFFF);
					th.urg = 1;
				}
			}

			skb_shinfo(skb).gso_type = tp.sk_gso_type;
			if ((tcb.tcp_flags & tcp_sock.TCPHDR_SYN) == 0)
			{

			}
			else
			{
				th.window = (ushort)htons(Math.Min(tp.rcv_wnd, 65535));
			}

			//tcp_options_write(th, tp, null, opts, key);

			//if (tcp_key_is_md5(&key))
			//{
			//	sk_gso_disable(sk);
			//	tp->af_specific->calc_md5_hash(opts.hash_location,key.md5_key, sk, skb);
			//}
			//else if (tcp_key_is_ao(&key))
			//{
			//	int err;

			//	err = tcp_ao_transmit_skb(sk, skb, key.ao_key, th,
			//				  opts.hash_location);
			//	if (err)
			//	{
			//		kfree_skb_reason(skb, SKB_DROP_REASON_NOT_SPECIFIED);
			//		return -ENOMEM;
			//	}
			//}

			/* BPF prog is the last one writing header option */
			//bpf_skops_write_hdr_opt(sk, skb, NULL, NULL, 0, &opts);

			tcp_v4_send_check(tp, skb);
			if ((tcb.tcp_flags & tcp_sock.TCPHDR_ACK) > 0)
			{
				tcp_event_ack_sent(tp, rcv_nxt);
			}

			if (skb.len != tcp_header_size)
			{
				tcp_event_data_sent(tp);
				tp.data_segs_out += (uint)tcp_skb_pcount(skb);
				tp.bytes_sent += skb.len - tcp_header_size;
			}

			if (after(tcb.end_seq, tp.snd_nxt) || tcb.seq == tcb.end_seq)
			{
				TCP_ADD_STATS(sock_net(tp), TCPMIB.TCP_MIB_OUTSEGS, tcp_skb_pcount(skb));
			}

			tp.segs_out += (uint)tcp_skb_pcount(skb);
			skb_set_hash_from_sk(skb, tp);

			skb_shinfo(skb).gso_segs = (ushort)tcp_skb_pcount(skb);
			skb_shinfo(skb).gso_size = (ushort)tcp_skb_mss(skb);

			tcp_add_tx_delay(skb, tp);

			err = ip_queue_xmit(tp, skb, tp.cork.fl);

			if (err > 0)
			{
				tcp_enter_cwr(tp);
			}
			return err;
		}

		public static bool skb_still_in_host_queue(tcp_sock tp, sk_buff skb)
		{
			if (skb_fclone_busy(tp, skb))
			{
				tp.sk_tsq_flags |= (byte)tsq_enum.TSQ_THROTTLED;
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
				tcp_set_skb_tso_segs(skb, (uint)tcp_skb_mss(skb));
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

			TCP_SKB_CB(skb).tcp_gso_size = (int)mss_now;
			tso_segs = (int)Math.Round(skb.len / (float)mss_now);
			tcp_skb_pcount_set(skb, tso_segs);
			return tso_segs;
		}

		public static uint tcp_current_mss(tcp_sock tp)
		{
			uint mss_now = tp.mss_cache;
			return mss_now;
		}

		public static int tcp_fragment(tcp_sock tp, tcp_queue tcp_queue, sk_buff skb, int len, uint mss_now)
		{
			sk_buff buff;
			int old_factor;
			long limit;
			int nlen;
			byte flags;

			if (WARN_ON(len > skb.len))
			{
				return;
			}

			limit = tp.sk_sndbuf;
			if ((tp.sk_wmem_queued >> 1) > limit && tcp_queue != tcp_queue.TCP_FRAG_IN_WRITE_QUEUE &&
					 skb != tcp_rtx_queue_head(tp) &&
					 skb != tcp_rtx_queue_tail(tp))
			{
				NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPWQUEUETOOBIG, 1);
				return;
			}

			buff = new sk_buff();
			if (buff == null)
			{
				return;
			}

			skb_copy_decrypted(buff, skb);
			nlen = skb.len - len;

			TCP_SKB_CB(buff).seq = TCP_SKB_CB(skb).seq + (uint)len;
			TCP_SKB_CB(buff).end_seq = TCP_SKB_CB(skb).end_seq;
			TCP_SKB_CB(skb).end_seq = TCP_SKB_CB(buff).seq;

			flags = TCP_SKB_CB(skb).tcp_flags;
			TCP_SKB_CB(skb).tcp_flags = (byte)(flags & ~(tcp_sock.TCPHDR_FIN | tcp_sock.TCPHDR_PSH));
			TCP_SKB_CB(buff).tcp_flags = flags;
			TCP_SKB_CB(buff).sacked = TCP_SKB_CB(skb).sacked;
			tcp_skb_fragment_eor(skb, buff);

			skb_split(skb, buff, len);

			skb_set_delivery_time(buff, skb.tstamp, skb_tstamp_type.SKB_CLOCK_MONOTONIC);
			tcp_fragment_tstamp(skb, buff);

			old_factor = tcp_skb_pcount(skb);

			tcp_set_skb_tso_segs(skb, mss_now);
			tcp_set_skb_tso_segs(buff, mss_now);

			TCP_SKB_CB(buff).tx = TCP_SKB_CB(skb).tx;
			if (!before(tp.snd_nxt, TCP_SKB_CB(buff).end_seq))
			{
				int diff = old_factor - tcp_skb_pcount(skb) - tcp_skb_pcount(buff);
				if (diff > 0)
				{
					tcp_adjust_pcount(tp, skb, diff);
				}
			}

			__skb_header_release(buff);
			tcp_insert_write_queue_after(skb, buff, tp, tcp_queue);
			if (tcp_queue == tcp_queue.TCP_FRAG_IN_RTX_QUEUE)
			{
				//skb.tcp_tsorted_anchor.AddAfter(buff.tcp_tsorted_anchor);
			}
			return 0;
		}

		public static void tcp_skb_fragment_eor(sk_buff skb, sk_buff skb2)
		{
			TCP_SKB_CB(skb2).eor = TCP_SKB_CB(skb).eor;
			TCP_SKB_CB(skb).eor = 0;
		}

		static void tcp_retrans_try_collapse(tcp_sock tp, sk_buff to, int space)
		{
			sk_buff skb = to;
			sk_buff tmp = null;
			bool first = true;

			if (sock_net(tp).ipv4.sysctl_tcp_retrans_collapse == 0)
			{
				return;
			}

			if ((TCP_SKB_CB(skb).tcp_flags & tcp_sock.TCPHDR_SYN) > 0)
			{
				return;
			}

			for (; (tmp = (skb != null ? skb_rb_next(tp.tcp_rtx_queue, skb) : null)) != null; skb = tmp)
			{
				if (!tcp_can_collapse(tp, skb))
				{
					break;
				}

				if (!tcp_skb_can_collapse(to, skb))
					break;

				space -= skb.len;

				if (first)
				{
					first = false;
					continue;
				}

				if (space < 0)
					break;

				if (after(TCP_SKB_CB(skb).end_seq, tcp_wnd_end(tp)))
					break;

				if (!tcp_collapse_retrans(tp, to))
					break;
			}
		}

		public static bool tcp_can_collapse(tcp_sock tp, sk_buff skb)
		{
			if (tcp_skb_pcount(skb) > 1)
			{
				return false;
			}

			if (skb_cloned(skb))
			{
				return false;
			}

			if (!skb_frags_readable(skb))
			{
				return false;
			}

			if ((TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED) > 0)
			{
				return false;
			}
			return true;
		}

		public static void tcp_ecn_clear_syn(tcp_sock tp, sk_buff skb)
		{
			if (sock_net(tp).ipv4.sysctl_tcp_ecn_fallback > 0)
			{
				TCP_SKB_CB(skb).tcp_flags = (byte)(TCP_SKB_CB(skb).tcp_flags & ~(tcp_sock.TCPHDR_ECE | tcp_sock.TCPHDR_CWR));
			}
		}

		public static bool tcp_has_tx_tstamp(sk_buff skb)
		{
			return (TCP_SKB_CB(skb).txstamp_ack > 0 || (skb_shinfo(skb).tx_flags & (byte)SKBTX_ANY_TSTAMP) > 0);
		}

		public static void tcp_fragment_tstamp(sk_buff skb, sk_buff skb2)
		{
			skb_shared_info shinfo = skb_shinfo(skb);
			if (tcp_has_tx_tstamp(skb) && !before(shinfo.tskey, TCP_SKB_CB(skb2).seq))
			{
				skb_shared_info shinfo2 = skb_shinfo(skb2);
				byte tsflags = (byte)(shinfo.tx_flags & SKBTX_ANY_TSTAMP);

				shinfo.tx_flags = (byte)(shinfo.tx_flags & ~tsflags);
				shinfo2.tx_flags |= tsflags;

				var temp = shinfo.tskey;
				shinfo.tskey = shinfo2.tskey;
				shinfo2.tskey = temp;

				TCP_SKB_CB(skb2).txstamp_ack = TCP_SKB_CB(skb).txstamp_ack;
				TCP_SKB_CB(skb).txstamp_ack = 0;
			}
		}

		static bool tcp_collapse_retrans(tcp_sock tp, sk_buff skb)
		{
			sk_buff next_skb = skb_rb_next(tp.tcp_rtx_queue, skb);
			int next_skb_size;
			next_skb_size = next_skb.len;

			BUG_ON(tcp_skb_pcount(skb) != 1 || tcp_skb_pcount(next_skb) != 1);

			if (next_skb_size > 0 && tcp_skb_shift(skb, next_skb, 1, next_skb_size) == 0)
			{
				return false;
			}

			tcp_highest_sack_replace(tp, next_skb, skb);

			TCP_SKB_CB(skb).end_seq = TCP_SKB_CB(next_skb).end_seq;
			TCP_SKB_CB(skb).tcp_flags |= TCP_SKB_CB(next_skb).tcp_flags;
			TCP_SKB_CB(skb).sacked = (byte)(TCP_SKB_CB(skb).sacked | (TCP_SKB_CB(next_skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_EVER_RETRANS));
			TCP_SKB_CB(skb).eor = TCP_SKB_CB(next_skb).eor;

			tcp_clear_retrans_hints_partial(tp);
			if (next_skb == tp.retransmit_skb_hint)
			{
				tp.retransmit_skb_hint = skb;
			}
			tcp_adjust_pcount(tp, next_skb, tcp_skb_pcount(next_skb));

			tcp_skb_collapse_tstamp(skb, next_skb);

			tcp_rtx_queue_unlink_and_free(next_skb, tp);
			return true;
		}

		static void tcp_adjust_pcount(tcp_sock tp, sk_buff skb, int decr)
		{
			tp.packets_out -= (uint)decr;

			if ((TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED) > 0)
			{
				tp.sacked_out -= (uint)decr;
			}

			if ((TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS) > 0)
			{
				tp.retrans_out -= (uint)decr;
			}
			if ((TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_LOST) > 0)
			{
				tp.lost_out -= (uint)decr;
			}

			if (tcp_is_reno(tp) && decr > 0)
			{
				tp.sacked_out -= (uint)Math.Min(tp.sacked_out, decr);
			}

			if (tp.lost_skb_hint != null && before(TCP_SKB_CB(skb).seq, TCP_SKB_CB(tp.lost_skb_hint).seq) &&
				((TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED) > 0))
			{
				tp.lost_cnt_hint -= decr;
			}

			WARN_ON(tcp_left_out(tp) > tp.packets_out);
		}

		public static bool tcp_urg_mode(tcp_sock tp)
		{
			return tp.snd_una != tp.snd_up;
		}

		//tcp_select_window 函数的主要任务是根据当前连接的状态、接收缓冲区的可用空间以及网络条件等因素，
		//动态地选择一个合适的TCP窗口大小。
		//这有助于：
		//提高吞吐量：确保发送方能够充分利用网络带宽。
		//减少延迟：避免不必要的等待时间，加快数据传输速度。
		//防止拥塞：通过合理控制窗口大小，避免网络过载。
		public static ushort tcp_select_window(tcp_sock tp)
		{
			net net = sock_net(tp);
			uint old_win = tp.rcv_wnd;
			uint cur_win, new_win;

			if ((tp.icsk_ack.pending & (byte)inet_csk_ack_state_t.ICSK_ACK_NOMEM) > 0)
			{
				return 0;
			}

			cur_win = tcp_receive_window(tp);
			new_win = __tcp_select_window(tp);
			if (new_win < cur_win)
			{
				if (net.ipv4.sysctl_tcp_shrink_window == 0 || tp.rx_opt.rcv_wscale == 0)
				{
					//接收方不应减小其通告的窗口大小
					if (new_win == 0)
					{
						NET_ADD_STATS(net, LINUXMIB.LINUX_MIB_TCPWANTZEROWINDOWADV, 1);
					}
					new_win = (uint)(cur_win * (1 << tp.rx_opt.rcv_wscale));
				}
			}

			tp.rcv_wnd = new_win;
			tp.rcv_wup = tp.rcv_nxt;

			new_win = (uint)Math.Min(new_win, ushort.MaxValue << tp.rx_opt.rcv_wscale);
			/* RFC1323 scaling applied */
			new_win >>= tp.rx_opt.rcv_wscale;

			/* If we advertise zero window, disable fast path. */
			if (new_win == 0)
			{
				tp.pred_flags = 0;
				if (old_win > 0)
				{
					NET_ADD_STATS(net, LINUXMIB.LINUX_MIB_TCPTOZEROWINDOWADV, 1);
				}
			}
			else if (old_win == 0)
			{
				NET_ADD_STATS(net, LINUXMIB.LINUX_MIB_TCPFROMZEROWINDOWADV, 1);
			}

			return (ushort)new_win;
		}

		static uint __tcp_select_window(tcp_sock tp)
		{
			net net = sock_net(tp);

			int mss = tp.icsk_ack.rcv_mss;
			int free_space = (int)tcp_space(tp);
			int allowed_space = (int)tcp_full_space(tp);
			int full_space, window;

			full_space = (int)Math.Min(tp.window_clamp, allowed_space);
			if (mss > full_space)
			{
				mss = full_space;
				if (mss <= 0)
				{
					return 0;
				}
			}

			if (net.ipv4.sysctl_tcp_shrink_window > 0 && tp.rx_opt.rcv_wscale > 0)
			{
				free_space = rounddown(free_space, 1 << tp.rx_opt.rcv_wscale);

				if (free_space < (full_space >> 1))
				{
					tp.icsk_ack.quick = 0;

					if (tcp_under_memory_pressure(tp))
					{
						tcp_adjust_rcv_ssthresh(tp);
					}

					if (free_space < (allowed_space >> 4) || free_space < mss || free_space < (1 << tp.rx_opt.rcv_wscale))
					{
						return 0;
					}
				}

				if (free_space > tp.rcv_ssthresh)
				{
					free_space = (int)tp.rcv_ssthresh;
					free_space = free_space * (1 << tp.rx_opt.rcv_wscale);
				}

				return (uint)free_space;
			}

			if (free_space < (full_space >> 1))
			{
				tp.icsk_ack.quick = 0;

				if (tcp_under_memory_pressure(tp))
				{
					tcp_adjust_rcv_ssthresh(tp);
				}

				free_space = rounddown(free_space, 1 << tp.rx_opt.rcv_wscale);
				if (free_space < (allowed_space >> 4) || free_space < mss)
				{
					return 0;
				}
			}

			if (free_space > tp.rcv_ssthresh)
			{
				free_space = (int)tp.rcv_ssthresh;
			}

			if (tp.rx_opt.rcv_wscale > 0)
			{
				window = free_space;
				window = window * (1 << tp.rx_opt.rcv_wscale);
			}
			else
			{
				window = (int)tp.rcv_wnd;
				if (window <= free_space - mss || window > free_space)
				{
					window = rounddown(free_space, mss);
				}
				else if (mss == full_space && free_space > window + (full_space >> 1))
				{
					window = free_space;
				}
			}

			return (uint)window;
		}

		static void tcp_event_ack_sent(tcp_sock tp, uint rcv_nxt)
		{
			if (tp.compressed_ack > 0)
			{
				NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPACKCOMPRESSED, tp.compressed_ack);
				tp.compressed_ack = 0;
				if (tp.compressed_ack_timer.TryToCancel())
				{
					__sock_put(tp);
				}
			}

			if (rcv_nxt != tp.rcv_nxt)
			{
				return;
			}

			tcp_dec_quickack_mode(tp);
			inet_csk_clear_xmit_timer(tp, tcp_sock.ICSK_TIME_DACK);
		}

		static uint tcp_established_options(tcp_sock tp, sk_buff skb, tcp_out_options opts)
		{
			uint size = 0;
			uint eff_sacks;
			opts.options = 0;

			//if (tcp_key_is_md5(key))
			//{
			//	opts->options |= OPTION_MD5;
			//	size += TCPOLEN_MD5SIG_ALIGNED;
			//} 
			//else if (tcp_key_is_ao(key))
			//{
			//	opts->options |= OPTION_AO;
			//	size += tcp_ao_len_aligned(key->ao_key);
			//}

			if (tp.rx_opt.tstamp_ok > 0)
			{
				opts.options |= (ushort)OPTION_TS;
				opts.tsval = (uint)(skb != null ? tcp_skb_timestamp_ts(tp.tcp_usec_ts, skb) + tp.tsoffset : 0);
				opts.tsecr = tp.rx_opt.ts_recent;
				size += tcp_sock.TCPOLEN_TSTAMP_ALIGNED;
			}

			//if (sk_is_mptcp(sk))
			//{
			//	unsigned int remaining = MAX_TCP_OPTION_SPACE - size;
			//	unsigned int opt_size = 0;

			//	if (mptcp_established_options(sk, skb, &opt_size, remaining,
			//					  &opts->mptcp))
			//	{
			//		opts->options |= OPTION_MPTCP;
			//		size += opt_size;
			//	}
			//}

			eff_sacks = (uint)(tp.rx_opt.num_sacks + tp.rx_opt.dsack);
			if (eff_sacks > 0)
			{
				uint remaining = MAX_TCP_OPTION_SPACE - size;
				if (remaining < TCPOLEN_SACK_BASE_ALIGNED + TCPOLEN_SACK_PERBLOCK)
				{
					return size;
				}

				opts.num_sack_blocks = (byte)Math.Min(eff_sacks, (remaining - TCPOLEN_SACK_BASE_ALIGNED) / TCPOLEN_SACK_PERBLOCK);
				size += (uint)(TCPOLEN_SACK_BASE_ALIGNED + opts.num_sack_blocks * TCPOLEN_SACK_PERBLOCK);
			}

			//if (BPF_SOCK_OPS_TEST_FLAG(tp, BPF_SOCK_OPS_WRITE_HDR_OPT_CB_FLAG))
			//{
			//	unsigned int remaining = MAX_TCP_OPTION_SPACE - size;

			//	bpf_skops_hdr_opt_len(sk, skb, NULL, NULL, 0, opts, &remaining);

			//	size = MAX_TCP_OPTION_SPACE - remaining;
			//}

			return size;
		}

		static void tcp_event_data_sent(tcp_sock tp)
		{
			long now = tcp_jiffies32;
			if (tcp_packets_in_flight(tp) == 0)
			{
				tcp_ca_event_func(tp, tcp_ca_event.CA_EVENT_TX_START);
			}
			tp.lsndtime = now;
			if ((now - tp.icsk_ack.lrcvtime) < tp.icsk_ack.ato)
			{
				inet_csk_inc_pingpong_cnt(tp);
			}
		}

		static void tcp_update_skb_after_send(tcp_sock tp, sk_buff skb, long prior_wstamp)
		{
			if (tp.sk_pacing_status != (uint)sk_pacing.SK_PACING_NONE)
			{
				long rate = tp.sk_pacing_rate;
				if (rate != long.MaxValue && rate > 0 && tp.data_segs_out >= 10)
				{
					long len_ns = skb.len * 1000 / rate;
					long credit = tp.tcp_wstamp_ns - prior_wstamp;
					len_ns -= Math.Min(len_ns / 2, credit);
					tp.tcp_wstamp_ns += len_ns;
				}
			}

			tp.tsorted_sent_queue.AddLast(skb);
		}

		static void tcp_insert_write_queue_after(sk_buff skb, sk_buff buff, tcp_sock tp, tcp_queue tcp_queue)
		{
			if (tcp_queue == tcp_queue.TCP_FRAG_IN_WRITE_QUEUE)
			{
				tp.sk_write_queue.AddLast(buff);
			}
			else
			{
				tp.tcp_rtx_queue.Add(buff);
			}
		}

		static void tcp_skb_collapse_tstamp(sk_buff skb, sk_buff next_skb)
		{
			if (tcp_has_tx_tstamp(next_skb))
			{
				skb_shared_info next_shinfo = skb_shinfo(next_skb);
				skb_shared_info shinfo = skb_shinfo(skb);

				shinfo.tx_flags = (byte)(shinfo.tx_flags | (next_shinfo.tx_flags & SKBTX_ANY_TSTAMP));
				shinfo.tskey = next_shinfo.tskey;
				TCP_SKB_CB(skb).txstamp_ack |= TCP_SKB_CB(next_skb).txstamp_ack;
			}
		}

		static uint tcp_tso_autosize(tcp_sock tp, uint mss_now, int min_tso_segs)
		{
			long bytes = (tp.sk_pacing_rate) >> (tp.sk_pacing_shift);
			uint r = tcp_min_rtt(tp) >> (sock_net(tp).ipv4.sysctl_tcp_tso_rtt_log);
			if (r < sizeof(uint) * 8)
			{
				bytes += tp.sk_gso_max_size >> (int)r;
			}
			bytes = Math.Min(bytes, tp.sk_gso_max_size);

			return (uint)Math.Max(bytes / mss_now, min_tso_segs);
		}

		static uint tcp_tso_segs(tcp_sock tp, uint mss_now)
		{
			tcp_congestion_ops ca_ops = tp.icsk_ca_ops;
			uint min_tso, tso_segs;

			min_tso = ca_ops.min_tso_segs != null ? ca_ops.min_tso_segs(tp) : (sock_net(tp).ipv4.sysctl_tcp_min_tso_segs);

			tso_segs = tcp_tso_autosize(sk, mss_now, min_tso);
			return Math.Min(tso_segs, tp.sk_gso_max_segs);
		}

		static bool tcp_rtx_queue_empty_or_single_skb(tcp_sock tp)
		{
			var node = tp.tcp_rtx_queue.FirstNode();
			if (node == null)
			{
				return true;
			}
			return node.LeftChild == null && node.RightChild == null;
		}

		static bool tcp_small_queue_check(tcp_sock tp, sk_buff skb, uint factor)
		{
			long limit = (long)Math.Max(2 * skb.truesize, tp.sk_pacing_rate) >> tp.sk_pacing_shift;
			if (tp.sk_pacing_status == (uint)sk_pacing.SK_PACING_NONE)
			{
				limit = Math.Min(limit, sock_net(tp).ipv4.sysctl_tcp_limit_output_bytes);
			}

			limit = (long)(limit << (int)factor);

			if (tcp_tx_delay_enabled && tp.tcp_tx_delay > 0)
			{
				long extra_bytes = (long)(tp.sk_pacing_rate) * tp.tcp_tx_delay;
				extra_bytes >>= (20 - 1);
				limit += extra_bytes;
			}

			if (tp.sk_wmem_alloc > limit)
			{
				if (tcp_rtx_queue_empty_or_single_skb(tp))
				{
					return false;
				}

				set_bit((byte)tsq_enum.TSQ_THROTTLED, tp.sk_tsq_flags);
				if (tp.sk_wmem_alloc > limit)
				{
					return true;
				}
			}
			return false;
		}

		static bool tcp_pacing_check(tcp_sock tp)
		{
			if (!tcp_needs_internal_pacing(tp))
			{
				return false;
			}

			if (tp.tcp_wstamp_ns <= tp.tcp_clock_cache)
			{
				return false;
			}

			if (!tp.pacing_timer.hrtimer_is_queued())
			{
				tp.pacing_timer.ModTimer(tp.tcp_wstamp_ns);
			}
			return true;
		}

		static void tcp_xmit_retransmit_queue(tcp_sock tp)
		{
			sk_buff skb, rtx_head, hole = null;
			bool rearm_timer = false;
			uint max_segs;
			LINUXMIB mib_idx;

			if (tp.packets_out == 0)
			{
				return;
			}

			rtx_head = tcp_rtx_queue_head(tp);
			skb = tp.retransmit_skb_hint != null ? tp.retransmit_skb_hint : rtx_head;
			max_segs = tcp_tso_segs(tp, tcp_current_mss(tp));


			for (; skb != null; skb = skb_rb_next(tp.tcp_rtx_queue, skb))
			{
				byte sacked;
				int segs;

				if (tcp_pacing_check(tp))
					break;

				if (hole == null)
				{
					tp.retransmit_skb_hint = skb;
				}

				segs = (int)(tcp_snd_cwnd(tp) - tcp_packets_in_flight(tp));
				if (segs <= 0)
				{
					break;
				}

				sacked = TCP_SKB_CB(skb).sacked;
				segs = (int)Math.Min(segs, max_segs);

				if (tp.retrans_out >= tp.lost_out)
				{
					break;
				}
				else if (!BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_LOST))
				{
					if (hole == null && !BoolOk(sacked & (byte)(tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS | tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED)))
					{
						hole = skb;
					}
					continue;
				}
				else
				{
					if (tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_Loss)
					{
						mib_idx = LINUXMIB.LINUX_MIB_TCPFASTRETRANS;
					}
					else
					{
						mib_idx = LINUXMIB.LINUX_MIB_TCPSLOWSTARTRETRANS;
					}
				}

				if (BoolOk(sacked & (byte)(tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED | tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS)))
				{
					continue;
				}

				if (tcp_small_queue_check(tp, skb, 1))
				{
					break;
				}

				if (tcp_retransmit_skb(tp, skb, segs) > 0)
				{
					break;
				}

				NET_ADD_STATS(sock_net(tp), mib_idx, tcp_skb_pcount(skb));

				if (tcp_in_cwnd_reduction(tp))
				{
					tp.prr_out += (uint)tcp_skb_pcount(skb);
				}

				if (skb == rtx_head && tp.icsk_pending != tcp_sock.ICSK_TIME_REO_TIMEOUT)
				{
					rearm_timer = true;
				}
			}

			if (rearm_timer)
			{
				tcp_reset_xmit_timer(tp, tcp_sock.ICSK_TIME_RETRANS, tp.icsk_rto, tcp_sock.TCP_RTO_MAX);
			}
		}

		static bool tcp_snd_wnd_test(tcp_sock tp, sk_buff skb, uint cur_mss)
		{
			uint end_seq = TCP_SKB_CB(skb).end_seq;
			if (skb.len > cur_mss)
			{
				end_seq = TCP_SKB_CB(skb).seq + cur_mss;
			}
			return !after(end_seq, tcp_wnd_end(tp));
		}

		static uint tcp_cwnd_test(tcp_sock tp)
		{
			uint in_flight, cwnd, halfcwnd;
			in_flight = tcp_packets_in_flight(tp);
			cwnd = tcp_snd_cwnd(tp);
			if (in_flight >= cwnd)
			{
				return 0;
			}
			halfcwnd = Math.Max(cwnd >> 1, 1);
			return Math.Min(halfcwnd, cwnd - in_flight);
		}

		static int __tcp_mtu_to_mss(tcp_sock tp, int pmtu)
		{
			int mss_now;
			mss_now = pmtu - tp.icsk_af_ops.net_header_len - sizeof_tcphdr;
			if (mss_now > tp.rx_opt.mss_clamp)
			{
				mss_now = tp.rx_opt.mss_clamp;
			}
			mss_now -= tp.icsk_ext_hdr_len;
			mss_now = Math.Max(mss_now, sock_net(tp).ipv4.sysctl_tcp_min_snd_mss);
			return mss_now;
		}
		
		static int tcp_mtu_to_mss(tcp_sock tp, int pmtu)
		{
			return __tcp_mtu_to_mss(tp, pmtu) - (tp.tcp_header_len - sizeof_tcphdr);
		}

		static uint tcp_mss_to_mtu(tcp_sock tp, uint mss)
		{
			return mss + tp.tcp_header_len + tp.icsk_ext_hdr_len + tp.icsk_af_ops.net_header_len;
		}

        //它在 Linux 内核的 TCP 协议栈中用于检查是否需要重新探测路径 MTU（Maximum Transmission Unit）。
		//这个函数的核心逻辑是根据时间间隔来决定是否启动新的 MTU 探测过程。
        static void tcp_mtu_check_reprobe(tcp_sock tp)
		{
			net net = sock_net(tp);
			uint interval;
			int delta;

			interval = net.ipv4.sysctl_tcp_probe_interval;
			delta = (int)(tcp_jiffies32 - tp.icsk_mtup.probe_timestamp);
			if (delta >= interval * tcp_sock.HZ)
			{
				uint mss = tcp_current_mss(tp);
				tp.icsk_mtup.probe_size = 0;
				tp.icsk_mtup.search_high = tp.rx_opt.mss_clamp + sizeof_tcphdr + tp.icsk_af_ops.net_header_len;
				tp.icsk_mtup.search_low = (int)tcp_mss_to_mtu(tp, mss);
				tp.icsk_mtup.probe_timestamp = tcp_jiffies32;
			}
		}

		static bool tcp_can_coalesce_send_queue_head(tcp_sock tp, int len)
		{
			sk_buff skb, next;
			skb = tcp_send_head(tp);
			LinkedListNode<sk_buff> skbNode = tp.sk_write_queue.First;
			while (skbNode != null)
			{
                skb = skbNode.Value;
				next = skbNode.Next.Value;

				if (len <= skb.len)
				{
					break;
				}

				if (tcp_has_tx_tstamp(skb) || !tcp_skb_can_collapse(skb, next))
				{
					return false;
				}

				len -= skb.len;
				skb = next;

                skbNode = skbNode.Next;
			}
			return true;
		}

		static int tcp_clone_payload(tcp_sock tp, sk_buff to, int probe_size)
		{
			skb_frag lastfrag = null;
			skb_frag[] fragto = skb_shinfo(to).frags;

			int i, todo, len = 0, nr_frags = 0;
			sk_buff skb;

			if (!sk_wmem_schedule(tp, to.truesize + probe_size))
			{
				return -ErrorCode.ENOMEM;
			}
			
			skb_queue_walk(&sk->sk_write_queue, skb)
			{
				const skb_frag_t* fragfrom = skb_shinfo(skb)->frags;

				if (skb_headlen(skb))
					return -EINVAL;

				for (i = 0; i < skb_shinfo(skb)->nr_frags; i++, fragfrom++)
				{
					if (len >= probe_size)
						goto commit;
					todo = min_t(int, skb_frag_size(fragfrom),
								probe_size - len);
					len += todo;
					if (lastfrag &&
						skb_frag_page(fragfrom) == skb_frag_page(lastfrag) &&
						skb_frag_off(fragfrom) == skb_frag_off(lastfrag) +
										skb_frag_size(lastfrag))
					{
						skb_frag_size_add(lastfrag, todo);
						continue;
					}
					if (unlikely(nr_frags == MAX_SKB_FRAGS))
						return -E2BIG;
					skb_frag_page_copy(fragto, fragfrom);
					skb_frag_off_copy(fragto, fragfrom);
					skb_frag_size_set(fragto, todo);
					nr_frags++;
					lastfrag = fragto++;
				}
			}
			commit:
			WARN_ON_ONCE(len != probe_size);
			for (i = 0; i<nr_frags; i++)
				skb_frag_ref(to, i);

				skb_shinfo(to)->nr_frags = nr_frags;
			to->truesize += probe_size;
			to->len += probe_size;
			to->data_len += probe_size;
			__skb_header_release(to);
			return 0;
		}

		static int tcp_mtu_probe(tcp_sock tp)
		{
			sk_buff skb, nskb, next;
			net net = sock_net(tp);
			int probe_size;
			int size_needed;
			int copy, len;
			uint mss_now;
			int interval;

			if (!tp.icsk_mtup.enabled || tp.icsk_mtup.probe_size > 0 ||
				   tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_Open ||
				   tcp_snd_cwnd(tp) < 11 ||
				   tp.rx_opt.num_sacks > 0 || tp.rx_opt.dsack > 0)
			{
				return -1;
			}

			mss_now = tcp_current_mss(tp);
			probe_size = tcp_mtu_to_mss(tp, (tp.icsk_mtup.search_high + tp.icsk_mtup.search_low) >> 1);
			size_needed = (int)(probe_size + (tp.reordering + 1) * tp.mss_cache);
			interval = tp.icsk_mtup.search_high - tp.icsk_mtup.search_low;

			if (probe_size > tcp_mtu_to_mss(tp, tp.icsk_mtup.search_high) || interval < net.ipv4.sysctl_tcp_probe_threshold)
			{
				tcp_mtu_check_reprobe(tp);
				return -1;
			}

			if (tp.write_seq - tp.snd_nxt < size_needed)
			{
				return -1;
			}

			if (tp.snd_wnd < size_needed)
			{
				return -1;
			}

			if (after((uint)(tp.snd_nxt + size_needed), tcp_wnd_end(tp)))
			{
				return 0;
			}

			if (tcp_packets_in_flight(tp) + 2 > tcp_snd_cwnd(tp))
			{
				if (tcp_packets_in_flight(tp) > 0)
					return -1;
				else
					return 0;
			}

			if (!tcp_can_coalesce_send_queue_head(tp, probe_size))
			{
				return -1;
			}

			/* We're allowed to probe.  Build it now. */
			nskb = new sk_buff();
			if (nskb == null)
			{
				return -1;
			}
			
			if (tcp_clone_payload(tp, nskb, probe_size))
			{
				tcp_skb_tsorted_anchor_cleanup(nskb);
				consume_skb(nskb);
				return -1;
			}

			sk_wmem_queued_add(sk, nskb->truesize);
			sk_mem_charge(sk, nskb->truesize);

			skb = tcp_send_head(sk);
			skb_copy_decrypted(nskb, skb);
			mptcp_skb_ext_copy(nskb, skb);

			TCP_SKB_CB(nskb)->seq = TCP_SKB_CB(skb)->seq;
			TCP_SKB_CB(nskb)->end_seq = TCP_SKB_CB(skb)->seq + probe_size;
			TCP_SKB_CB(nskb)->tcp_flags = TCPHDR_ACK;

			tcp_insert_write_queue_before(nskb, skb, sk);
			tcp_highest_sack_replace(sk, skb, nskb);

			len = 0;
			tcp_for_write_queue_from_safe(skb, next, sk) {
				copy = min_t(int, skb->len, probe_size - len);

				if (skb->len <= copy)
				{
					tcp_eat_one_skb(sk, nskb, skb);
				}
				else
				{
					TCP_SKB_CB(nskb)->tcp_flags |= TCP_SKB_CB(skb)->tcp_flags &
								   ~(TCPHDR_FIN | TCPHDR_PSH);
					__pskb_trim_head(skb, copy);
					tcp_set_skb_tso_segs(skb, mss_now);
					TCP_SKB_CB(skb)->seq += copy;
				}

				len += copy;

				if (len >= probe_size)
					break;
			}
			tcp_init_tso_segs(nskb, nskb->len);

			/* We're ready to send.  If this fails, the probe will
			 * be resegmented into mss-sized pieces by tcp_write_xmit().
			 */
			if (!tcp_transmit_skb(sk, nskb, 1, GFP_ATOMIC))
			{
				/* Decrement cwnd here because we are sending
				 * effectively two packets. */
				tcp_snd_cwnd_set(tp, tcp_snd_cwnd(tp) - 1);
				tcp_event_new_data_sent(sk, nskb);

				icsk->icsk_mtup.probe_size = tcp_mss_to_mtu(sk, nskb->len);
				tp->mtu_probe.probe_seq_start = TCP_SKB_CB(nskb)->seq;
				tp->mtu_probe.probe_seq_end = TCP_SKB_CB(nskb)->end_seq;

				return 1;
			}

			return -1;
		}

		static bool tcp_write_xmit(tcp_sock tp, uint mss_now, int nonagle, int push_one)
		{
			sk_buff skb;
			uint tso_segs, sent_pkts;
			uint cwnd_quota, max_segs;
			int result;
			bool is_cwnd_limited = false, is_rwnd_limited = false;
			sent_pkts = 0;

			tcp_mstamp_refresh(tp);
			if (push_one == 0)
			{
				result = tcp_mtu_probe(tp);
				if (result == 0)
				{
					return false;
				}
				else if (result > 0)
				{
					sent_pkts = 1;
				}
			}

			max_segs = tcp_tso_segs(tp, mss_now);
			while ((skb = tcp_send_head(tp)) != null)
			{
				uint limit;
				int missing_bytes;

				if (tcp_pacing_check(tp))
				{
					break;
				}

				cwnd_quota = tcp_cwnd_test(tp);
				if (cwnd_quota == 0)
				{
					if (push_one == 2)
					{
						cwnd_quota = 1;
					}
					else
					{
						break;
					}
				}

				cwnd_quota = Math.Min(cwnd_quota, max_segs);
				missing_bytes = (int)(cwnd_quota * mss_now - skb.len);
				if (missing_bytes > 0)
				{
					tcp_grow_skb(tp, skb, missing_bytes);
				}
				tso_segs = tcp_set_skb_tso_segs(skb, mss_now);

				if (!tcp_snd_wnd_test(tp, skb, mss_now))
				{
					is_rwnd_limited = true;
					break;
				}

				if (tso_segs == 1)
				{
					if (!tcp_nagle_test(tp, skb, mss_now, (tcp_skb_is_last(tp, skb) ? nonagle : TCP_NAGLE_PUSH)))
					{
						break;
					}
				}
				else
				{
					if (!push_one && tcp_tso_should_defer(tp, skb, is_cwnd_limited, is_rwnd_limited, max_segs))
					{
						break;
					}
				}

				limit = mss_now;
				if (tso_segs > 1 && !tcp_urg_mode(tp))
				{
					limit = tcp_mss_split_point(sk, skb, mss_now, cwnd_quota, nonagle);
				}

				if (skb.len > limit && tso_fragment(sk, skb, limit, mss_now))
				{
					break;
				}

				if (tcp_small_queue_check(tp, skb, 0))
				{
					break;
				}

				if (TCP_SKB_CB(skb).end_seq == TCP_SKB_CB(skb).seq)
				{
					break;
				}
				if (tcp_transmit_skb(tp, skb, 1) > 0)
				{
					break;
				}
			repair:
				tcp_event_new_data_sent(tp, skb);
				tcp_minshall_update(tp, mss_now, skb);
				sent_pkts += (uint)tcp_skb_pcount(skb);

				if (push_one > 0)
				{
					break;
				}
			}

			if (is_rwnd_limited)
			{
				tcp_chrono_start(sk, TCP_CHRONO_RWND_LIMITED);
			}
			else
			{
				tcp_chrono_stop(sk, TCP_CHRONO_RWND_LIMITED);
			}

			is_cwnd_limited |= (tcp_packets_in_flight(tp) >= tcp_snd_cwnd(tp));
			if (sent_pkts || is_cwnd_limited)
			{
				tcp_cwnd_validate(tp, is_cwnd_limited);
			}
			if (sent_pkts > 0)
			{
				if (tcp_in_cwnd_reduction(tp))
				{
					tp.prr_out += sent_pkts;
				}

				if (push_one != 2)
				{
					tcp_schedule_loss_probe(tp, false);
				}
				return false;
			}
			return !tp->packets_out && !tcp_write_queue_empty(sk);
		}

		static void tcp_send_loss_probe(tcp_sock tp)
		{
			sk_buff skb;
			int pcount;
			uint mss = tcp_current_mss(tp);
			if (tp.tlp_high_seq > 0)
			{
				tcp_rearm_rto(tp);
			}

			tp.tlp_retrans = 0;
			skb = tp.sk_write_queue.First.Value;
			if (skb != null && tcp_snd_wnd_test(tp, skb, mss))
			{
				pcount = (int)tp.packets_out;
				tcp_write_xmit(tp, mss, TCP_NAGLE_OFF, 2);
				if (tp.packets_out > pcount)
				{
					tp.tlp_high_seq = tp.snd_nxt;
					NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPLOSSPROBES, 1);
				}
				tcp_rearm_rto(tp);
			}

			skb = skb_rb_last(tp.tcp_rtx_queue);
			if (skb == null)
			{
				return;
			}

			if (skb_still_in_host_queue(tp, skb))
			{
				tcp_rearm_rto(tp);
			}

			pcount = tcp_skb_pcount(skb);
			if (WARN_ON(pcount == 0))
			{
				tcp_rearm_rto(tp);
			}

			if ((pcount > 1) && (skb.len > (pcount - 1) * mss))
			{
				if (tcp_fragment(tp, tcp_queue.TCP_FRAG_IN_RTX_QUEUE, skb, (int)((pcount - 1) * mss), mss) > 0)
				{
					tcp_rearm_rto(tp);
				}
				skb = skb_rb_next(tp.tcp_rtx_queue, skb);
			}

			if (skb == null || tcp_skb_pcount(skb) == 0)
			{
				tcp_rearm_rto(tp);
			}

			if (__tcp_retransmit_skb(tp, skb, 1) > 0)
			{
				tcp_rearm_rto(tp);
			}

			tp.tlp_retrans = 1;
		}


        static int tcp_write_wakeup(tcp_sock tp, int mib)
		{
			sk_buff skb;
			if (tp.sk_state == TCP_STATE.TCP_CLOSE)
			{
				return -1;
			}

			skb = tcp_send_head(tp);
			if (skb != null && before(TCP_SKB_CB(skb).seq, tcp_wnd_end(tp)))
			{
				int err;
				uint mss = tcp_current_mss(tp);
				uint seg_size = tcp_wnd_end(tp) - TCP_SKB_CB(skb).seq;

				if (before(tp.pushed_seq, TCP_SKB_CB(skb).end_seq))
				{
					tp.pushed_seq = TCP_SKB_CB(skb).end_seq;
				}

				if (seg_size < TCP_SKB_CB(skb).end_seq - TCP_SKB_CB(skb).seq || skb.len > mss)
				{
					seg_size = Math.Min(seg_size, mss);
					TCP_SKB_CB(skb).tcp_flags |= tcp_sock.TCPHDR_PSH;
					if (tcp_fragment(tp, tcp_queue.TCP_FRAG_IN_WRITE_QUEUE, skb, (int)seg_size, mss) > 0)
					{
						return -1;
					}
				}
				else if (tcp_skb_pcount(skb) == 0)
				{
					tcp_set_skb_tso_segs(skb, mss);
				}

				TCP_SKB_CB(skb).tcp_flags |= tcp_sock.TCPHDR_PSH;
				err = tcp_transmit_skb(tp, skb, 1);
				if (err == 0)
				{
					tcp_event_new_data_sent(tp, skb);
				}
				return err;
			}
			else
			{
				if (between(tp.snd_up, tp.snd_una + 1, tp.snd_una + 0xFFFF))
				{
					tcp_xmit_probe_skb(sk, 1, mib);
				}
				return tcp_xmit_probe_skb(sk, 0, mib);
			}
		}

	}
}

