/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Diagnostics;

namespace TestCommon
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

        public static void Do2(Action<double> updateFunc)
        {
            long fBeginTime = mStopWatch.ElapsedMilliseconds;
            long fFinishTime = mStopWatch.ElapsedMilliseconds;
            fElapsed = 0.0;
            while (true)
            {
                fBeginTime = mStopWatch.ElapsedMilliseconds;
                updateFunc(fElapsed);
                Thread.Sleep(1);
                fFinishTime = mStopWatch.ElapsedMilliseconds;
                fElapsed = (fFinishTime - fBeginTime) / 1000.0;
            }
        }

    }
}
