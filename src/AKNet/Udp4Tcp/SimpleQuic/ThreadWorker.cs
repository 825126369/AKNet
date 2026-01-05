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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace AKNet.Udp4Tcp.Common
{
    internal class ThreadWorker:IDisposable
    {
        //每个线程，被多个逻辑Worker使用，比如创建了 N个服务器，那么线程池复用。
        private readonly LinkedList<LogicWorker> mLogicWorkerList = new LinkedList<LogicWorker>();
        private readonly Queue<LogicWorker> mAddLogicWorkerQueue = new Queue<LogicWorker>();
        private readonly Queue<LogicWorker> mRemoveLogicWorkerQueue = new Queue<LogicWorker>();

        public readonly ObjectPool<Connection> mConnectionPool = new ObjectPool<Connection>(0, byte.MaxValue);
        public readonly ObjectPool<NetUdpSendFixedSizePackage> mSendPackagePool = new ObjectPool<NetUdpSendFixedSizePackage>(0, byte.MaxValue);
        public readonly ObjectPool<NetUdpReceiveFixedSizePackage> mReceivePackagePool = new ObjectPool<NetUdpReceiveFixedSizePackage>(0, byte.MaxValue);
        
        public bool IsActive;
        public long TimeNow;
        public long LastWorkTime;
        public long LastPoolProcessTime;
        public long WaitTime;
        public int NoWorkCount;
        public int ThreadID;
        public int nThreadIndex;
        private bool bInit = false;
        //谁用到他，再启动他
        public void Init()
        {
            if(bInit) return; 
            bInit = true;

            Thread mThread = new Thread(ThreadFunc);
            mThread.IsBackground = true;
            mThread.Start();
        }

        public void Dispose()
        {
            
        }

        ~ThreadWorker() { IsActive = false; }

        public void ThreadFunc()
        {
            ThreadID = Thread.CurrentThread.ManagedThreadId;
            IsActive = true;
            while (IsActive)
            {
                TimeNow = SimpleQuicFunc.GetNowTimeMS();

                if (mAddLogicWorkerQueue.Count > 0)
                {
                    lock (mAddLogicWorkerQueue)
                    {
                        while (mAddLogicWorkerQueue.TryDequeue(out var v))
                        {
                            mLogicWorkerList.AddLast(v.GetEntry());
                        }
                    }
                }

                if (mRemoveLogicWorkerQueue.Count > 0)
                {
                    lock (mRemoveLogicWorkerQueue)
                    {
                        while (mRemoveLogicWorkerQueue.TryDequeue(out var v))
                        {
                            mLogicWorkerList.Remove(v.GetEntry());
                        }

                    }
                }
                
                foreach (var v in mLogicWorkerList)
                {
                    v.ThreadUpdate();
                }

                Thread.Sleep(1);
            }
        }

        public void AddLogicWorker(LogicWorker mWorker)
        {
            lock (mAddLogicWorkerQueue)
            {
                mAddLogicWorkerQueue.Enqueue(mWorker);
            }
        }

        public void RemoveLogicWorker(LogicWorker mWorker)
        {
            lock (mRemoveLogicWorkerQueue)
            {
                mRemoveLogicWorkerQueue.Enqueue(mWorker);
            }
        }

    }
}









