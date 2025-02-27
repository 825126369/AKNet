/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
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
