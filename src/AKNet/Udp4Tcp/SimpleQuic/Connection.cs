using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Connection : IDisposable, IPoolItemInterface
    {
        public readonly LinkedListNode<Connection> mEntry;
        private SocketMgr.Config mConfig;
        private SocketMgr mSocketMgr = new SocketMgr();

        private int nCurrentCheckPackageCount = 0;
        public IPEndPoint RemoteEndPoint;

        private readonly AkCircularManyBuffer mMTReceiveStreamList = new AkCircularManyBuffer();
        private bool m_Connected;
        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;
        
        private readonly WeakReference<ConnectionEventArgs> mWRConnectEventArgs = new WeakReference<ConnectionEventArgs>(null);
        private readonly WeakReference<ConnectionEventArgs> mWRDisConnectEventArgs = new WeakReference<ConnectionEventArgs>(null);
        private readonly WeakReference<ConnectionEventArgs> mWRReceiveEventArgs = new WeakReference<ConnectionEventArgs>(null);
        private readonly Queue<ConnectionEventArgs> mWRSendEventArgsQueue = new Queue<ConnectionEventArgs>();
        private bool bInit = false;
        public bool HasQueuedWork;
        public readonly LinkedList<ConnectionOP> mOPList = new LinkedList<ConnectionOP>();

        private UdpCheckMgr mUdpCheckMgr;
        public LogicWorker mLogicWorker;
        private ConnectionType mConnectionType;

        public Connection()
        {
            mEntry = new LinkedListNode<Connection>(this);
            mUdpCheckMgr = new UdpCheckMgr(this);
        }

        public void Init(ConnectionType nType)
        {
            if (bInit) return;
            bInit = true;

            this.mConnectionType = nType;
            if (this.mConnectionType == ConnectionType.Client)
            {
                ThreadWorkerMgr.Init();
                mLogicWorker = new LogicWorker();
                mLogicWorker.Init(ThreadWorkerMgr.GetRandomThreadWorker());
                mLogicWorker.AddConnection(this);
            }

            mUdpCheckMgr.Reset();
        }

        private void OnConnectInit()
        {
            Init(ConnectionType.Client);
        }

        public void Dispose()
        {
            
        }
        
        public bool ConnectAsync(ConnectionEventArgs arg)
        {
            OnConnectInit();

            bool bIOPending = true;
            if (m_Connected)
            {
                bIOPending = false;
                arg.LastOperation = ConnectionAsyncOperation.Connect;
                arg.ConnectionError = ConnectionError.Success;
            }
            else
            {
                RemoteEndPoint = arg.RemoteEndPoint;
                SocketMgr.Config mConfig = new SocketMgr.Config();
                mConfig.bServer = false;
                mConfig.mEndPoint = arg.RemoteEndPoint;
                mConfig.mReceiveFunc = WorkerThreadReceiveNetPackage;
                this.mConfig = mConfig;

                if (SimpleQuicFunc.SUCCESSED(mSocketMgr.InitNet(mConfig)))
                {
                    mLogicWorker.SetSocketItem(mSocketMgr.GetSocketItem(0));
                    mWRConnectEventArgs.SetTarget(arg);

                    lock (mOPList)
                    {
                        mOPList.AddLast(new ConnectionOP() { nOPType =  ConnectionOP.E_OP_TYPE.SendConnect });
                    }
                }
                else
                {
                    arg.LastOperation = ConnectionAsyncOperation.Connect;
                    arg.ConnectionError = ConnectionError.Error;
                    bIOPending = false;
                }
            }

            return bIOPending;
        }

        public bool DisconnectAsync(ConnectionEventArgs arg)
        {
            bool bIOPending = true;
            if (m_Connected)
            {
                mWRDisConnectEventArgs.SetTarget(arg);
                lock (mOPList)
                {
                    mOPList.AddLast(new ConnectionOP() { nOPType = ConnectionOP.E_OP_TYPE.SendDisConnect });
                }
            }
            else
            {
                arg.LastOperation = ConnectionAsyncOperation.Disconnect;
                arg.ConnectionError = ConnectionError.Success;
                bIOPending = false;
            }
            return bIOPending;
        }

        public bool SendAsync(ConnectionEventArgs arg)
        {
            SendTcpStream(arg);
            return true;
        }

        public bool ReceiveAsync(ConnectionEventArgs arg)
        {
            bool bIOPending = true;
            arg.LastOperation = ConnectionAsyncOperation.Receive;
            arg.ConnectionError = ConnectionError.Success;
            lock (mMTReceiveStreamList)
            {
                if (mMTReceiveStreamList.Length > 0)
                {
                    bIOPending = false;
                    arg.Offset = 0;
                    arg.Length = arg.MemoryBuffer.Length;
                    arg.BytesTransferred = mMTReceiveStreamList.WriteTo(arg.GetCanWriteSpan());
                }
                else
                {
                    mWRReceiveEventArgs.SetTarget(arg);
                }
            }

            return bIOPending;
        }

        public bool Connected
        {
            get
            {
                return m_Connected;
            }
        }

        //static void QuicConnQueueOper(QUIC_CONNECTION Connection, QUIC_OPERATION Oper)
        //{
        //    if(QuicOperationEnqueue(Connection.OperQ, Connection.Partition, Oper))
        //    {
        //        QuicWorkerQueueConnection(Connection.Worker, Connection);
        //    }
        //}
        
    }
}
