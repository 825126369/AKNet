﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal class TcpStanardRTOFunc
    {
        const long DefaultRtt = 1000;
        const long DefaultRttStd = 50;

        long RttOld = 0;
        long RttNew = DefaultRtt;
        long RttAverage  = -1;
        long RttStdOld = 0;
        long RttStd = DefaultRttStd;
        long nStartTime = 0;
        

        private long GetNowTime()
        {
            return UdpStaticCommon.GetNowTime();
        }

        public void BeginRtt()
        {
            nStartTime = GetNowTime();
        }

        public void FinishRttSuccess()
        {
            long nRtt = GetNowTime() - nStartTime;

            RttOld = RttNew;
            RttNew = nRtt;
            RttAverage = (long)(0.125 * RttOld + (1 - 0.125) * RttNew);
            RttStdOld = RttStd;
            RttStd = (long)(0.25 * RttStdOld + (1 - 0.25) * Math.Abs(RttAverage - RttNew));
        }

        public long GetRTOTime()
        {
            if (RttAverage >= 0)
            {
                return RttAverage + 4 * RttStd;
            }
            return DefaultRtt;
        }
    }
}