/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:28
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
	internal static class UdpNetCommand
	{
		public const ushort COMMAND_PACKAGE_CHECK_SURE_ORDERID = 1;
		public const ushort COMMAND_HEARTBEAT = 2;
		public const ushort COMMAND_CONNECT = 3;
		public const ushort COMMAND_DISCONNECT = 4;

		public static bool orNeedCheck(ushort id)
		{
			return id > 10;
		}

		public static bool orInnerCommand(ushort id)
		{
			return id >= 1 && id <= 10;
		}
	}
}
