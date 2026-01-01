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
using System.Threading;

namespace AKNet.Udp4Tcp.Common
{
    internal class LogicWorker
    {
        private readonly LinkedList<Connection> mConnectionList = new LinkedList<Connection>();
        private readonly LinkedList<OP> mOPList = new LinkedList<OP>();

        public EventWaitHandle Ready = new AutoResetEvent(false);
        public EventWaitHandle Done = new AutoResetEvent(false);
        public readonly LinkedListNode<LogicWorker> mEntry;

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
            //WorkerLoop();
            foreach (var v in mConnectionList)
            {
                v.ThreadUpdate();
            }
        }

        public void AddConnection(Connection peer)
        {
            peer.mLogicWorker = this;
            mConnectionList.AddLast(peer);
        }

        public void RemoveConnection(Connection peer)
        {
            peer.mLogicWorker = null;
            mConnectionList.Remove(peer.mEntry);
        }

        private bool WorkerLoop()
        {
            //if (TimerWheel.NextExpirationTime != long.MaxValue && Worker.TimerWheel.NextExpirationTime <= State.TimeNow)
            //{
            //    QuicWorkerProcessTimers(Worker, State.ThreadID, State.TimeNow);
            //    State.NoWorkCount = 0;
            //}

            //这里是处理连接内操作
            Connection Connection = GetNextWorkConnection();
            if (Connection != null)
            {
                //在这里 处理命令
                ProcessConnection(Connection, mThreadWorker.TimeNow);
                mThreadWorker.NoWorkCount = 0;
            }

            //这里是 无状态/内部操作
            //OP Operation = GetNextOP();
            //if (Operation != null)
            //{
            //    QuicBindingProcessStatelessOperation(Operation.Type, Operation.STATELESS.Context);
            //    mThreadWorker.mOPPool.recycle(Operation);
            //    mThreadWorker.NoWorkCount = 0;
            //}

            return true;
        }

        private Connection GetNextWorkConnection()
        {
            Connection Connection = null;
            if (mConnectionList.Count > 0)
            {
                lock (mConnectionList)
                {
                    if (mConnectionList.Count > 0)
                    {
                        Connection = mConnectionList.First.Value;
                        mConnectionList.RemoveFirst();
                        NetLog.Assert(Connection.HasQueuedWork);
                        Connection.HasQueuedWork = false;
                    }
                }
            }
            return Connection;
        }

        private OP GetNextOP()
        {
            OP Operation = null;
            if (mOPList.Count > 0)
            {
                lock (mOPList)
                {
                    Operation = mOPList.First.Value;
                    mOPList.RemoveFirst();
                }
            }
            return Operation;
        }

        void ProcessConnection(Connection Connection, long TimeNow)
        {
            OP Oper;
            bool FreeOper = false;
            while (Connection.mOPList.Count > 0)
            {
                Oper = Connection.mOPList.First.Value;
                Connection.mOPList.RemoveFirst();
                FreeOper = true;
                switch (Oper.Type)
                {
                    //case E_OP_TYPE.QUIC_OPER_TYPE_API_CALL:
                    //    NetLog.Assert(Oper.API_CALL.Context != null);
                    //    QuicConnProcessApiOperation(Connection, Oper.API_CALL.Context);
                    //    break;

                    case E_OP_TYPE.FLUSH_RECV:
                        //if (Connection.State.ShutdownComplete)
                        //{
                        //    break;
                        //}

                        //if (!QuicConnFlushRecv(Connection))
                        //{
                        //    FreeOper = false;
                        //    QuicOperationEnqueue(Connection.OperQ, Connection.Partition, Oper);
                        //}
                        break;

                    case E_OP_TYPE.FLUSH_SEND:
                        //if (Connection.State.ShutdownComplete)
                        //{
                        //    break;
                        //}
                        //if (QuicSendFlush(Connection.Send))
                        //{
                        //    Connection.Send.FlushOperationPending = false;
                        //}
                        //else
                        //{
                        //    FreeOper = false;
                        //    QuicOperationEnqueue(Connection.OperQ, Connection.Partition, Oper);
                        //}
                        break;

                    case E_OP_TYPE.TIMER_EXPIRED:
                        //if (Connection.State.ShutdownComplete)
                        //{
                        //    break;
                        //}
                        //QuicConnProcessExpiredTimer(Connection, Oper.TIMER_EXPIRED.Type);
                        break;

                    default:
                        NetLog.Assert(false);
                        break;
                }

                if (FreeOper)
                {
                    mThreadWorker.mOPPool.recycle(Oper);
                }
            }
        }
    }
}









