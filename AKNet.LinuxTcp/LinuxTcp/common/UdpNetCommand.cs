/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:57
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp.Common
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
