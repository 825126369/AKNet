/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:09
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp.Common
{
    internal class timespec64
    {
        public long tv_sec;            /* seconds */
        public long tv_nsec;       /* nanoseconds */
    }

    internal static partial class LinuxTcpFunc
    {
        //public const long MSEC_PER_SEC = 1000;
        //public const long MSEC_PER_USEC = 1000;
        //public const long MSEC_PER_NSEC = 1000000;
        //public const long USEC_PER_MSEC = 1000;
        //public const long NSEC_PER_USEC = 1000; //1 微秒 = 1000 纳秒。
        //public const long NSEC_PER_MSEC = 1000000;//1 豪秒 = 1000000 纳秒。
        //public const long USEC_PER_SEC = 1000000;
        //public const long NSEC_PER_SEC = 1000000000;//1秒 = 1 000 000 000 纳秒。
    }
}
