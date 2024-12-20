/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        public static int tcp_rack_skb_timeout(tcp_sock tp, sk_buff skb, uint reo_wnd)
        {
	        return (int)(tp.rack.rtt_us + reo_wnd - tcp_stamp_us_delta(tp.tcp_mstamp, tcp_skb_timestamp_us(skb)));
        }
    }
}
