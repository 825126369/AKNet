/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:42
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.LinuxTcp.Common
{
    internal class TimerList
    {
        private readonly TimeOutGenerator _timer = new TimeOutGenerator();
        private tcp_sock tcp_sock_Instance;
        private Action<tcp_sock> _callback;
        private bool bRun = false;

        public TimerList(long period_ms, Action<tcp_sock> callback, tcp_sock tcp_sock_Instance)
        {
            this._timer.SetExpiresTime(period_ms);
            this.tcp_sock_Instance = tcp_sock_Instance;
            this._callback = callback;
            this.bRun = false;
        }

        private long MS_TO_MS(long period_ms)
        {
            return period_ms;
        }

        public void Update(double elapsed)
        {
            if (bRun && _timer.orTimeOut())
            {
                _callback(tcp_sock_Instance);
            }
        }

        private void Start()
        {
            bRun = true;
        }

        public void Stop()
        {
            bRun = false;
        }

        public void ModTimer(long period_ms)
        {
            if (period_ms > 0)
            {
                _timer.SetExpiresTime(period_ms);
                Start();
            }
            else
            {
                Stop();
            }
        }

        public void Reset()
        {
            Stop();
        }
    }
}
