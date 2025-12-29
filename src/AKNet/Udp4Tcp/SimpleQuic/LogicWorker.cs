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
        private readonly int nIndex;
        private ThreadWorker mThreadWorker;

        public LogicWorker(int nIndex)
        {
            this.nIndex = nIndex;
            this.mEntry = new LinkedListNode<LogicWorker>(this);
            this.mThreadWorker = ThreadWorkerMgr.GetThreadWorker(nIndex);
            this.mThreadWorker.AddLogicWorker(this);
        }

        public void Init()
        {

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

        public void Add_SocketAsyncEventArgs(SSocketAsyncEventArgs arg)
        {
            mSocketAsyncEventArgsQueue.Enqueue(arg);
        }
    }
}









