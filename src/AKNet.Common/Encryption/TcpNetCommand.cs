/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:44
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.CompilerServices;

namespace AKNet.Common
{
    internal static class TcpNetCommand
    {
        public const ushort COMMAND_HEARTBEAT = 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orInnerCommand(ushort nPackageId)
        {
            return nPackageId == COMMAND_HEARTBEAT;
        }
    }
}

