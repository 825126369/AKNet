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
        public readonly static LinkedList<Connection> mConnectionPeerList = new LinkedList<Connection>();
        public readonly static LinkedList<Listener> mListenerList = new LinkedList<Listener>();
        private readonly static List<ThreadWorker> mThreadWorkerList = new List<ThreadWorker>();
        private static bool bInit = false;

        public static void Init(bool bServer)
        {
            if (bInit) return;
            bInit = true;
            
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                var mWorker = new ThreadWorker();
                mThreadWorkerList.Add(mWorker);
                mWorker.Init();
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









