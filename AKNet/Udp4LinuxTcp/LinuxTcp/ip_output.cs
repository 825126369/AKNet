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
		static int ip_queue_xmit(tcp_sock tp, sk_buff skb)
		{
            skb_set_network_header(skb, -sizeof_iphdr);
            skb_push(skb, sizeof_iphdr);
            skb_set_mac_header(skb, -sizeof_ethhdr);
            skb_push(skb, sizeof_ethhdr);

            ip_hdr(skb).tot_len = (ushort)skb.len;

            Span<byte> mBuffer = skb.mBuffer.AsSpan().Slice(skb.mac_header);
            eth_hdr(skb).WriteTo(mBuffer);

            mBuffer = skb.mBuffer.AsSpan().Slice(skb.network_header);
            ip_hdr(skb).WriteTo(mBuffer);

            IPLayerSendStream(tp, skb);
            return 0;
        }
    }
}
