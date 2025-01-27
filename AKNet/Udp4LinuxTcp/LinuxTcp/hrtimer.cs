/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal enum hrtimer_restart
    {
        HRTIMER_NORESTART,  /* Timer is not restarted */
        HRTIMER_RESTART,    /* Timer must be restarted */
    }

    internal class HRTimer
    {
        private readonly TimeOutGenerator _timer = new TimeOutGenerator();
        private bool bRun = false;

        private tcp_sock tcp_sock_Instance;
        private Func<tcp_sock, hrtimer_restart> _callback;

        public const byte HRTIMER_STATE_INACTIVE = 0x00;
        public const byte HRTIMER_STATE_ENQUEUED = 0x01;
        public byte state;

        public HRTimer(long period, Func<tcp_sock, hrtimer_restart> callback, tcp_sock tcp_sock_Instance)
        {
            _timer.SetInternalTime(period / 1000.0);
            this.tcp_sock_Instance = tcp_sock_Instance;
            this._callback = callback;
            this.state = HRTIMER_STATE_INACTIVE;
            bRun = false;
        }

        public void Update(double elapsed)
        {
            if (bRun && _timer.orTimeOut(elapsed))
            {
                _callback(tcp_sock_Instance);
            }
        }

        public void Start()
        {
            bRun = true;
            this.state = HRTIMER_STATE_ENQUEUED;
        }

        public void Stop()
        {
            bRun = false;
            this.state = HRTIMER_STATE_INACTIVE;
        }

        public bool TryToCancel()
        {
            Stop();
            return true;
        }

        public bool hrtimer_is_queued()
        {
            return LinuxTcpFunc.BoolOk(state & HRTIMER_STATE_ENQUEUED);
        }

        public bool ModTimer(long period)
        {
            if (period <= 0)
                throw new ArgumentException("New period must be greater than zero.", nameof(period));

            _timer.SetInternalTime(period / 1000.0);

            return true;
        }

        public void Reset()
        {
            Stop();
        }
    }
}
