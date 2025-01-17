/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Threading;

namespace AKNet.LinuxTcp
{
    internal enum hrtimer_restart
    {
        HRTIMER_NORESTART,  /* Timer is not restarted */
        HRTIMER_RESTART,    /* Timer must be restarted */
    }

    internal class HRTimer : IDisposable
    {
        private readonly Timer _timer;
        private long _period;

        private tcp_sock tcp_sock_Instance;
        private Func<tcp_sock, hrtimer_restart> _callback;


        public const byte HRTIMER_STATE_INACTIVE = 0x00;
        public const byte HRTIMER_STATE_ENQUEUED = 0x01;
        public byte state;

        public HRTimer(long period, Func<tcp_sock, hrtimer_restart> callback, tcp_sock tcp_sock_Instance)
        {
            _period = period;

            this.tcp_sock_Instance = tcp_sock_Instance;
            this._callback = callback;
            _timer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            this.state = HRTIMER_STATE_INACTIVE;
        }

        private void OnTimerElapsed(object state)
        {
            _callback(tcp_sock_Instance);
        }

        public void Start()
        {
            _timer.Change(_period, _period);
            this.state = HRTIMER_STATE_ENQUEUED;
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
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

        public bool ModTimer(long newPeriod)
        {
            if (newPeriod <= 0)
                throw new ArgumentException("New period must be greater than zero.", nameof(newPeriod));

            _period = newPeriod;
            _timer.Change(_period, _period);

            return true;
        }

        public void Reset()
        {
            Stop();
        }

        public void Dispose()
        {
            Stop();
            _timer?.Dispose();
        }
    }
}
