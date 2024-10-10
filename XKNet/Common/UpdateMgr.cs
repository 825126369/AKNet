using System;
using System.Diagnostics;
using System.Threading;

namespace XKNet.Common
{
#if DEBUG
    public static class UpdateMgr
#else
    internal static class UpdateMgr
#endif
    {
        public static void Do(Action<double> updateFunc)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                long fBeginTime = stopwatch.ElapsedMilliseconds;
                long fFinishTime = stopwatch.ElapsedMilliseconds;
                double fElapsed = 0.0;
                while (true)
                {
                    fBeginTime = stopwatch.ElapsedMilliseconds;
                    updateFunc(fElapsed);
                    Thread.Sleep(10);
                    fFinishTime = stopwatch.ElapsedMilliseconds;
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
