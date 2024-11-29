/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp2Tcp.Common
{
	internal static class UdpNetCommand
	{
		public const ushort COMMAND_PACKAGE_CHECK_SURE_ORDERID = 1;
		public const ushort COMMAND_HEARTBEAT = 2;
		public const ushort COMMAND_CONNECT = 3;
		public const ushort COMMAND_DISCONNECT = 4;

        public const ushort COMMAND_MAX = 10;

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
