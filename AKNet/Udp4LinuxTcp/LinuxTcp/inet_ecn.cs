/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp.Common
{
	internal static partial class LinuxTcpFunc
	{
		static void INET_ECN_xmit(tcp_sock tp)
		{
			tp.tos |= INET_ECN_ECT_0;
		}

		static void INET_ECN_dontxmit(tcp_sock tp)
		{
			tp.tos = (byte)(tp.tos & (~INET_ECN_MASK));
		}
	}
}
