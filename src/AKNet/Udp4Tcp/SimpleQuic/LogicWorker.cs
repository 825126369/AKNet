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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class LogicWorker
    {
        private ConcurrentQueue<SSocketAsyncEventArgs> mSocketAsyncEventArgsQueue = new ConcurrentQueue<SSocketAsyncEventArgs>();
        private readonly static LinkedList<ConnectionPeer> mConnectionList = new LinkedList<ConnectionPeer>();
        private AutoResetEvent mEventQReady = new AutoResetEvent(false);
        private readonly LinkedListNode<LogicWorker> mEntry;
        private readonly int nThreadIndex;
        private ThreadWorker mThreadWorker;
        private SocketItem mSocketItem;

        public LogicWorker(int nThreadIndex)
        {
            this.nThreadIndex = nThreadIndex;
            this.mEntry = new LinkedListNode<LogicWorker>(this);
            this.mThreadWorker = ThreadWorkerMgr.GetThreadWorker(nThreadIndex);
            this.mThreadWorker.AddLogicWorker(this);
        }

        public void SetSocketItem(SocketItem mSocketItem)
        {
            this.mSocketItem = mSocketItem;
        }

        public void ThreadUpdate()
        {
            mEventQReady.WaitOne();
            foreach (var v in mConnectionList)
            {
                v.ThreadUpdate();
            }

            while (mSocketAsyncEventArgsQueue.TryDequeue(out SSocketAsyncEventArgs arg))
            {
                arg.Do();
            }
        }

        public void AddConnectionPeer(ConnectionPeer peer)
        {
            peer.mLogicWorker = this;
            peer.mThreadWorker = mThreadWorker;
            peer.mSocketItem = mSocketItem;
            mConnectionList.AddLast(peer);
        }

        public void Add_SocketAsyncEventArgs(SSocketAsyncEventArgs arg)
        {
            mSocketAsyncEventArgsQueue.Enqueue(arg);
        }
    }
}









