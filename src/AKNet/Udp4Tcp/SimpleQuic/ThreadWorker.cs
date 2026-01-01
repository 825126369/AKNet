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
        private readonly static LinkedList<LogicWorker> mLogicWorkerList = new LinkedList<LogicWorker>();
        private readonly static LinkedList<LogicWorker> mAddLogicWorkerList = new LinkedList<LogicWorker>();
        private readonly static LinkedList<LogicWorker> mRemoveLogicWorkerList = new LinkedList<LogicWorker>();

        private readonly ManualResetEventSlim mWaitHandle = new ManualResetEventSlim(true);

        public readonly ObjectPool<OP> mOPPool = new ObjectPool<OP>();
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

        public void ThreadFunc()
        {
            ThreadID = Thread.CurrentThread.ManagedThreadId;
            IsActive = true;
            while (IsActive)
            {
                mWaitHandle.Wait();

                ++NoWorkCount;
                TimeNow = SimpleQuicFunc.CxPlatTimeUs();

                foreach (var v in mLogicWorkerList)
                {
                    v.ThreadUpdate();
                }

                lock (mRemoveLogicWorkerList)
                {
                    foreach (var v in mRemoveLogicWorkerList)
                    {
                        mLogicWorkerList.Remove(v);
                    }
                    mRemoveLogicWorkerList.Clear();
                }

                lock (mAddLogicWorkerList)
                {
                    foreach (var v in mAddLogicWorkerList)
                    {
                        mLogicWorkerList.AddLast(v);
                    }
                    mAddLogicWorkerList.Clear();
                }

                while (mSocketAsyncEventArgsQueue.TryDequeue(out SSocketAsyncEventArgs arg))
                {
                    arg.Do();
                }

                if(mSocketAsyncEventArgsQueue.IsEmpty && 
                    mAddLogicWorkerList.Count == 0 && 
                    mRemoveLogicWorkerList.Count == 0)
                {
                    NoWorkCount++;
                }
                else
                {
                    NoWorkCount = 0;
                }

                if (NoWorkCount >= 2)
                {
                    mWaitHandle.Reset();
                }
            }
        }

        public void AddLogicWorker(LogicWorker mWorker)
        {
            lock (mAddLogicWorkerList)
            {
                mAddLogicWorkerList.AddLast(mWorker);
            }
            mWaitHandle.Set();
        }

        public void RemoveLogicWorker(LogicWorker mWorker)
        {
            lock (mRemoveLogicWorkerList)
            {
                mRemoveLogicWorkerList.AddLast(mWorker);
            }
            mWaitHandle.Set();
        }

        public void Add_SocketAsyncEventArgs(SSocketAsyncEventArgs arg)
        {
            mSocketAsyncEventArgsQueue.Enqueue(arg);
            mWaitHandle.Set();
        }

    }
}









