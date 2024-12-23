﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Diagnostics;

namespace AKNet.Udp.POINTTOPOINT.Common
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
