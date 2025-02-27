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
        //它用于确保 TCP 的重传超时（RTO, Retransmission Timeout）不会超过用户设定的连接超时时间。
        static int rounddown(int __x, int y)
        {
            return __x - (__x % (y));
        }

        static int roundup_pow_of_two(int n)
        {
            if (n == 0) return 1;
            int result = 1;
            while (result < n) result <<= 1;
            return result;
        }

        static int min3(int x, int y, int z)
        {
            var t = Math.Min(x, y);
            t = Math.Min(z, t);
            return t;
        }

        static long DIV_ROUND_UP(long x, long y)
        {
            if (x % y == 0)
            {
                return x;
            }
            else
            {
                return x / y + 1;
            }
        }
    }
}
