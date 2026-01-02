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
using System.Collections.Generic;

namespace AKNet.Udp4Tcp.Common
{
    internal class LogicWorker
    {
        private readonly LinkedList<Connection> mConnectionList = new LinkedList<Connection>();
        private readonly LinkedListNode<LogicWorker> mEntry;

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

        public void Init(int nThreadIndex)
        {
            this.mThreadWorker = ThreadWorkerMgr.GetThreadWorker(nThreadIndex);
            this.mThreadWorker.AddLogicWorker(this);
            this.mThreadWorker.Init();
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
        }

        public void ThreadUpdate()
        {
            foreach (var v in mConnectionList)
            {
                v.ThreadUpdate();
            }
        }

        public void AddConnection(Connection peer)
        {
#if DEBUG
            NetLog.Assert(!mConnectionList.Contains(peer));
#endif
            peer.mLogicWorker = this;
            mConnectionList.AddLast(peer);
        }

        public void RemoveConnection(Connection peer)
        {
#if DEBUG
            NetLog.Assert(mConnectionList.Contains(peer));
#endif
            peer.mLogicWorker = null;
            mConnectionList.Remove(peer.mEntry);
        }
    }
}









