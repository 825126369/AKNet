/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp2MSQuic.Common
{
    internal static class TcpNetCommand
    {
        public const ushort COMMAND_HEARTBEAT = 1;
        public static bool orInnerCommand(ushort nPackageId)
        {
            return nPackageId == COMMAND_HEARTBEAT;
        }
    }
}

