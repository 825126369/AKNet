/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:34
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AKNet.Common
{
    public static class ProfilerTool2

    {
        private static long StartTime = -1;
        private static long ItemStartTime = -1;
        private static long mSumItemSpendTime = 0;
        private static bool bStart = false;

        public static void TestStart()
        {
            StartTime = ProfilerTool.GetNowTime();
            mSumItemSpendTime = 0;
            ItemStartTime = -1;
        }

        public static void ItemTestStart()
        {
            if (StartTime >= 0)
            {
                ItemStartTime = ProfilerTool.GetNowTime();
            }
        }

        public static void ItemTestFinish()
        {
            if (ItemStartTime >= 0)
            {
                mSumItemSpendTime += (ProfilerTool.GetNowTime() - ItemStartTime);
                ItemStartTime = -1;
            }
        }

        public static void TestFinishAndLog(string TAG, long fMinTimeLog = -1)
        {
            if (StartTime >= 0)
            {
                if (mSumItemSpendTime > fMinTimeLog)
                {
                    long nSumTime = ProfilerTool.GetNowTime() - StartTime;
                    int fPercent = (int)Math.Floor(mSumItemSpendTime / (double)nSumTime * 100);
                    NetLog.Log($"ProfilerTool2 =====[{TAG}]: {mSumItemSpendTime / 1000.0} / {nSumTime / 1000.0} = {fPercent}%");
                }
                StartTime = -1;
            }
        }
    }
        
    public static class ProfilerTool

    {
        private static readonly Stack<long> TestStack = new Stack<long>();
        private static readonly Stopwatch mStopWatch = Stopwatch.StartNew();

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
