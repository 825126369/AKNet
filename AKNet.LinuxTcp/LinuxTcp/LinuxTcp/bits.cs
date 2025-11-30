/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:21
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.CompilerServices;

namespace AKNet.LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BIT(int nr)
        {
            return (ulong)(1 << nr);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BoolOk(long nr)
        {
            return nr != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BoolOk(ulong nr)
        {
            return nr != 0;
        }
    }
}
