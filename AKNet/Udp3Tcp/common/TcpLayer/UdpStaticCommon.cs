/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:29
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
