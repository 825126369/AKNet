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
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AKNet.Udp4Tcp.Common
{
    internal class LogicWorker
    {
        private readonly LinkedList<Connection> mConnectionList = new LinkedList<Connection>();
        private readonly Queue<Connection> mAddConnectionList = new Queue<Connection>();
        private readonly Queue<Connection> mRemoveConnectionList = new Queue<Connection>();
        private readonly LinkedListNode<LogicWorker> mEntry;

        private readonly ConcurrentQueue<SSocketAsyncEventArgs> mSocketAsyncEventArgsQueue = new ConcurrentQueue<SSocketAsyncEventArgs>();

        public ThreadWorker mThreadWorker;
        public SocketItem mSocketItem;

        public bool IsExternal;
        public bool Enabled;
        public bool IsActive;
        public int PartitionIndex;
        public long AverageQueueDelay;
        public int OperationCount;
        public int DroppedOperationCount;

        public LogicWorker()
        {
            this.mEntry = new LinkedListNode<LogicWorker>(this);
        }

        public LinkedListNode<LogicWorker> GetEntry()
        {
            return mEntry;
        }

        public void Reset()
        {
            mSendEventArgsPool = null;
        }

        public void Init(ThreadWorker mThreadWorker)
        {
            this.mThreadWorker = mThreadWorker;
            this.mThreadWorker.AddLogicWorker(this);
            this.mThreadWorker.Init();
        }

        public void SetSocketItem(SocketItem mSocketItem)
        {
            this.mSocketItem = mSocketItem;
            mSocketItem.mLogicWorker = this;
            this.mSendEventArgsPool = new SSocketAsyncEventArgsPool(this, 1024);
        }

        public void ThreadUpdate()
        {
            if (mAddConnectionList.Count > 0)
            {
                lock (mAddConnectionList)
                {
                    while (mAddConnectionList.TryDequeue(out var v))
                    {
                        mConnectionList.AddLast(v.GetEntry());
                    }
                }
            }

            if (mRemoveConnectionList.Count > 0)
            {
                lock (mRemoveConnectionList)
                {
                    while (mRemoveConnectionList.TryDequeue(out var v))
                    {
                        mConnectionList.Remove(v.GetEntry());
                        if (v.ConnectionType == E_CONNECTION_TYPE.Server)
                        {
                            v.OwnerListener.RemoveFakeSocket(v);
                            v.mLogicWorker = null;
                            v.OwnerListener = null;
                        }
                    }
                }
            }

            foreach (var v in mConnectionList)
            {
                v.ThreadUpdate();
            }

            while (mSocketAsyncEventArgsQueue.TryDequeue(out SSocketAsyncEventArgs arg))
            {
                arg.Do();
            }
        }

        public void AddConnection(Connection peer)
        {
#if DEBUG
            NetLog.Assert(!mConnectionList.Contains(peer));
#endif
            peer.SetLogicWorker(this);
            lock (mAddConnectionList)
            {
                mAddConnectionList.Enqueue(peer);
            }
        }

        public void RemoveConnection(Connection peer)
        {
#if DEBUG
            NetLog.Assert(mConnectionList.Contains(peer));
#endif
            lock (mRemoveConnectionList)
            {
                mRemoveConnectionList.Enqueue(peer);
            }
        }

        public void Add_SocketAsyncEventArgs(SSocketAsyncEventArgs arg)
        {
            mSocketAsyncEventArgsQueue.Enqueue(arg);
        }
        
        private readonly SafeObjectPool<NetUdpSendFixedSizePackage> mSendPackagePool = new SafeObjectPool<NetUdpSendFixedSizePackage>(1024);
        private readonly SafeObjectPool<NetUdpReceiveFixedSizePackage> mReceivePackagePool = new SafeObjectPool<NetUdpReceiveFixedSizePackage>(1024);
        public SSocketAsyncEventArgsPool mSendEventArgsPool;
        private readonly SafeObjectPool<NetStreamSendPackage> mNetStreamSendPackagePool = new SafeObjectPool<NetStreamSendPackage>(1024);

        public NetUdpSendFixedSizePackage UdpSendPackage_Pop()
        {
            if (Config.bUseSocketAsyncEventArgsTwoComplete)
            {
                return mThreadWorker.mSendPackagePool.Pop();
            }
            else
            {
                return mSendPackagePool.Pop();
            }
        }

        public void UdpSendPackage_Recycle(NetUdpSendFixedSizePackage mPackage)
        {
            if (Config.bUseSocketAsyncEventArgsTwoComplete)
            {
                mThreadWorker.mSendPackagePool.recycle(mPackage);
            }
            else
            {
                mSendPackagePool.recycle(mPackage);
            }
        }

        public NetUdpReceiveFixedSizePackage UdpReceivePackage_Pop()
        {
            if (Config.bUseSocketAsyncEventArgsTwoComplete)
            {
                return mThreadWorker.mReceivePackagePool.Pop();
            }
            else
            {
                return mReceivePackagePool.Pop();
            }
        }

        public void UdpReceivePackage_Recycle(NetUdpReceiveFixedSizePackage mPackage)
        {
            if (Config.bUseSocketAsyncEventArgsTwoComplete)
            {
                mThreadWorker.mReceivePackagePool.recycle(mPackage);
            }
            else
            {
                mReceivePackagePool.recycle(mPackage);
            }
        }

        public NetStreamSendPackage NetStreamSendPackage_Pop()
        {
            return mNetStreamSendPackagePool.Pop();
        }

        public void NetStreamSendPackage_Recycle(NetStreamSendPackage mPackage)
        {
            mNetStreamSendPackagePool.recycle(mPackage);
        }
    }
}









