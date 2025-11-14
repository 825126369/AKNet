/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
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
                return x / y;
            }
            else
            {
                return x / y + 1;
            }
        }
    }
}
