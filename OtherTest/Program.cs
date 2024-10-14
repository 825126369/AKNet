using System.Diagnostics;
using System.Timers;
using XKNet.Common;

namespace OtherTest
{
    internal class Program
    {
        static CheckPackageInfo mCheckPackageInfo = new CheckPackageInfo();
        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToString());
            mCheckPackageInfo.DelayedCall4(10);

            Console.WriteLine(DateTime.Now.ToString());
            mCheckPackageInfo.DelayedCall3(10);

            Console.WriteLine(DateTime.Now.ToString());
            mCheckPackageInfo.DelayedCall2(10);

            Console.WriteLine(DateTime.Now.ToString());
            mCheckPackageInfo.DelayedCall1(10);

            Console.WriteLine(DateTime.Now.ToString());
            mCheckPackageInfo.DelayedCall0(10);

            UpdateMgr.Do(Update);
        }

        static void Update(double a)
        {

        }
    }

    internal class CheckPackageInfo
    {
        private readonly System.Threading.Timer mSystemThreadingTimer = null;
        private readonly System.Timers.Timer mSystemTimersTimer = null;
        CancellationTokenSource mDelayedCall4CancellationTokenSource = null;
        private readonly Stopwatch mStopwatch2 = new Stopwatch();
        public CheckPackageInfo()
        {
            mSystemThreadingTimer = new System.Threading.Timer(DelayedCall2Func);
            mSystemTimersTimer = new System.Timers.Timer();
            mSystemTimersTimer.Elapsed += DelayedCall3Func;
        }

        public void DelayedCall0(long millisecondsTimeout)
        {
            mDelayedCall4CancellationTokenSource = new CancellationTokenSource();
            mStopwatch2.Start();
            Task.Run(() =>
            {
                while (!mDelayedCall4CancellationTokenSource.IsCancellationRequested)
                {
                    if (mStopwatch2.ElapsedMilliseconds >= millisecondsTimeout)
                    {
                        DelayedCall0Func();
                        break;
                    }
                }
            });
        }

        private void DelayedCall0Func()
        {
            Console.WriteLine(DateTime.Now.ToString() + ":  DelayedCall1Func 00000");
        }

        public void DelayedCall1(long millisecondsTimeout)
        {
            mDelayedCall4CancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                Thread.Sleep((int)millisecondsTimeout);
                if (!mDelayedCall4CancellationTokenSource.IsCancellationRequested)
                {
                    DelayedCall1Func();
                }
            });
        }

        private void DelayedCall1Func()
        {
            Console.WriteLine(DateTime.Now.ToString() + ":  DelayedCall1Func 1111111");
        }
        
        public void DelayedCall2(long millisecondsTimeout)
        {
            mSystemThreadingTimer.Change(millisecondsTimeout, millisecondsTimeout);
        }

        private void DelayedCall2Func(object state = null)
        {
            mSystemThreadingTimer.Change(-1, -1);
            Console.WriteLine(DateTime.Now.ToString() + ":  DelayedCall1Func 22222222");
        }

        public void DelayedCall3(long millisecondsTimeout)
        {
            mSystemTimersTimer.Interval = millisecondsTimeout;
            mSystemTimersTimer.AutoReset = false;
            mSystemTimersTimer.Start();
        }

        private void DelayedCall3Func(object sender, ElapsedEventArgs e)
        {
            mSystemTimersTimer.Stop();
            Console.WriteLine(DateTime.Now.ToString() + ":  DelayedCall1Func 333333333");
        }

        public async void DelayedCall4(long millisecondsTimeout)
        {
            mDelayedCall4CancellationTokenSource = new CancellationTokenSource();
            CancellationToken ct = mDelayedCall4CancellationTokenSource.Token;
            await Task.Run(async () =>
            {
                await Task.Delay((int)millisecondsTimeout, ct);
                if (!ct.IsCancellationRequested)
                {
                    DelayedCall4Func();
                }
            }, ct);
        }

        private void DelayedCall4Func()
        {
            Console.WriteLine(DateTime.Now.ToString() + ":  DelayedCall1Func 444444444");
        }

        public void CancelTask()
        {
            if (mDelayedCall4CancellationTokenSource != null)
            {
                mDelayedCall4CancellationTokenSource.Cancel();
            }

            if (mSystemThreadingTimer != null)
            {
                mSystemThreadingTimer.Change(-1, -1);
            }

            if (mSystemTimersTimer != null)
            {
                mSystemTimersTimer.Stop();
            }
        }
    }
}
