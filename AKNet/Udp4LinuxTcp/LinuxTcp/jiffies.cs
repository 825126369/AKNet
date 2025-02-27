/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        public static bool time_after(long a, long b)
        {
            return a > b;
        }

        public static bool time_before(long a, long b)
        {
            return time_after(b, a);
        }

        public static bool time_after_eq(long a, long b)
        {
            return a >= b;
        }

        public static bool time_before_eq(long a, long b)
        {
            return time_after_eq(b, a);
        }
    }
}
