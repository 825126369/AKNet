#if DEBUG

using System.Collections.Generic;
using System.Diagnostics;

namespace XKNet.Common
{
    public static class ProfilerTool2

    {
        private static long StartTime = -1;
        private static long mSumSpendTime = 0;
        private static bool bStart = false;

        public static void TestStart()
        {
            mSumSpendTime = 0;
            StartTime = -1;
        }

        public static void ItemTestStart()
        {
            StartTime = ProfilerTool.GetNowTime();
        }

        public static void ItemTestFinish()
        {
            if (CheckStackOk())
            {
                var FinishTime = ProfilerTool.GetNowTime();
                var mSpend = FinishTime - StartTime;
                mSumSpendTime += mSpend;

                StartTime = -1;
            }
        }

        public static void TestFinishAndLog(string TAG)
        {
            NetLog.Log($"ProfilerTool2 =====[{TAG}]: {mSumSpendTime / 1000.0}");
            mSumSpendTime = 0;
        }

        private static bool CheckStackOk()
        {
            return StartTime >= 0;
        }
    }
        
    public static class ProfilerTool

    {
        private static readonly Stack<long> TestStack = new Stack<long>();
        private static readonly Stopwatch mStopWatch = new Stopwatch();

        static ProfilerTool()
        {
            mStopWatch.Start();
        }

        public static long GetNowTime()
        {
           return mStopWatch.ElapsedMilliseconds;
        }

        public static void TestStart()
        {
            TestStack.Push(GetNowTime());
        }

        public static double GetTestFinishSpendTime()
        {
            NetLog.Assert(CheckStackOk(), "Test 方法 要成对出现  !!!");
            var FinishTime = GetNowTime();
            var StartTime = TestStack.Pop();
            return (FinishTime - StartTime) / 1000.0;
        }

        private static bool CheckStackOk()
        {
            return TestStack.Count > 0;
        }

        public static void TestFinishAndLog(string TAG)
        {
            if (CheckStackOk())
            {
                NetLog.Log($"ProfilerTool ======[{TAG}]: " + GetTestFinishSpendTime());
            }
        }
    }

}

#endif
