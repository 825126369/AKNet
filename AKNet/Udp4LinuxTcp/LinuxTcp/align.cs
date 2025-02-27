/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        public static int ALIGN(int value, int alignment)
        {
            if (alignment <= 0)
                throw new ArgumentException("Alignment must be a positive number.", nameof(alignment));

            return (value + alignment - 1) & ~(alignment - 1);
        }

        public static long ALIGN(long value, long alignment)
        {
            if (alignment <= 0)
                throw new ArgumentException("Alignment must be a positive number.", nameof(alignment));

            return (value + alignment - 1) & ~(alignment - 1);
        }
    }
}
