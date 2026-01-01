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
        private readonly static LinkedList<Connection> mConnectionList = new LinkedList<Connection>();
        public EventWaitHandle Ready = new AutoResetEvent(false);
        public EventWaitHandle Done = new AutoResetEvent(false);
        private readonly LinkedListNode<LogicWorker> mEntry;

        public ThreadWorker mThreadWorker;
        public SocketItem mSocketItem;

        public LogicWorker()
        {
            this.mEntry = new LinkedListNode<LogicWorker>(this);
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

        public void AddConnectionPeer(Connection peer)
        {
            peer.mLogicWorker = this;
            mConnectionList.AddLast(peer);
        }
    }
}









