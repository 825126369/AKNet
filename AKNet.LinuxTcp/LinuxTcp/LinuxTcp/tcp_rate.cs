/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:42
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
		public static void tcp_rate_skb_sent(tcp_sock tp, sk_buff skb)
		{
			if (tp.packets_out == 0)
			{
				long tstamp_us = tcp_skb_timestamp(skb);
				tp.first_tx_mstamp = tstamp_us;
				tp.delivered_mstamp = tstamp_us;
			}

			TCP_SKB_CB(skb).tx.first_tx_mstamp = tp.first_tx_mstamp;
			TCP_SKB_CB(skb).tx.delivered_mstamp = tp.delivered_mstamp;
			TCP_SKB_CB(skb).tx.delivered = tp.delivered;
			TCP_SKB_CB(skb).tx.delivered_ce = tp.delivered_ce;
			TCP_SKB_CB(skb).tx.is_app_limited = (uint)(tp.app_limited > 0 ? 1 : 0);
		}

		static void tcp_rate_check_app_limited(tcp_sock tp)
		{
			if (tp.write_seq - tp.snd_nxt < tp.mss_cache &&
				tcp_packets_in_flight(tp) < tcp_snd_cwnd(tp) &&
				tp.lost_out <= tp.retrans_out)
			{
				uint app_limited = tp.delivered + tcp_packets_in_flight(tp);
				tp.app_limited = (app_limited > 0) ? app_limited : 1;
			}
		}

		static void tcp_rate_skb_delivered(tcp_sock tp, sk_buff skb, rate_sample rs)
		{
			tcp_skb_cb scb = TCP_SKB_CB(skb);
			long tx_tstamp;

			if (scb.tx.delivered_mstamp == 0)
			{
				return;
			}

			tx_tstamp = tcp_skb_timestamp(skb);

			if (rs.prior_delivered == 0 || tcp_skb_sent_after(tx_tstamp, tp.first_tx_mstamp, scb.end_seq, rs.last_end_seq))
			{
				rs.prior_delivered_ce = scb.tx.delivered_ce;
				rs.prior_delivered = scb.tx.delivered;
				rs.prior_mstamp = scb.tx.delivered_mstamp;
				rs.is_app_limited = BoolOk(scb.tx.is_app_limited);
				rs.is_retrans = BoolOk(scb.sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_RETRANS);
				rs.last_end_seq = scb.end_seq;
				tp.first_tx_mstamp = tx_tstamp;
				rs.interval_us = tcp_stamp_us_delta(tp.first_tx_mstamp, scb.tx.first_tx_mstamp);
			}

			if (BoolOk(scb.sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED))
			{
				scb.tx.delivered_mstamp = 0;
			}
		}

		static void tcp_rate_gen(tcp_sock tp, uint delivered, uint lost, bool is_sack_reneg, rate_sample rs)
		{
			uint snd_us, ack_us;
			if (tp.app_limited > 0 && after(tp.delivered, tp.app_limited))
			{
				tp.app_limited = 0;
			}

			if (delivered > 0)
			{
				tp.delivered_mstamp = tp.tcp_mstamp;
			}

			rs.acked_sacked = delivered;
			rs.losses = (int)lost;

			if (rs.prior_mstamp == 0 || is_sack_reneg)
			{
				rs.delivered = -1;
				rs.interval_us = -1;
				return;
			}
			rs.delivered = (int)(tp.delivered - rs.prior_delivered);

			rs.delivered_ce = (int)(tp.delivered_ce - rs.prior_delivered_ce);
			rs.delivered_ce &= TCPCB_DELIVERED_CE_MASK;

			snd_us = (uint)rs.interval_us;
			ack_us = (uint)tcp_stamp_us_delta(tp.tcp_mstamp, rs.prior_mstamp);
			rs.interval_us = Math.Max(snd_us, ack_us);

			rs.snd_interval_us = snd_us;
			rs.rcv_interval_us = ack_us;

			if (rs.interval_us < tcp_min_rtt(tp))
			{
				rs.interval_us = -1;
				return;
			}

			if (!rs.is_app_limited || (rs.delivered * tp.rate_interval_us >= tp.rate_delivered * rs.interval_us))
			{
				tp.rate_delivered = (uint)rs.delivered;
				tp.rate_interval_us = (uint)rs.interval_us;
				tp.rate_app_limited = rs.is_app_limited;
			}
		}

	}
}
