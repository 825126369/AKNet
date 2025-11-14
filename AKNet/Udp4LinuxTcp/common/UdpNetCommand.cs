/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp.Common
{
    internal static class UdpNetCommand
	{
		public const byte COMMAND_HEARTBEAT = 1;
		public const byte COMMAND_CONNECT = 2;
		public const byte COMMAND_DISCONNECT = 3;
        public const byte COMMAND_MAX = 10;

		public static bool orInnerCommand(ushort id)
		{
			return id >= 1 && id <= COMMAND_MAX;
		}
	}
}
