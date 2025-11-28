/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:44
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Diagnostics;
using System.Net.Sockets;

namespace AKNet.Udp3Tcp.Common
{
    internal static class UdpStaticCommon
    {
        static readonly Stopwatch mStopwatch = Stopwatch.StartNew();
        public static long GetNowTime()
        {
            return mStopwatch.ElapsedMilliseconds;
        }
    }
}
