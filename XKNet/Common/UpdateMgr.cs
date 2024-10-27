using System;
using System.Diagnostics;
using System.Threading;

namespace XKNet.Common
{
    public static class UpdateMgr
    {
        private static readonly Stopwatch mStopWatch = Stopwatch.StartNew();
        private static double fElapsed = 0;

        public static double deltaTime
        {
            get { return fElapsed; }
        }

        public static double realtimeSinceStartup
        {
            get { return mStopWatch.ElapsedMilliseconds / 1000.0; }
        }

        public static void Do(Action<double> updateFunc, int nTargetFPS = 30)
        {
            int nFrameTime = (int)Math.Ceiling(1000.0 / nTargetFPS);
            try
            {
                long fBeginTime = mStopWatch.ElapsedMilliseconds;
                long fFinishTime = mStopWatch.ElapsedMilliseconds;
                fElapsed = 0.0;
                while (true)
                {
                    fBeginTime = mStopWatch.ElapsedMilliseconds;
                    updateFunc(fElapsed);

                    int fElapsed2 = (int)(mStopWatch.ElapsedMilliseconds - fBeginTime);
                    int nSleepTime = Math.Max(0, nFrameTime - fElapsed2);
                    Thread.Sleep(nSleepTime);

                    fFinishTime = mStopWatch.ElapsedMilliseconds;
                    fElapsed = (fFinishTime - fBeginTime) / 1000.0;
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.Message + ", " + e.StackTrace);
            }
        }
    }
}
