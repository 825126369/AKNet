/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:27
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp3Tcp.Common
{
    internal static class UdpNetCommand
	{
		public const byte COMMAND_PACKAGE_CHECK_SURE_ORDERID = 1;
		public const byte COMMAND_HEARTBEAT = 2;
		public const byte COMMAND_CONNECT = 3;
		public const byte COMMAND_DISCONNECT = 4;
        public const byte COMMAND_MAX = 10;

		public static bool orInnerCommand(ushort id)
		{
			return id >= 1 && id <= COMMAND_MAX;
		}
	}
}
