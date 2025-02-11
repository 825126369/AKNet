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

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class tcp_out_options
    {
        public ushort options;        /* bit field of OPTION_* */
        public ushort mss;        /* 0 to disable */
        public byte ws;          /* window scale, 0 to disable */
        public byte num_sack_blocks; /* number of SACK blocks to include */
		public long tsval;
		public long	tsecr; /* need to include OPTION_TS */
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
            long val = tcp_jiffies32;
            tp.tcp_clock_cache = val;
			tp.tcp_mstamp = val;
		}

		public static void tcp_send_ack(tcp_sock tp)
		{
			__tcp_send_ack(tp, tp.rcv_nxt);
		}

		public static void __tcp_send_ack(tcp_sock tp, uint rcv_nxt)
		{
			sk_buff buff = tcp_stream_alloc_skb(tp);
			uint seq = tcp_acceptable_seq(tp);
            tcp_init_nondata_skb(buff, TCPHDR_ACK, ref seq);
			__tcp_transmit_skb(tp, buff, rcv_nxt, false);
		}

		public static int tcp_retransmit_skb(tcp_sock tp, sk_buff skb)
		{
			int err = __tcp_retransmit_skb(tp, skb);

			if (err == 0)
			{
				TCP_SKB_CB(skb).sacked |= (byte)tcp_skb_cb_sacked_flags.TCPCB_RETRANS;
				tp.retrans_out++;
			}

			if (tp.retrans_stamp == 0)
			{
				tp.retrans_stamp = tcp_skb_timestamp(skb);
			}
			if (tp.undo_retrans < 0)
			{
				tp.undo_retrans = 0;
			}
			tp.undo_retrans++;
			return err;
		}

		public static int __tcp_retransmit_skb(tcp_sock tp, sk_buff skb)
		{
			if (tp.icsk_mtup.probe_size > 0)
			{
				tp.icsk_mtup.probe_size = 0;
			}

			if (skb_still_in_host_queue(tp, skb))
			{
				return -ErrorCode.EBUSY;
			}
			
			if (before(TCP_SKB_CB(skb).seq, tp.snd_una))
			{
				if (before(TCP_SKB_CB(skb).end_seq, tp.snd_una))
				{
					return -ErrorCode.EINVAL;
				}
				if (tcp_trim_head(tp, skb, (int)(tp.snd_una - TCP_SKB_CB(skb).seq)) > 0)
				{
					return -ErrorCode.ENOMEM;
				}
			}
			
			uint cur_mss = tcp_current_mss(tp);
			int avail_wnd = (int)(tcp_wnd_end(tp) - TCP_SKB_CB(skb).seq);
			if (avail_wnd <= 0)
			{
				if (TCP_SKB_CB(skb).seq != tp.snd_una)
				{
					return -ErrorCode.EAGAIN;
				}
				avail_wnd = (int)cur_mss;
			}

			int len = (int)cur_mss;
			if (len > avail_wnd)
			{
				len = rounddown(avail_wnd, (int)cur_mss);
				if (len == 0)
				{
					len = avail_wnd;
				}
			}

			NetLog.Assert(skb.nBufferLength <= len);
			if (skb.nBufferLength > len)
			{
				if(tcp_fragment(tp, tcp_queue.TCP_FRAG_IN_RTX_QUEUE, skb, len, cur_mss) > 0)
				{
                    return -ErrorCode.ENOMEM;
                }
			}
			else
			{
				avail_wnd = Math.Min(avail_wnd, (int)cur_mss);
				if (skb.nBufferLength < avail_wnd)
				{
					tcp_retrans_try_collapse(tp, skb, avail_wnd);
				}
			}

			if ((TCP_SKB_CB(skb).tcp_flags & TCPHDR_SYN_ECN) == TCPHDR_SYN_ECN)
			{
				tcp_ecn_clear_syn(tp, skb);
			}
			
			tp.total_retrans++;
			tp.bytes_retrans += skb.nBufferLength;
			tcp_transmit_skb(tp, skb, true);

			TCP_SKB_CB(skb).sacked = (byte)(TCP_SKB_CB(skb).sacked | (byte)tcp_skb_cb_sacked_flags.TCPCB_EVER_RETRANS);
			return 0;
		}

        //原始方法: tcp_syn_options
        public static int tcp_syn_options(tcp_sock tp, sk_buff skb, tcp_out_options opts)
        {
            uint remaining = MAX_TCP_OPTION_SPACE;
            byte timestamps = sock_net(tp).ipv4.sysctl_tcp_timestamps;
            opts.mss = tcp_advertise_mss(tp);
            remaining -= TCPOLEN_MSS_ALIGNED;

            if (timestamps > 0)
            {
                opts.options |= OPTION_TS;
                opts.tsval = tcp_skb_timestamp(skb) + tp.tsoffset;
                opts.tsecr = tp.rx_opt.ts_recent;
                remaining -= TCPOLEN_TSTAMP_ALIGNED;
            }

            if (sock_net(tp).ipv4.sysctl_tcp_window_scaling > 0)
            {
                opts.options |= OPTION_WSCALE;
                opts.ws = (byte)tp.rx_opt.rcv_wscale;
                remaining -= TCPOLEN_WSCALE_ALIGNED;
            }

            if (sock_net(tp).ipv4.sysctl_tcp_sack > 0)
            {
                opts.options |= OPTION_SACK_ADVERTISE;
                if (!BoolOk(OPTION_TS & opts.options))
                {
                    remaining -= TCPOLEN_SACKPERM_ALIGNED;
                }
            }
            return (int)(MAX_TCP_OPTION_SPACE - remaining);
        }

        static int tcp_established_options(tcp_sock tp, sk_buff skb, tcp_out_options opts)
        {
            int size = 0;
            uint eff_sacks;
            opts.options = 0;

            if (tp.rx_opt.tstamp_ok > 0)
            {
                opts.options |= (ushort)OPTION_TS;
                opts.tsval = (uint)(skb != null ? tcp_skb_timestamp(skb) + tp.tsoffset : 0);
                opts.tsecr = tp.rx_opt.ts_recent;
                size += TCPOLEN_TSTAMP_ALIGNED;
            }

            eff_sacks = (uint)(tp.rx_opt.num_sacks + tp.rx_opt.dsack);
            if (eff_sacks > 0)
            {
                int remaining = MAX_TCP_OPTION_SPACE - size;
                if (remaining < TCPOLEN_SACK_BASE_ALIGNED + TCPOLEN_SACK_PERBLOCK)
                {
                    return size;
                }

                opts.num_sack_blocks = (byte)Math.Min(eff_sacks, (remaining - TCPOLEN_SACK_BASE_ALIGNED) / TCPOLEN_SACK_PERBLOCK);
                size += (TCPOLEN_SACK_BASE_ALIGNED + opts.num_sack_blocks * TCPOLEN_SACK_PERBLOCK);
            }

            return size;
        }

		public static void tcp_options_write(sk_buff skb, tcp_sock tp, tcp_out_options opts)
		{
			int nPtrSize = 4;
			Span<byte> ptr = skb_transport_header(skb).Slice(sizeof_tcphdr);

			ushort options = opts.options;
			if (opts.mss > 0)
			{
				EndianBitConverter.SetBytes(ptr, 0, (TCPOPT_MSS << 24) | (TCPOLEN_MSS << 16) | opts.mss);
				NetLog.Assert((ushort)EndianBitConverter.ToUInt32(ptr) == opts.mss);
				ptr = ptr.Slice(nPtrSize);
			}

			if (BoolOk(OPTION_TS & options))
			{
				if (BoolOk(OPTION_SACK_ADVERTISE & options))
				{
					uint nValue = (TCPOPT_SACK_PERM << 24) |
							   (TCPOLEN_SACK_PERM << 16) |
							   (TCPOPT_TIMESTAMP << 8) |
							   TCPOLEN_TIMESTAMP;

					EndianBitConverter.SetBytes(ptr, 0, nValue);
					ptr = ptr.Slice(nPtrSize);
					options = (ushort)(options & ~OPTION_SACK_ADVERTISE);
				}
				else
				{
					uint nValue = (TCPOPT_NOP << 24) |
							   (TCPOPT_NOP << 16) |
							   (TCPOPT_TIMESTAMP << 8) |
							   TCPOLEN_TIMESTAMP;

					EndianBitConverter.SetBytes(ptr, 0, nValue);
					ptr = ptr.Slice(nPtrSize);
				}

				EndianBitConverter.SetBytes(ptr, 0, (uint)opts.tsval);
				ptr = ptr.Slice(nPtrSize);

				EndianBitConverter.SetBytes(ptr, 0, (uint)opts.tsecr);
				ptr = ptr.Slice(nPtrSize);
			}

			if (BoolOk(OPTION_SACK_ADVERTISE & options))
			{
				var nValue = (TCPOPT_NOP << 24) |
						   (TCPOPT_NOP << 16) |
						   (TCPOPT_SACK_PERM << 8) |
						   TCPOLEN_SACK_PERM;

				EndianBitConverter.SetBytes(ptr, 0, nValue);
				ptr = ptr.Slice(nPtrSize);
			}

			if (BoolOk(OPTION_WSCALE & options))
			{
				var nValue = (TCPOPT_NOP << 24) |
						   (TCPOPT_WINDOW << 16) |
						   (TCPOLEN_WINDOW << 8) |
						   opts.ws;

				EndianBitConverter.SetBytes(ptr, 0, nValue);
				ptr = ptr.Slice(nPtrSize);
			}

			if (opts.num_sack_blocks > 0)
			{
				tcp_sack_block[] sp = tp.rx_opt.dsack > 0 ? tp.duplicate_sack : tp.selective_acks;
				var nValue = (uint)((TCPOPT_NOP << 24) |
						   (TCPOPT_NOP << 16) |
						   (TCPOPT_SACK << 8) |
						   (TCPOLEN_SACK_BASE + (opts.num_sack_blocks * TCPOLEN_SACK_PERBLOCK)));

				EndianBitConverter.SetBytes(ptr, 0, nValue);
				ptr = ptr.Slice(nPtrSize);

				NetLog.Assert(opts.num_sack_blocks <= sp.Length, opts.num_sack_blocks + " | " + sp.Length);
				for (int this_sack = 0; this_sack < opts.num_sack_blocks; ++this_sack)
				{
					EndianBitConverter.SetBytes(ptr, 0, sp[this_sack].start_seq);
					ptr = ptr.Slice(nPtrSize);
					EndianBitConverter.SetBytes(ptr, 0, sp[this_sack].end_seq);
					ptr = ptr.Slice(nPtrSize);
				}
				tp.rx_opt.dsack = 0;
			}
		}

        //clone_it = 1：表示需要克隆 skb。
        //在这种情况下，tcp_transmit_skb 会创建一个 skb 的副本用于发送，而原始的 skb 保留用于可能的重传。
		//这是 TCP 可靠传输机制的一部分，因为 TCP 支持重传机制，未收到 ACK 确认的数据不能被删除。
		//clone_it = 0：表示不需要克隆 skb。
		//在这种情况下，直接使用传入的 skb 进行发送，而不创建副本。
		public static int tcp_transmit_skb(tcp_sock tp, sk_buff skb, bool clone_it)
		{
			return __tcp_transmit_skb(tp, skb, tp.rcv_nxt, clone_it);
		}

		static void tcp_ecn_send(tcp_sock tp, sk_buff skb, tcphdr th, int tcp_header_len)
		{
			if (BoolOk(tp.ecn_flags & TCP_ECN_OK))
			{
				if (skb.nBufferLength != tcp_header_len && !before(TCP_SKB_CB(skb).seq, tp.snd_nxt))
				{
					INET_ECN_xmit(tp);
					if (BoolOk(tp.ecn_flags & TCP_ECN_QUEUE_CWR))
					{
						tp.ecn_flags = (byte)(tp.ecn_flags & ~TCP_ECN_QUEUE_CWR);
						th.cwr = 1;
					}
				}

				if (BoolOk(tp.ecn_flags & TCP_ECN_DEMAND_CWR))
				{
					th.ece = 1;
				}
			}
		}
		
		//clone_it: false 比如发送ACK
		//clone_it: true 比如发送正常包
		static int __tcp_transmit_skb(tcp_sock tp, sk_buff skb, uint rcv_nxt, bool clone_it)
		{
			tcp_skb_cb tcb = TCP_SKB_CB(skb);

			long prior_wstamp = tp.tcp_wstamp_ns;
			tp.tcp_wstamp_ns = Math.Max(tp.tcp_wstamp_ns, tp.tcp_clock_cache);
			skb_set_delivery_time(skb, tp.tcp_wstamp_ns, skb_tstamp_type.SKB_CLOCK_MONOTONIC);
			sk_buff oskb = null;
			if (clone_it)
			{
				oskb = skb;
				skb = tcp_stream_alloc_skb(tp);
				oskb.GetTcpReceiveBufferSpan().CopyTo(skb.mBuffer.AsSpan().Slice(skb.nBufferOffset));
				skb.nBufferLength = oskb.nBufferLength;
			}

			tcp_out_options opts = new tcp_out_options();
			int tcp_options_size = tcp_established_options(tp, skb, opts);
			byte tcp_header_size = (byte)(tcp_options_size + sizeof_tcphdr);
			skb.ooo_okay = tcp_rtx_queue_empty(tp);

			tcphdr th = tcp_hdr(skb);
			th.source = tp.inet_sport;
			th.dest = tp.inet_dport;
			th.seq = tcb.seq;
			th.ack_seq = rcv_nxt;
			th.doff = tcp_header_size;
			th.tcp_flags = tcb.tcp_flags;
			th.check = 0;
			th.urg = 0;
			th.window = tcp_select_window(tp);
			th.commandId = 0;
			tcp_ecn_send(tp, skb, th, tcp_header_size);

			th.tot_len = (ushort)(tcp_header_size + skb.nBufferLength);
			skb_push(skb, tcp_header_size);

			tcp_hdr(skb).WriteTo(skb);
			tcp_options_write(skb, tp, opts);

			tcp_v4_send_check(tp, skb);
			if (BoolOk(tcb.tcp_flags & TCPHDR_ACK))
			{
				tcp_event_ack_sent(tp, rcv_nxt);
			}

			if (skb.nBufferLength != tcp_header_size)
			{
				tcp_event_data_sent(tp);
				tp.data_segs_out++;
				tp.bytes_sent += skb.nBufferLength - tcp_header_size;
			}

			tp.segs_out++;
			tcp_add_tx_delay(skb, tp);
			int err = ip_queue_xmit(tp, skb);

			if (err > 0)
			{
				tcp_enter_cwr(tp);
			}

			if (err == 0 && clone_it)
			{
				tcp_update_skb_after_send(tp, oskb, prior_wstamp);
				tcp_rate_skb_sent(tp, oskb);
			}
			return err;
		}

        //用于检查套接字缓冲区（SKB，Socket Buffer）是否仍然在主机队列中的函数。
		//这个函数通常用于 TCP 协议栈中，特别是在处理 TCP 重传和拥塞控制时。
        public static bool skb_still_in_host_queue(tcp_sock tp, sk_buff skb)
		{
			return false;
		}

        //当部分数据已经被确认接收后，需要从发送队列中移除这部分数据
        public static int tcp_trim_head(tcp_sock tp, sk_buff skb, int len)
		{
			int maxLength = (int)(TCP_SKB_CB(skb).end_seq - TCP_SKB_CB(skb).seq);
            if (len > maxLength)
			{
				len = maxLength;
			}

			TCP_SKB_CB(skb).seq += (uint)len;
			skb_pull(skb, len);
			return 0;
		}

		public static uint tcp_current_mss(tcp_sock tp)
		{
			dst_entry dst = __sk_dst_get(tp);
			uint mss_now = tp.mss_cache;
            if (dst != null)
            {
                uint mtu = ipv4_mtu(dst);
				if (mtu != tp.icsk_pmtu_cookie)
				{
					mss_now = tcp_sync_mss(tp, mtu);
				}
            }

			// 上面得到的Mss，是减去UDP头部的长度
			// 现在得减去 TCP最大头部长度
			mss_now -= max_tcphdr_length;
            return mss_now;
		}

        //用于处理 TCP 数据包的分段操作。它会根据 MSS 的值将较大的 TCP 数据包分割成多个较小的分段
		//这个一般发生在 动态调整 Mss的情况下，如果Mss 减小，那么得分割 SKB
        public static int tcp_fragment(tcp_sock tp, tcp_queue tcp_queue, sk_buff skb, int len, uint mss_now)
		{
			if (WARN_ON(len > skb.nBufferLength))
			{
				return -ErrorCode.EINVAL;
			}

			NetLog.Assert(skb.nBufferLength > mss_now, $"tcp_fragment: {len} {mss_now} {skb.nBufferLength}");

			long limit = tp.sk_sndbuf;
			if ((tp.sk_wmem_queued >> 1) > limit && tcp_queue != tcp_queue.TCP_FRAG_IN_WRITE_QUEUE &&
					 skb != tcp_rtx_queue_head(tp) &&
					 skb != tcp_rtx_queue_tail(tp))
			{
				return -ErrorCode.ENOMEM;
			}

            sk_buff twoSkb = tcp_stream_alloc_skb(tp);

            sk_wmem_queued_add(tp, twoSkb.mBuffer.Length);
            sk_mem_charge(tp, twoSkb.mBuffer.Length);
            int nlen = skb.nBufferLength - len;

			TCP_SKB_CB(twoSkb).seq = TCP_SKB_CB(skb).seq + (uint)skb.nBufferLength;
			TCP_SKB_CB(twoSkb).end_seq = TCP_SKB_CB(skb).end_seq;
			TCP_SKB_CB(skb).end_seq = TCP_SKB_CB(twoSkb).seq;

			byte flags = TCP_SKB_CB(skb).tcp_flags;
			TCP_SKB_CB(skb).tcp_flags = (byte)(flags & ~(TCPHDR_FIN | TCPHDR_PSH));
			TCP_SKB_CB(twoSkb).tcp_flags = flags;
			TCP_SKB_CB(twoSkb).sacked = TCP_SKB_CB(skb).sacked;
			tcp_skb_fragment_eor(skb, twoSkb);

            skb_split(skb, twoSkb, len);

            skb_set_delivery_time(twoSkb, skb.tstamp, skb_tstamp_type.SKB_CLOCK_MONOTONIC);
			tcp_fragment_tstamp(skb, twoSkb);

			TCP_SKB_CB(twoSkb).tx = TCP_SKB_CB(skb).tx;
            if (!before(tp.snd_nxt, TCP_SKB_CB(twoSkb).end_seq))
            {
				int diff = -1;
                tcp_adjust_pcount(tp, skb, diff);
            }

            tcp_insert_write_queue_after(skb, twoSkb, tp, tcp_queue);
			if (tcp_queue == tcp_queue.TCP_FRAG_IN_RTX_QUEUE)
			{
				list_add(twoSkb.tcp_tsorted_anchor, skb.tcp_tsorted_anchor);
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

			if ((TCP_SKB_CB(skb).tcp_flags & TCPHDR_SYN) > 0)
			{
				return;
			}

			for (; (tmp = (skb != null ? skb_rb_next(skb) : null)) != null; skb = tmp)
			{
				if (!tcp_can_collapse(tp, skb))
				{
					break;
				}

				if (!tcp_skb_can_collapse(to, skb))
					break;

				space -= skb.nBufferLength;

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
				TCP_SKB_CB(skb).tcp_flags = (byte)(TCP_SKB_CB(skb).tcp_flags & ~(TCPHDR_ECE | TCPHDR_CWR));
			}
		}

		public static bool tcp_has_tx_tstamp(sk_buff skb)
		{
			return (TCP_SKB_CB(skb).txstamp_ack > 0 || (skb.tx_flags & (byte)SKBTX_ANY_TSTAMP) > 0);
		}

		public static void tcp_fragment_tstamp(sk_buff skb, sk_buff skb2)
		{
			if (tcp_has_tx_tstamp(skb) && !before(skb.tskey, TCP_SKB_CB(skb2).seq))
			{
				byte tsflags = (byte)(skb.tx_flags & SKBTX_ANY_TSTAMP);

                skb.tx_flags = (byte)(skb.tx_flags & ~tsflags);
                skb2.tx_flags |= tsflags;

				var temp = skb.tskey;
                skb.tskey = skb2.tskey;
                skb2.tskey = temp;

				TCP_SKB_CB(skb2).txstamp_ack = TCP_SKB_CB(skb).txstamp_ack;
				TCP_SKB_CB(skb).txstamp_ack = 0;
			}
		}

		static bool tcp_collapse_retrans(tcp_sock tp, sk_buff skb)
		{
			sk_buff next_skb = skb_rb_next(skb);
			int next_skb_size;
			next_skb_size = next_skb.nBufferLength;

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

			int diff = 1;
            tcp_adjust_pcount(tp, next_skb, diff);
			tcp_skb_collapse_tstamp(skb, next_skb);
			tcp_rtx_queue_unlink_and_free(next_skb, tp);
			return true;
		}

		//用于调整 TCP 发送队列中报文段的计数信息，以确保 TCP 协议栈中的各种状态变量（如 packets_out、sacked_out、retrans_out 等）
		//能够正确反映当前的发送状态
		static void tcp_adjust_pcount(tcp_sock tp, sk_buff skb, int decr)
		{
			tp.packets_out -= (uint)decr;

			if (BoolOk(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED))
			{
				tp.sacked_out -= (uint)decr;
			}

			if (BoolOk(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS))
			{
				tp.retrans_out -= (uint)decr;
			}

			if (BoolOk(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_LOST))
			{
				tp.lost_out -= (uint)decr;
			}

			if (tcp_is_reno(tp) && decr > 0)
			{
				tp.sacked_out -= (uint)Math.Min(tp.sacked_out, decr);
			}

			if (tp.lost_skb_hint != null && before(TCP_SKB_CB(skb).seq, TCP_SKB_CB(tp.lost_skb_hint).seq) &&
				BoolOk(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED))
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
					new_win = (uint)(cur_win * (1 << tp.rx_opt.rcv_wscale));
				}
			}

			tp.rcv_wnd = new_win;
			tp.rcv_wup = tp.rcv_nxt;

			new_win = (uint)Math.Min(new_win, ushort.MaxValue << tp.rx_opt.rcv_wscale);
			new_win >>= tp.rx_opt.rcv_wscale;
			if (new_win == 0)
			{
				tp.pred_flags = 0;
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
				tp.compressed_ack = 0;
                tp.compressed_ack_timer.Stop();
            }

			if (rcv_nxt != tp.rcv_nxt)
			{
				return;
			}

			tcp_dec_quickack_mode(tp);
			inet_csk_clear_xmit_timer(tp, ICSK_TIME_DACK);
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
					long len_ns = skb.nBufferLength * 1000 / rate;
					long credit = tp.tcp_wstamp_ns - prior_wstamp;
					len_ns -= Math.Min(len_ns / 2, credit);
					tp.tcp_wstamp_ns += len_ns;
				}
			}
			list_move_tail(skb.tcp_tsorted_anchor, tp.tsorted_sent_queue);
		}

		static void tcp_insert_write_queue_after(sk_buff skb, sk_buff buff, tcp_sock tp, tcp_queue tcp_queue)
		{
			if (tcp_queue == tcp_queue.TCP_FRAG_IN_WRITE_QUEUE)
			{
				__skb_queue_after(tp.sk_write_queue, skb, buff);
			}
			else
			{
				tcp_rbtree_insert(tp.tcp_rtx_queue, buff);
			}
		}

		static void tcp_skb_collapse_tstamp(sk_buff skb, sk_buff next_skb)
		{
			if (tcp_has_tx_tstamp(next_skb))
			{
                skb.tx_flags = (byte)(skb.tx_flags | (next_skb.tx_flags & SKBTX_ANY_TSTAMP));
                skb.tskey = next_skb.tskey;
				TCP_SKB_CB(skb).txstamp_ack |= TCP_SKB_CB(next_skb).txstamp_ack;
			}
		}

		static bool tcp_rtx_queue_empty_or_single_skb(tcp_sock tp)
		{
			rb_node node = tp.tcp_rtx_queue.rb_node;
			if (node == null)
			{
				return true;
			}
			return node.rb_left == null && node.rb_right == null;
		}

		static bool tcp_small_queue_check(tcp_sock tp, sk_buff skb, int factor)
		{
			long limit = (long)Math.Max(2 * skb.nBufferLength, tp.sk_pacing_rate) >> tp.sk_pacing_shift;
			if (tp.sk_pacing_status == (uint)sk_pacing.SK_PACING_NONE)
			{
				limit = Math.Min(limit, sock_net(tp).ipv4.sysctl_tcp_limit_output_bytes);
			}

			limit = (limit << factor);

			if (tcp_tx_delay_enabled && tp.tcp_tx_delay > 0)
			{
				long extra_bytes = tp.sk_pacing_rate * tp.tcp_tx_delay;
				extra_bytes >>= 19;
				limit += extra_bytes;
			}

			if (tp.sk_wmem_alloc > limit)
			{
				if (tcp_rtx_queue_empty_or_single_skb(tp))
				{
					return false;
				}

				tp.sk_tsq_flags |= (1 << (byte)tsq_enum.TSQ_THROTTLED);
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
				tp.pacing_timer.Start(tp.tcp_wstamp_ns);
			}
			return true;
		}

		static void tcp_xmit_retransmit_queue(tcp_sock tp)
		{
			bool rearm_timer = false;
			if (tp.packets_out == 0)
			{
				return;
			}

			sk_buff rtx_head = tcp_rtx_queue_head(tp);
			sk_buff skb = tp.retransmit_skb_hint != null ? tp.retransmit_skb_hint : rtx_head;
			sk_buff hole = null;
			for (; skb != null; skb = skb_rb_next(skb))
			{
				if (tcp_pacing_check(tp))
				{
					break;
				}

				if (hole == null)
				{
					tp.retransmit_skb_hint = skb;
				}

				byte sacked = TCP_SKB_CB(skb).sacked;
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

				if (BoolOk(sacked & (byte)(tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED | tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS)))
				{
					continue;
				}

				if (tcp_small_queue_check(tp, skb, 1))
				{
					break;
				}

				if (tcp_retransmit_skb(tp, skb) > 0)
				{
					break;
				}

				if (tcp_in_cwnd_reduction(tp))
				{
					tp.prr_out++;
				}

				if (skb == rtx_head && tp.icsk_pending != ICSK_TIME_REO_TIMEOUT)
				{
					rearm_timer = true;
				}
			}

			if (rearm_timer)
			{
				tcp_reset_xmit_timer(tp, ICSK_TIME_RETRANS, tp.icsk_rto, TCP_RTO_MAX);
			}
		}

		static bool tcp_snd_wnd_test(tcp_sock tp, sk_buff skb, uint cur_mss)
		{
			uint end_seq = TCP_SKB_CB(skb).end_seq;
			if (skb.nBufferLength > cur_mss)
			{
				end_seq = TCP_SKB_CB(skb).seq + cur_mss;
			}
			return !after(end_seq, tcp_wnd_end(tp));
		}

		//主要作用是判断当前拥塞窗口是否允许发送新的数据包
		// 0: 测试未通过
		// 非零值：可以发包的最大数量
		static uint tcp_cwnd_test(tcp_sock tp)
		{
			uint in_flight = tcp_packets_in_flight(tp);
			uint cwnd = tcp_snd_cwnd(tp);
			if (in_flight >= cwnd)
			{
				return 0;
			}
			uint halfcwnd = Math.Max(cwnd >> 1, 1);
			return Math.Min(halfcwnd, cwnd - in_flight);
		}

		static int __tcp_mtu_to_mss(tcp_sock tp, int pmtu)
		{
			int mss_now;
			mss_now = pmtu - mtu_max_head_length;
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
			return __tcp_mtu_to_mss(tp, pmtu);
		}

		static uint tcp_mss_to_mtu(tcp_sock tp, uint mss)
		{
            return (uint)(mss + mtu_max_head_length);
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
			if (delta >= interval * HZ)
			{
				uint mss = tcp_current_mss(tp);
				tp.icsk_mtup.probe_size = 0;
				tp.icsk_mtup.search_high = tp.rx_opt.mss_clamp + sizeof_tcphdr;
				tp.icsk_mtup.search_low = (int)tcp_mss_to_mtu(tp, mss);
				tp.icsk_mtup.probe_timestamp = tcp_jiffies32;
			}
		}

		static bool tcp_can_coalesce_send_queue_head(tcp_sock tp, int len)
		{
			sk_buff skb, next;
			skb = tcp_send_head(tp);
			for (next = skb.next; skb != null; skb = next, next = skb.next)
			{
				if (len <= skb.nBufferLength)
				{
					break;
				}

				if (tcp_has_tx_tstamp(skb) || !tcp_skb_can_collapse(skb, next))
				{
					return false;
				}

				len -= skb.nBufferLength;
			}
			return true;
		}

		static void tcp_eat_one_skb(tcp_sock tp, sk_buff dst, sk_buff src)
		{
			TCP_SKB_CB(dst).tcp_flags |= TCP_SKB_CB(src).tcp_flags;
			TCP_SKB_CB(dst).eor = TCP_SKB_CB(src).eor;
			tcp_skb_collapse_tstamp(dst, src);
			tcp_unlink_write_queue(src, tp);
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

			nskb = new sk_buff();
			if (nskb == null)
			{
				return -1;
			}

			sk_wmem_queued_add(tp, nskb.nBufferLength);
			sk_mem_charge(tp, nskb.nBufferLength);

			skb = tcp_send_head(tp);
			TCP_SKB_CB(nskb).seq = TCP_SKB_CB(skb).seq;
			TCP_SKB_CB(nskb).end_seq = (uint)(TCP_SKB_CB(skb).seq + probe_size);
			TCP_SKB_CB(nskb).tcp_flags = TCPHDR_ACK;

			tcp_insert_write_queue_before(nskb, skb, tp);
			tcp_highest_sack_replace(tp, skb, nskb);

			len = 0;
			for (next = skb.next; skb != null; skb = next, next = skb.next)
			{
				copy = Math.Min(skb.nBufferLength, probe_size - len);

				if (skb.nBufferLength <= copy)
				{
					tcp_eat_one_skb(tp, nskb, skb);
				}
				else
				{
					TCP_SKB_CB(nskb).tcp_flags |= (byte)(TCP_SKB_CB(skb).tcp_flags & ~(TCPHDR_FIN | TCPHDR_PSH));
					TCP_SKB_CB(skb).seq += (uint)copy;
				}

				len += copy;

				if (len >= probe_size)
				{
					break;
				}
			}

			tcp_transmit_skb(tp, nskb, true);
			tcp_snd_cwnd_set(tp, tcp_snd_cwnd(tp) - 1);
			tcp_event_new_data_sent(tp, nskb);
			tp.icsk_mtup.probe_size = tcp_mss_to_mtu(tp, (uint)nskb.nBufferLength);
			tp.mtu_probe.probe_seq_start = TCP_SKB_CB(nskb).seq;
			tp.mtu_probe.probe_seq_end = TCP_SKB_CB(nskb).end_seq;
			return 1;
		}

		static void tcp_grow_skb(tcp_sock tp, sk_buff skb, int amount)
		{
			if (tcp_skb_is_last(tp, skb))
			{
				return;
			}

            sk_buff next_skb = skb.next;
            if (!tcp_skb_can_collapse(skb, next_skb))
			{
				return;
			}

			int nlen = Math.Min(amount, next_skb.nBufferLength);
			if (nlen == 0 || skb_shift(skb, next_skb, nlen) == 0)
			{
				return;
			}

			TCP_SKB_CB(skb).end_seq += (uint)nlen;
			TCP_SKB_CB(next_skb).seq += (uint)nlen;

			if (next_skb.nBufferLength == 0)
			{
				TCP_SKB_CB(skb).end_seq = TCP_SKB_CB(next_skb).end_seq;
				tcp_eat_one_skb(tp, skb, next_skb);
			}
		}

        //tcp_minshall_check 是 Linux 内核 TCP 协议栈中的一个辅助函数，用于检查是否满足 Minshall 的 Nagle 算法条件。
		//Minshall 的 Nagle 算法是一种优化机制，旨在减少发送小数据包的数量，从而提高网络效率并减少网络拥塞。
        //Minshall 的 Nagle 算法
		//Minshall 的 Nagle 算法的核心思想是：
		//如果发送方有未确认的数据（即 tcp_unacked 队列中有数据），则发送方会缓存新的小数据包，直到收到确认（ACK）或数据包大小达到 MSS（最大报文段长度）。
		//如果当前数据包的大小加上已发送但未确认的数据包的大小达到 MSS，则可以发送数据包。
		//tcp_minshall_check 函数的作用
		//tcp_minshall_check 函数用于检查当前是否满足 Minshall 的 Nagle 算法条件。具体来说，它检查当前数据包的大小加上已发送但未确认的数据包的大小是否达到 MSS。
        static bool tcp_minshall_check(tcp_sock tp)
		{
			return after(tp.snd_sml, tp.snd_una) && !after(tp.snd_sml, tp.snd_nxt);
		}

        //Nagle 算法的核心思想是：
        //如果发送方有未确认的数据（即 tcp_unacked 队列中有数据），则发送方会缓存新的小数据包，直到收到确认（ACK）或数据包大小达到 MSS（最大报文段长度）。
		//如果发送方没有未确认的数据，则可以立即发送新的数据包。
		//tcp_nagle_check 函数的作用
		//tcp_nagle_check 函数用于检查当前是否满足 Nagle 算法的发送条件。它会考虑以下因素：
		//是否有未确认的数据。
		//当前数据包的大小是否达到 MSS。
		//是否有其他数据包已经发送但尚未确认。
		static bool tcp_nagle_check(bool bPartial, tcp_sock tp, int nonagle)
		{
			return bPartial &&
				(BoolOk(nonagle & TCP_NAGLE_CORK) ||
				 (nonagle == 0 && tp.packets_out > 0 && tcp_minshall_check(tp)));
		}

        //是否需要立即发送：
        //如果最后一个数据包是用户显式请求发送的（例如，调用了 send 或 write 函数），则通常需要立即发送，以确保数据及时到达对端。
		//如果最后一个数据包是由于某些内部机制（如定时器或拥塞控制）触发的，则可能不需要立即发送，而是等待更多的数据或确认。
		//是否满足 Nagle 算法的条件：
		//如果最后一个数据包已经满足 Nagle 算法的条件（例如，数据包大小达到 MSS 或没有未确认的数据），则可以立即发送。
		// Nagle 算法的条件（例如，数据包很小且有未确认的数据），则可能需要缓存，直到满足条件。
        //检查当前是否可以发送数据包。它根据 Nagle 算法的规则，判断是否满足发送条件。
		//如果将要发送则为True; 
		//不发送则为False;
        static bool tcp_nagle_test(tcp_sock tp, sk_buff skb, uint cur_mss, int nonagle)
		{
			if (BoolOk(nonagle & TCP_NAGLE_PUSH))
			{
				return true;
			}

			if (!tcp_nagle_check(skb.nBufferLength < cur_mss, tp, nonagle))
			{
				return true;
			}

			return false;
		}

		static bool tcp_tso_should_defer(tcp_sock tp, sk_buff skb, bool is_cwnd_limited, bool is_rwnd_limited, uint max_segs)
		{
			uint send_win, cong_win, limit, in_flight;
			sk_buff head;
			int win_divisor;
			long delta;

			if (tp.icsk_ca_state >= (byte)tcp_ca_state.TCP_CA_Recovery)
			{
				return false;
			}

			delta = tp.tcp_clock_cache - tp.tcp_wstamp_ns - 1;
			if (delta > 0)
			{
				return false;
			}

			in_flight = tcp_packets_in_flight(tp);
			send_win = tcp_wnd_end(tp) - TCP_SKB_CB(skb).seq;
			cong_win = (tcp_snd_cwnd(tp) - in_flight) * tp.mss_cache;

			limit = Math.Min(send_win, cong_win);

			if (limit >= max_segs * tp.mss_cache)
			{
				return false;
			}

			if ((skb != tcp_write_queue_tail(tp)) && (limit >= skb.nBufferLength))
			{
				return false;
			}

			win_divisor = sock_net(tp).ipv4.sysctl_tcp_tso_win_divisor;
			if (win_divisor > 0)
			{
				uint chunk = Math.Min(tp.snd_wnd, tcp_snd_cwnd(tp) * tp.mss_cache);
				chunk = (uint)(chunk / win_divisor);
				if (limit >= chunk)
				{
					return false;
				}
			}
			else
			{
				if (limit > tcp_max_tso_deferred_mss(tp) * tp.mss_cache)
				{
					return false;
				}
			}

			head = tcp_rtx_queue_head(tp);
			if (head == null)
			{
				return false;
			}
			delta = tp.tcp_clock_cache - head.tstamp;

			if (delta - (tp.srtt_us >> 4) < 0)
			{
				return false;
			}

			if (cong_win < send_win)
			{
				if (cong_win <= skb.nBufferLength)
				{
					is_cwnd_limited = true;
					return true;
				}
			}
			else
			{
				if (send_win <= skb.nBufferLength)
				{
					is_rwnd_limited = true;
					return true;
				}
			}

			if (BoolOk(TCP_SKB_CB(skb).tcp_flags & TCPHDR_FIN) || TCP_SKB_CB(skb).eor > 0)
			{
				return false;
			}
			return true;
		}

		static uint tcp_mss_split_point(tcp_sock tp, sk_buff skb, uint mss_now, uint max_segs, int nonagle)
		{
			uint partial, needed, window, max_len;
			window = tcp_wnd_end(tp) - TCP_SKB_CB(skb).seq;
			max_len = mss_now * max_segs;

			if (max_len <= window && skb != tcp_write_queue_tail(tp))
			{
				return max_len;
			}

			needed = (uint)Math.Min(skb.nBufferLength, window);

			if (max_len <= needed)
			{
				return max_len;
			}

			partial = needed % mss_now;
			if (tcp_nagle_check(partial != 0, tp, nonagle))
			{
				return needed - partial;
			}
			return needed;
		}

		static void tcp_minshall_update(tcp_sock tp, uint mss_now, sk_buff skb)
		{
			if (skb.nBufferLength < mss_now)
			{
				tp.snd_sml = TCP_SKB_CB(skb).end_seq;
			}
		}

		static void tcp_chrono_start(tcp_sock tp, tcp_chrono type)
		{
			if (type > tp.chrono_type)
			{
				tcp_chrono_set(tp, type);
			}
		}

		static void tcp_cwnd_application_limited(tcp_sock tp)
		{
			if (tp.icsk_ca_state == (byte)tcp_ca_state.TCP_CA_Open)
			{
				uint init_win = tcp_init_cwnd(tp, __sk_dst_get(tp));
				uint win_used = Math.Max(tp.snd_cwnd_used, init_win);
				if (win_used < tcp_snd_cwnd(tp))
				{
					tp.snd_ssthresh = tcp_current_ssthresh(tp);
					tcp_snd_cwnd_set(tp, (tcp_snd_cwnd(tp) + win_used) >> 1);
				}
				tp.snd_cwnd_used = 0;
			}
			tp.snd_cwnd_stamp = tcp_jiffies32;
		}

		static void tcp_cwnd_validate(tcp_sock tp, bool is_cwnd_limited)
		{
			tcp_congestion_ops ca_ops = tp.icsk_ca_ops;
			if (!before(tp.snd_una, tp.cwnd_usage_seq) ||
				is_cwnd_limited ||
				(!tp.is_cwnd_limited &&
				 tp.packets_out > tp.max_packets_out))
			{
				tp.is_cwnd_limited = is_cwnd_limited;
				tp.max_packets_out = tp.packets_out;
				tp.cwnd_usage_seq = tp.snd_nxt;
			}

			if (tcp_is_cwnd_limited(tp))
			{
				tp.snd_cwnd_used = 0;
				tp.snd_cwnd_stamp = tcp_jiffies32;
			}
			else
			{
				if (tp.packets_out > tp.snd_cwnd_used)
				{
					tp.snd_cwnd_used = tp.packets_out;
				}

				if (sock_net(tp).ipv4.sysctl_tcp_slow_start_after_idle &&
					tcp_jiffies32 - tp.snd_cwnd_stamp >= tp.icsk_rto && ca_ops.cong_control == null)
				{
					tcp_cwnd_application_limited(tp);
				}

				if (tcp_write_queue_empty(tp) && BoolOk((1 << tp.sk_state) & (TCPF_ESTABLISHED | TCPF_CLOSE_WAIT)))
				{
					tcp_chrono_start(tp, tcp_chrono.TCP_CHRONO_SNDBUF_LIMITED);
				}
			}
		}

		static bool tcp_schedule_loss_probe(tcp_sock tp, bool advancing_rto)
		{
			uint timeout, timeout_us, rto_delta_us;
			int early_retrans = sock_net(tp).ipv4.sysctl_tcp_early_retrans;
			if ((early_retrans != 3 && early_retrans != 4) ||
				tp.packets_out == 0 || !tcp_is_sack(tp) ||
				(tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_Open &&
				 tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_CWR))
			{
				return false;
			}

			if (tp.srtt_us > 0)
			{
				timeout_us = (uint)(tp.srtt_us >> 2);
				if (tp.packets_out == 1)
				{
					timeout_us += (uint)tcp_rto_min_us(tp);
				}
				else
				{
					timeout_us += TCP_TIMEOUT_MIN_US * 1000;
				}
				timeout = timeout_us;
			}
			else
			{
				timeout = TCP_TIMEOUT_INIT;
			}

			rto_delta_us = (uint)(advancing_rto ? tp.icsk_rto : tcp_rto_delta_us(tp));  /* How far in future is RTO? */
			if (rto_delta_us > 0)
			{
				timeout = Math.Min(timeout, rto_delta_us);
			}
			tcp_reset_xmit_timer(tp, ICSK_TIME_LOSS_PROBE, timeout, TCP_RTO_MAX);
			return true;
		}

		
		//push_one == 0：
        //行为：尝试发送尽可能多的数据包，直到发送队列为空或遇到其他限制条件。
		//用途：通常用于批量发送数据，适用于需要高效发送多个数据包的场景。
		//push_one == 1：
		//行为：只尝试发送一个数据包，然后立即退出。
		//用途：适用于需要发送单个数据包的场景，例如发送紧急数据或在某些特定的拥塞控制算法中。
		//push_one == 2：
		//行为：尝试发送尽可能多的数据包，但不发送超过一个 MSS 的数据。
		//用途：适用于需要发送少量数据的场景，例如在某些拥塞控制算法中，需要发送少量数据以探测网络状态。
		static bool tcp_write_xmit(tcp_sock tp, uint mss_now, int nonagle, int push_one)
		{
			sk_buff skb;
			uint sent_pkts = 0;
			uint cwnd_quota;
			int result;
			bool is_cwnd_limited = false, is_rwnd_limited = false;

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
			
			while ((skb = tcp_send_head(tp)) != null)
			{
				if (tcp_pacing_check(tp))
				{
					break;
				}

				cwnd_quota = tcp_cwnd_test(tp);
				if (cwnd_quota == 0) //测试未通过
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
				
				int missing_bytes = (int)(cwnd_quota * mss_now - skb.nBufferLength);
				if (missing_bytes > 0)
				{
					tcp_grow_skb(tp, skb, missing_bytes);
				}

				if (!tcp_snd_wnd_test(tp, skb, mss_now))
				{
					is_rwnd_limited = true;
					break;
				}

				if (!tcp_nagle_test(tp, skb, mss_now, (tcp_skb_is_last(tp, skb) ? nonagle : TCP_NAGLE_PUSH)))
				{
					break;
				}

				if (skb.nBufferLength > mss_now)
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

				tcp_transmit_skb(tp, skb, true);
				tcp_event_new_data_sent(tp, skb);
				tcp_minshall_update(tp, mss_now, skb);
				sent_pkts++;

				//if (push_one > 0)
				//{
				//	break;
				//}
			}

			if (is_rwnd_limited)
			{
				tcp_chrono_start(tp, tcp_chrono.TCP_CHRONO_RWND_LIMITED);
			}
			else
			{
				tcp_chrono_stop(tp, tcp_chrono.TCP_CHRONO_RWND_LIMITED);
			}

			is_cwnd_limited |= tcp_packets_in_flight(tp) >= tcp_snd_cwnd(tp);
			if (sent_pkts > 0 || is_cwnd_limited)
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
			return tp.packets_out == 0 && !tcp_write_queue_empty(tp);
		}

		static void tcp_send_loss_probe(tcp_sock tp)
		{
            NetLog.Log("tcp_send_loss_probe");
				
			int pcount = 0;
			uint mss = tcp_current_mss(tp);
			if (tp.tlp_high_seq > 0)
			{
                goto rearm_timer;
            }

			tp.tlp_retrans = 0;
            sk_buff skb = tcp_send_head(tp);
			if (skb != null && tcp_snd_wnd_test(tp, skb, mss))
			{
				pcount = (int)tp.packets_out;
				tcp_write_xmit(tp, mss, TCP_NAGLE_OFF, 2);
				if (tp.packets_out > pcount)
				{
                    goto probe_sent;
                }
                goto rearm_timer;
            }

			skb = skb_rb_last(tp.tcp_rtx_queue);
			if (skb == null)
			{
                tp.icsk_pending = 0;
                return;
			}

			if (skb_still_in_host_queue(tp, skb))
			{
                goto rearm_timer;
            }
			
			if (__tcp_retransmit_skb(tp, skb) != 0)
			{
                goto rearm_timer;
            }

			tp.tlp_retrans = 1;
        probe_sent:
            tp.tlp_high_seq = tp.snd_nxt;
            tp.icsk_pending = 0;
        rearm_timer:
            tcp_rearm_rto(tp);
        }

		static void tcp_event_new_data_sent(tcp_sock tp, sk_buff skb)
		{
			uint prior_packets = tp.packets_out;
			tp.snd_nxt = TCP_SKB_CB(skb).end_seq;
			__skb_unlink(skb, tp.sk_write_queue);
			tcp_rbtree_insert(tp.tcp_rtx_queue, skb);
            //NetLog.Log("tp.tcp_rtx_queue Count: " + rb_count(tp.tcp_rtx_queue));

            if (tp.highest_sack == null)
			{
				tp.highest_sack = skb;
			}

			tp.packets_out++;

			if (prior_packets == 0 || tp.icsk_pending == ICSK_TIME_LOSS_PROBE)
			{
				tcp_rearm_rto(tp);
			}
        }
		
		static int tcp_xmit_probe_skb(tcp_sock tp, int urgent)
		{
			sk_buff skb = new sk_buff();
			if (skb == null)
			{
				return -1;
			}

			uint urgent2 = (uint)(urgent > 0 ? 0 : 1);
			uint seq = tp.snd_una - urgent2;
            tcp_init_nondata_skb(skb, TCPHDR_ACK, ref seq);
			return tcp_transmit_skb(tp, skb, false);
		}

		//主要用于唤醒等待发送数据的进程。
		//当 TCP 连接上有新的空间可用时（例如，接收方确认了之前的数据或窗口扩大），内核会调用 tcp_write_wakeup 来通知应用程序可以继续发送数据。
		static int tcp_write_wakeup(tcp_sock tp)
		{
			sk_buff skb;
			if (tp.sk_state == TCP_CLOSE)
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

				if (seg_size < TCP_SKB_CB(skb).end_seq - TCP_SKB_CB(skb).seq || skb.nBufferLength > mss)
				{
					seg_size = Math.Min(seg_size, mss);
					TCP_SKB_CB(skb).tcp_flags |= TCPHDR_PSH;

					if (tcp_fragment(tp, tcp_queue.TCP_FRAG_IN_WRITE_QUEUE, skb, (int)seg_size, mss) > 0)
					{
						return -1;
					}
				}

				TCP_SKB_CB(skb).tcp_flags |= TCPHDR_PSH;
				err = tcp_transmit_skb(tp, skb, true);
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
					tcp_xmit_probe_skb(tp, 1);
				}
				return tcp_xmit_probe_skb(tp, 0);
			}
		}

		static void tcp_init_nondata_skb(sk_buff skb, byte flags, ref uint seq)
		{
			TCP_SKB_CB(skb).tcp_flags = flags;
			TCP_SKB_CB(skb).seq = seq;
			TCP_SKB_CB(skb).end_seq = seq;
		}

		static uint tcp_acceptable_seq(tcp_sock tp)
		{
			if (!before(tcp_wnd_end(tp), tp.snd_nxt) ||
				(tp.rx_opt.wscale_ok > 0 && ((tp.snd_nxt - tcp_wnd_end(tp)) < (1 << tp.rx_opt.rcv_wscale)))
				)
			{
				return tp.snd_nxt;
			}
			else
			{
				return tcp_wnd_end(tp);
			}
		}

		static void tcp_send_active_reset(tcp_sock tp, sk_rst_reason reason)
		{
			sk_buff skb = tcp_stream_alloc_skb(tp);
			uint seq = tcp_acceptable_seq(tp);
            tcp_init_nondata_skb(skb, TCPHDR_ACK | TCPHDR_RST, ref seq);
			tcp_mstamp_refresh(tp);
			tcp_transmit_skb(tp, skb, false);
        }

		static void tcp_tsq_write(tcp_sock tp)
		{
			if (BoolOk((1 << tp.sk_state) & TCPF_ESTABLISHED | TCPF_FIN_WAIT1 | TCPF_CLOSING | TCPF_CLOSE_WAIT | TCPF_LAST_ACK))
			{
				if (tp.lost_out > tp.retrans_out && tcp_snd_cwnd(tp) > tcp_packets_in_flight(tp))
				{
					tcp_mstamp_refresh(tp);
					tcp_xmit_retransmit_queue(tp);
				}
				tcp_write_xmit(tp, tcp_current_mss(tp), tp.nonagle, 0);
			}
		}

		static void tcp_tsq_handler(tcp_sock tp)
		{
			if (!sock_owned_by_user(tp))
			{
				tcp_tsq_write(tp);
			}
			else
			{
				tp.sk_tsq_flags = tp.sk_tsq_flags | (byte)tsq_enum.TCP_TSQ_DEFERRED;
			}
		}

		static hrtimer_restart tcp_pace_kick(tcp_sock tp)
		{
			tcp_tsq_handler(tp);
			return hrtimer_restart.HRTIMER_NORESTART;
		}

		static void tcp_cwnd_restart(tcp_sock tp, long delta)
		{
			uint restart_cwnd = tcp_init_cwnd(tp, __sk_dst_get(tp));
			uint cwnd = tcp_snd_cwnd(tp);

			tcp_ca_event_func(tp, tcp_ca_event.CA_EVENT_CWND_RESTART);

			tp.snd_ssthresh = tcp_current_ssthresh(tp);
			restart_cwnd = Math.Min(restart_cwnd, cwnd);

			while ((delta -= tp.icsk_rto) > 0 && cwnd > restart_cwnd)
			{
				cwnd >>= 1;
			}

			tcp_snd_cwnd_set(tp, Math.Max(cwnd, restart_cwnd));
			tp.snd_cwnd_stamp = tcp_jiffies32;
			tp.snd_cwnd_used = 0;
		}

		static void tcp_mtup_init(tcp_sock tp)
		{
			net net = sock_net(tp);
			tp.icsk_mtup.enabled = net.ipv4.sysctl_tcp_mtu_probing > 1;
			tp.icsk_mtup.search_high = tp.rx_opt.mss_clamp + sizeof_tcphdr;
			tp.icsk_mtup.search_low = (int)tcp_mss_to_mtu(tp, (uint)net.ipv4.sysctl_tcp_base_mss);
			tp.icsk_mtup.probe_size = 0;
			if (tp.icsk_mtup.enabled)
			{
				tp.icsk_mtup.probe_timestamp = tcp_jiffies32;
			}
		}

		static uint tcp_sync_mss(tcp_sock tp, uint pmtu)
		{
			if (tp.icsk_mtup.search_high > pmtu)
			{
				tp.icsk_mtup.search_high = (int)pmtu;
			}

			int mss_now = tcp_mtu_to_mss(tp, (int)pmtu);
			mss_now = tcp_bound_to_half_wnd(tp, mss_now);
			tp.icsk_pmtu_cookie = pmtu;
			if (tp.icsk_mtup.enabled)
			{
				mss_now = Math.Min(mss_now, tcp_mtu_to_mss(tp, tp.icsk_mtup.search_low));
			}

			tp.mss_cache = (uint)mss_now;
			return (uint)mss_now;
		}

		static long tcp_delack_max(tcp_sock tp)
		{
			long delack_from_rto_min = Math.Max(tcp_rto_min(tp), 2) - 1;
			return Math.Min(tp.icsk_delack_max, delack_from_rto_min);
		}

		static void tcp_send_delayed_ack(tcp_sock tp)
		{
				long ato = tp.icsk_ack.ato;
				long timeout;

			if (ato > TCP_DELACK_MIN)
			{
				long max_ato = HZ / 2;

				if (inet_csk_in_pingpong_mode(tp) || BoolOk(tp.icsk_ack.pending & (byte)inet_csk_ack_state_t.ICSK_ACK_PUSHED))
				{
					max_ato = TCP_DELACK_MAX;
				}

				if (tp.srtt_us > 0)
				{
					long rtt = Math.Max(tp.srtt_us >> 3, TCP_DELACK_MIN);
					if (rtt < max_ato)
					{
						max_ato = rtt;
					}
				}

				ato = Math.Min(ato, max_ato);
			}

			ato = Math.Min(ato, tcp_delack_max(tp));

			timeout = tcp_jiffies32 + ato;
			if (BoolOk(tp.icsk_ack.pending & (byte)inet_csk_ack_state_t.ICSK_ACK_TIMER))
			{
				if (time_before_eq(tp.icsk_ack.timeout, tcp_jiffies32 + (ato >> 2)))
				{
					tcp_send_ack(tp);
					return;
				}

				if (!time_before(timeout, tp.icsk_ack.timeout))
				{
					timeout = tp.icsk_ack.timeout;
				}
			}

			tp.icsk_ack.pending |= (byte)inet_csk_ack_state_t.ICSK_ACK_SCHED | (byte)inet_csk_ack_state_t.ICSK_ACK_TIMER;
			tp.icsk_ack.timeout = timeout;
			sk_reset_timer(tp, tp.icsk_delack_timer, timeout);
		}

		static ushort tcp_advertise_mss(tcp_sock tp)
		{
			dst_entry dst = __sk_dst_get(tp);
			ushort mss = tp.advmss;
			if (dst != null)
			{
				ushort metric = dst_metric_advmss(dst);
				if (metric < mss)
				{
					mss = metric;
					tp.advmss = mss;
				}
			}
			return mss;
		}

		static void tcp_select_initial_window(tcp_sock tp, int __space, uint mss, int wscale_ok, uint init_rcv_wnd,
                  ref uint rcv_wnd, ref uint __window_clamp, ref byte rcv_wscale)
		{
			uint space = (uint)(__space < 0 ? 0 : __space);
			uint window_clamp = __window_clamp;
			if (window_clamp == 0)
			{
				window_clamp = ushort.MaxValue << (int)TCP_MAX_WSCALE;
			}

			space = Math.Min(window_clamp, space);
			if (space > mss)
			{
				space = (uint)rounddown((int)space, (int)mss);
			}

            rcv_wnd = space;
            if (init_rcv_wnd > 0)
			{
				rcv_wnd = Math.Min(rcv_wnd, init_rcv_wnd * mss);
			}

			rcv_wscale = 0;
			if (wscale_ok > 0)
			{
				space = (uint)Math.Max(space, sock_net(tp).ipv4.sysctl_tcp_rmem[2]);
				space = (uint)Math.Max(space, window_clamp);
                rcv_wscale = (byte)Math.Clamp(ilog2(space) - 15, 0, TCP_MAX_WSCALE);
				NetLog.Assert(rcv_wscale > 0, "rcv_wscale:" + rcv_wscale);
			}
			__window_clamp = (uint)Math.Min(ushort.MaxValue << rcv_wscale, window_clamp);
		}

		static void tcp_connect_queue_skb(tcp_sock tp, sk_buff skb)
		{
			tcp_skb_cb tcb = TCP_SKB_CB(skb);
			tcb.end_seq += (uint)skb.nBufferLength;
			sk_wmem_queued_add(tp, skb.nBufferLength);
			sk_mem_charge(tp, skb.nBufferLength);
			tp.write_seq = tcb.end_seq;
			tp.packets_out++;
		}

		static void tcp_ecn_send_syn(tcp_sock tp, sk_buff skb)
		{
            tp.ecn_flags = 0;
            bool use_ecn = sock_net(tp).ipv4.sysctl_tcp_ecn == 1 || tcp_ca_needs_ecn(tp);
			if (!use_ecn)
			{
				dst_entry dst = __sk_dst_get(tp);
				if (dst != null && dst_feature(dst, RTAX_FEATURE_ECN) > 0)
				{
					use_ecn = true;
				}
			}

			if (use_ecn)
			{
				TCP_SKB_CB(skb).tcp_flags |= TCPHDR_ECE | TCPHDR_CWR;
				tp.ecn_flags = TCP_ECN_OK;
				if (tcp_ca_needs_ecn(tp))
				{
					INET_ECN_xmit(tp);
				}
			}
		}

		static int tcp_connect(tcp_sock tp)
		{
			tcp_connect_init(tp);
			tcp_mstamp_refresh(tp);
			tp.retrans_stamp = tcp_time_stamp_ms(tp);
            tp.snd_nxt = tp.write_seq;
			tp.pushed_seq = tp.write_seq;
			return 0;
		}

	}
}

