/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:24
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp.Common
{
    internal class timespec64
    {
        public long tv_sec;            /* seconds */
        public long tv_nsec;       /* nanoseconds */
    }

    internal static partial class LinuxTcpFunc
    {
        public const long MSEC_PER_SEC = 1000;
        public const long MSEC_PER_USEC = 1000;
        public const long MSEC_PER_NSEC = 1000000;
        public const long USEC_PER_MSEC = 1000;
        public const long NSEC_PER_USEC = 1000; //1 微秒 = 1000 纳秒。
        public const long NSEC_PER_MSEC = 1000000;//1 豪秒 = 1000000 纳秒。
        public const long USEC_PER_SEC = 1000000;
        public const long NSEC_PER_SEC = 1000000000;//1秒 = 1 000 000 000 纳秒。
    }
}
