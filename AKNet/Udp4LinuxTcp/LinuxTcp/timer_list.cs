/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:24
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class TimerList
    {
        private readonly TimeOutGenerator _timer = new TimeOutGenerator();
        private tcp_sock tcp_sock_Instance;
        private Action<tcp_sock> _callback;
        private bool bRun = false;

        public TimerList(long period, Action<tcp_sock> callback, tcp_sock tcp_sock_Instance)
        {
            _timer.SetInternalTime(period / 1000.0);
            this.tcp_sock_Instance = tcp_sock_Instance;
            this._callback = callback;
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
        }

        public void Stop()
        {
            bRun = false;
        }

        public bool ModTimer(long newPeriod)
        {
            if (newPeriod <= 0)
            {
                throw new ArgumentException("New period must be greater than zero.", nameof(newPeriod));
            }
            
            _timer.SetInternalTime(newPeriod / 1000.0);
            return true;
        }

        public void Reset()
        {
            Stop();
        }
    }
}
