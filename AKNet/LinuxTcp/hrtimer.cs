/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Diagnostics;
using System.Threading;
using System;

namespace AKNet.LinuxTcp
{
    public class HRTimer : IDisposable
    {
        private readonly Timer _timer;
        private readonly Stopwatch _stopwatch;
        private TimeSpan _period;
        private TimerCallback _callback;

        public HRTimer(TimeSpan period, TimerCallback callback)
        {
            if (period <= TimeSpan.Zero)
                throw new ArgumentException("Period must be greater than zero.", nameof(period));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            _period = period;
            _callback = callback;
            _stopwatch = Stopwatch.StartNew();
            _timer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void OnTimerElapsed(object state)
        {
            long elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
            _callback(elapsedMilliseconds);
        }

        public void Start()
        {
            _timer.Change(_period, _period);
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public bool TryToCancel()
        {
            Stop();
            return true;
        }

        public bool ModTimer(TimeSpan newPeriod)
        {
            if (newPeriod <= TimeSpan.Zero)
                throw new ArgumentException("New period must be greater than zero.", nameof(newPeriod));

            _period = newPeriod;
            _timer.Change(_period, _period);

            return true;
        }

        public void Reset()
        {
            Stop();
            _stopwatch.Restart();
        }

        public void Dispose()
        {
            Stop();
            _stopwatch.Stop();
            _timer?.Dispose();
        }
    }

    // 示例用法
    class Program
    {
        static void Test(string[] args)
        {
            TimerCallback callback = (object state) =>
            {
                Console.WriteLine($"Timer ticked at {DateTime.Now:HH:mm:ss.fff}");
            };

            using (HRTime hrTime = new HRTime(TimeSpan.FromMilliseconds(500), callback))
            {
                hrTime.Start();

                // 模拟运行一段时间
                Thread.Sleep(3000);

                hrTime.Reset();

                // 再次模拟运行一段时间
                Thread.Sleep(3000);

                // 停止计时器并退出
                hrTime.Stop();
            }
        }
    }
}
