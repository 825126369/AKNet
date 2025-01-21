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
            Span<byte> mBuffer = skb.mBuffer.AsSpan().Slice(skb.mac_header);
            eth_hdr(skb).WriteTo(mBuffer);

            mBuffer = skb.mBuffer.AsSpan().Slice(skb.network_header);
			ip_hdr(skb).WriteTo(mBuffer);

            mBuffer = skb.mBuffer.AsSpan().Slice(skb.transport_header);
            tcp_hdr(skb).WriteTo(mBuffer);

            return 0;
        }

    }
}
