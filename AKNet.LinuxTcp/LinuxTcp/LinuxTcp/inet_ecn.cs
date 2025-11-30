/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:21
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp.Common
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
