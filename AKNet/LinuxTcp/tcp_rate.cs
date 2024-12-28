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
    
    }
}
