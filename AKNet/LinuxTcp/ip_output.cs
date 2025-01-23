/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
	{
		static int ip_queue_xmit(tcp_sock tp, sk_buff skb, flowi fl)
		{
			return __ip_queue_xmit(tp, skb, fl, tp.tos);
		}

		static int __ip_queue_xmit(tcp_sock tp, sk_buff skb, flowi fl, byte tos)
		{
            skb_set_mac_header(skb, sizeof_ethhdr);
            skb_set_network_header(skb, sizeof_iphdr);

            ip_hdr(skb).tot_len = (ushort)skb.len;

            Span<byte> mBuffer = skb.mBuffer.AsSpan().Slice(skb.mac_header);
            eth_hdr(skb).WriteTo(mBuffer);

            mBuffer = skb.mBuffer.AsSpan().Slice(skb.network_header);
			ip_hdr(skb).WriteTo(mBuffer);

			IPLayerSendStream(tp, skb);
            return 0;
        }

        static int __ip_local_out(net net, tcp_sock tp, sk_buff skb)
        {
	            iphdr iph = ip_hdr(skb);

                IP_INC_STATS(net, IPSTATS_MIB_OUTREQUESTS);
                
                iph_set_totlen(iph, skb->len);
                ip_send_check(iph);

                /* if egress device is enslaved to an L3 master device pass the
                 * skb to its handler for processing
                 */
                skb = l3mdev_ip_out(sk, skb);
	        if (unlikely(!skb))
		        return 0;

	        skb->protocol = htons(ETH_P_IP);

	        return nf_hook(NFPROTO_IPV4, NF_INET_LOCAL_OUT,
                       net, sk, skb, NULL, skb_dst(skb)->dev,
                       dst_output);
        }

    }
}
