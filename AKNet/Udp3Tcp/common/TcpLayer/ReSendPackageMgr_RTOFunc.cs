/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.LinuxTcp;
using System;

namespace AKNet.Udp3Tcp.Common
{
    internal partial class ReSendPackageMgr
    {
        private const ushort HZ = 1000;
        private const long TCP_RTO_MAX = 120 * HZ;
        private const long TCP_RTO_MIN = HZ / 5;
        private const long TCP_RTO_INIT = 1 * HZ;

        private long srtt_us; 
        private long rttvar_us;
        private long mdev_us;
        private long mdev_max_us;
        private long icsk_rto;

        private uint rtt_seq;

        void tcp_rtt_estimator(long mrtt_us)
        {
            long m = mrtt_us;
            long srtt = srtt_us;

            if (srtt != 0)
            {
                m -= (srtt >> 3);
                srtt += m;
                if (m < 0)
                {
                    m = -m;
                    m -= (mdev_us >> 2);
                    if (m > 0)
                    {
                        m >>= 3;
                    }
                }
                else
                {
                    m -= (mdev_us >> 2);
                }

                mdev_us += m;
                if (mdev_us > mdev_max_us)
                {
                    mdev_max_us = mdev_us;
                    if (mdev_max_us > rttvar_us)
                    {
                        rttvar_us = mdev_max_us;
                    }
                }

                if ( (int)(mTcpSlidingWindow.nBeginOrderId - rtt_seq) > 0)
                {
                    if (mdev_max_us < rttvar_us)
                    {
                        rttvar_us -= (rttvar_us - mdev_max_us) >> 2;
                    }
                    rtt_seq = nCurrentWaitSendOrderId;
                    mdev_max_us = TCP_RTO_MIN;
                }
            }
            else
            {
                srtt = m << 3;
                mdev_us = m << 1;
                rttvar_us = Math.Max(mdev_us, TCP_RTO_MIN);
                mdev_max_us = rttvar_us;
                rtt_seq = nCurrentWaitSendOrderId;
            }
            srtt_us = Math.Max(1U, srtt);
        }

        void tcp_set_rto()
        {
            icsk_rto = 0;
            icsk_rto = (srtt_us >> 3) + rttvar_us;

            if (icsk_rto > tcp_sock.TCP_RTO_MAX)
            {
                icsk_rto = tcp_sock.TCP_RTO_MAX;
            }
        }

        public void FinishRttSuccess(long nRtt)
        {
            tcp_rtt_estimator(nRtt);
            tcp_set_rto();
        }

        public long GetRTOTime()
        {
            return icsk_rto;
        }
    }

    internal class TcpStanardRTOTimer
    {
        long nStartTime = 0;

        private long GetNowTime()
        {
            return UdpStaticCommon.GetNowTime();
        }

        public void BeginRtt()
        {
            nStartTime = GetNowTime();
        }

        public void FinishRtt(ReSendPackageMgr mReSendPackageMgr)
        {
            long nRtt = GetNowTime() - nStartTime;
            mReSendPackageMgr.FinishRttSuccess(nRtt);
            UdpStatistical.AddRtt(nRtt);
        }
    }
}
