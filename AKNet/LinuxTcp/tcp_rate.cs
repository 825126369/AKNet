/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
		public static void tcp_rate_skb_sent(tcp_sock tp, sk_buff skb)
		{
			if (tp.packets_out == 0)
			{
				long tstamp_us = tcp_skb_timestamp_us(skb);
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
				sk_wmem_alloc_get(tp) < SKB_TRUESIZE(1) &&
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

			tx_tstamp = tcp_skb_timestamp_us(skb);

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

	}
}
