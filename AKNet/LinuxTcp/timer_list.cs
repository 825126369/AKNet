using System;
using System.Threading;

namespace AKNet.LinuxTcp
{
    internal class TimerList : IDisposable
    {
        private readonly Timer _timer;
        private long _period;

        private tcp_sock tcp_sock_Instance;
        private Action<tcp_sock> _callback;
        
        public const byte HRTIMER_STATE_INACTIVE = 0x00;
        public const byte HRTIMER_STATE_ENQUEUED = 0x01;
        public byte state;

        public TimerList(long period, Action<tcp_sock> callback, tcp_sock tcp_sock_Instance)
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
