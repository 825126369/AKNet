using System.Collections.Generic;
using System.Diagnostics;

namespace XKNet.Common
{
    public static class ProfilerTool
    {
        private static readonly Stack<long> TestStack = new Stack<long>();
        private static readonly Stopwatch mStopWatch = new Stopwatch();

        static ProfilerTool()
        {
            mStopWatch.Start();
        }

        public static void TestStart()
        {
            TestStack.Push(mStopWatch.ElapsedMilliseconds);
        }

        public static double GetTestFinishSpendTime()
        {
            var FinishTime = mStopWatch.ElapsedMilliseconds;
            NetLog.Assert(TestStack.Count > 0, "Test 方法 要成对出现  !!!");
            var StartTime = TestStack.Pop();
            return (FinishTime - StartTime) / 1000.0;
        }

        public static void ClearTestStack()
        {
            TestStack.Clear();
        }

        public static void TestFinishAndLog(string TAG)
        {
            NetLog.Log($"GameProfiler [{TAG}]: " + GetTestFinishSpendTime());
        }
    }

}
