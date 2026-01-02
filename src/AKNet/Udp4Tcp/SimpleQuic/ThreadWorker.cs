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
        private readonly Queue<ThreadWorkerOP> mOPList = new Queue<ThreadWorkerOP>();

        public readonly ObjectPool<Connection> mConnectionPeerPool = new ObjectPool<Connection>();
        public readonly ObjectPool<NetUdpSendFixedSizePackage> mSendPackagePool = new ObjectPool<NetUdpSendFixedSizePackage>();
        public readonly ObjectPool<NetUdpReceiveFixedSizePackage> mReceivePackagePool = new ObjectPool<NetUdpReceiveFixedSizePackage>();
        private readonly ConcurrentQueue<SSocketAsyncEventArgs> mSocketAsyncEventArgsQueue = new ConcurrentQueue<SSocketAsyncEventArgs>();

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

                foreach (var v in mLogicWorkerList)
                {
                    v.ThreadUpdate();
                }

                lock (mOPList)
                {
                    if (mOPList.Count > 0)
                    {
                        while(mOPList.TryDequeue(out var mOP))
                        {
                            if (mOP.nOPType == ThreadWorkerOP.E_OP_TYPE.AddLogicWorker)
                            {
                                mLogicWorkerList.AddLast(mOP.mTarget.GetEntry());
                            }
                            else
                            {
                                mLogicWorkerList.Remove(mOP.mTarget.GetEntry());
                            }
                        }
                    }
                }

                while (mSocketAsyncEventArgsQueue.TryDequeue(out SSocketAsyncEventArgs arg))
                {
                    arg.Do();
                }

                Thread.Sleep(1);
            }
        }

        public void AddLogicWorker(LogicWorker mWorker)
        {
            lock (mOPList)
            {
                var mOP = new ThreadWorkerOP();
                mOP.nOPType = ThreadWorkerOP.E_OP_TYPE.AddLogicWorker;
                mOP.mTarget = mWorker;
                mOPList.Enqueue(mOP);
            }
        }

        public void RemoveLogicWorker(LogicWorker mWorker)
        {
            lock (mOPList)
            {
                var mOP = new ThreadWorkerOP();
                mOP.nOPType = ThreadWorkerOP.E_OP_TYPE.RemoveLogicWorker;
                mOP.mTarget = mWorker;
                mOPList.Enqueue(mOP);
            }
        }

        public void Add_SocketAsyncEventArgs(SSocketAsyncEventArgs arg)
        {
            mSocketAsyncEventArgsQueue.Enqueue(arg);
        }

    }
}









