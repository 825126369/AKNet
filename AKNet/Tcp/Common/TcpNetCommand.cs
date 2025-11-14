/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:51
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Tcp.Common
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

