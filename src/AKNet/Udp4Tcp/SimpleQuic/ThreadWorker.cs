/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class ThreadWorker:IDisposable
    {
        //每个线程，被多个逻辑Worker使用，比如创建了 N个服务器，那么线程池复用。
        private readonly static LinkedList<LogicWorker> mLogicWorkerList = new LinkedList<LogicWorker>();
        private readonly AutoResetEvent mEventQReady = new AutoResetEvent(false);
        private readonly ObjectPool<ConnectionPeer> mConnectionPeerPool = null;
        private readonly ObjectPool<NetUdpSendFixedSizePackage> mSendPackagePool = null;
        private readonly ObjectPool<NetUdpReceiveFixedSizePackage> mReceivePackagePool = null;

        public void Init()
        {
            Thread mThread = new Thread(ThreadFunc);
            mThread.IsBackground = true;
            mThread.Start();
        }

        public void Dispose()
        {
            
        }

        public void ThreadFunc()
        {
            while (true)
            {
                mEventQReady.WaitOne();
                var mLogicWorker = mLogicWorkerList.First;
                while (mLogicWorker != null)
                {
                    mLogicWorker.Value.Update();
                    mLogicWorker = mLogicWorker.Next;
                }
            }
        }
    }
}









