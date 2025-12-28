/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;

namespace AKNet.Udp4Tcp.Common
{
    internal static class ThreadWorkerMgr
    {
        private static readonly ThreadWorker[] mThreadWorkerList = new ThreadWorker[Environment.ProcessorCount];
        private static bool bInit = false;

        public static void Init()
        {
            if (bInit) return;
            bInit = true;
            
            for (int i = 0; i < mThreadWorkerList.Length; i++)
            {
                mThreadWorkerList[i] = new ThreadWorker();
                mThreadWorkerList[i].Init();
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
    }
}









