/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;

namespace AKNet.Udp5Tcp.Common
{
    internal static class ThreadWorkerMgr
    {
        private static readonly List<ThreadWorker> mThreadWorkerList = new List<ThreadWorker>();
        private static bool bInit = false;

        public static void Init()
        {
            if (bInit) return;
            bInit = true;
            
            int nThreadCount = Environment.ProcessorCount;
            for (int i = 0; i < nThreadCount; i++)
            {
                var mThreadWorker = new ThreadWorker();
                mThreadWorkerList.Add(mThreadWorker);
            }
        }

        public static ThreadWorker GetMainThreadWorker()
        {
            return mThreadWorkerList[0];
        }

        public static ThreadWorker GetThreadWorker(int i)
        {
            return mThreadWorkerList[i];
        }

        public static ThreadWorker GetRandomThreadWorker()
        {
            int nRandomIndex = RandomTool.RandomArrayIndex(0, mThreadWorkerList.Count);
            return mThreadWorkerList[nRandomIndex];
        }

        public static List<ThreadWorker> GetRandomThreadWorkerList(int nSocketCount)
        {
            List<ThreadWorker> mFinalList = new List<ThreadWorker>();

            List<int> mIndexList = new List<int>();
            for(int i = 0; i < mThreadWorkerList.Count; i++)
            {
                mIndexList.Add(i);
            }
            
            while (mFinalList.Count < nSocketCount)
            {
                int nRandomIndex = RandomTool.RandomArrayIndex(0, mIndexList.Count);
                ThreadWorker mThreadWorker = mThreadWorkerList[nRandomIndex];
                mIndexList.RemoveAt(nRandomIndex);
                mFinalList.Add(mThreadWorker);
            }

            NetLog.Assert(mFinalList.Count == nSocketCount);
            return mFinalList;
        }
    }
}









