using System.Diagnostics;

namespace githubExample
{
    internal class Program
    {
        static NetServerHandler mServer;
        static NetClientHandler mClient;
        static void Main(string[] args)
        {
            mServer = new NetServerHandler();
            mServer.Init();
            mClient = new NetClientHandler();
            mClient.Init();
            UpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            mServer.Update(fElapsed);
            mClient.Update(fElapsed);
        }
    }

    public static class UpdateMgr
    {
        private static readonly Stopwatch mStopWatch = Stopwatch.StartNew();
        private static double fElapsed = 0;

        public static void Do(Action<double> updateFunc, int nTargetFPS = 30)
        {
            int nFrameTime = (int)Math.Ceiling(1000.0 / nTargetFPS);

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
    }
}
