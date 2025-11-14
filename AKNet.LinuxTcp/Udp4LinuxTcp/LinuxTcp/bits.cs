/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.CompilerServices;

namespace AKNet.Udp4LinuxTcp.Common
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
