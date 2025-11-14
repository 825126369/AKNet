/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:40
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
