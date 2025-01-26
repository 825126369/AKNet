/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp
{
    internal static partial class LinuxTcpFunc
	{
		static int ip_queue_xmit(tcp_sock tp, sk_buff skb)
		{
            tcp_hdr(skb).tot_len = (ushort)skb.nBufferLength;
            tcp_hdr(skb).WriteTo(skb);
            IPLayerSendStream(tp, skb);
            return 0;
        }
    }
}
