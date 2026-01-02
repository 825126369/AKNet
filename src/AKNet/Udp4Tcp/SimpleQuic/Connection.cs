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

        private readonly UdpCheckMgr mUdpCheckMgr;
        public LogicWorker mLogicWorker;
        private ConnectionType mConnectionType;
        public SSocketAsyncEventArgsPool mSendEventArgsPool;

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

            mSendEventArgsPool = new SSocketAsyncEventArgsPool(mLogicWorker, 0, 1024);
        }

        private void OnConnectReset()
        {
            SimpleQuicFunc.ThreadCheck(this);
            this.fReceiveHeartBeatTime = 0;
            this.fMySendHeartBeatCdTime = 0;
            mUdpCheckMgr.Reset();
        }

        private void OnDisConnectReset()
        {
            SimpleQuicFunc.ThreadCheck(this);
            this.fReceiveHeartBeatTime = 0;
            this.fMySendHeartBeatCdTime = 0;
            mUdpCheckMgr.Reset();
        }

        public void Reset()
        {
            SimpleQuicFunc.ThreadCheck(this);
            mUdpCheckMgr.Reset();
            mWRConnectEventArgs.SetTarget(null);
            mWRDisConnectEventArgs.SetTarget(null);
            mWRReceiveEventArgs.SetTarget(null);
            mWRSendEventArgsQueue.Clear();
            mOPList.Clear();
            mSendEventArgsPool = null;
            bInit = false;
            RemoteEndPoint = null;
            mLogicWorker = null;
        }

        public void Dispose()
        {
            mLogicWorker.RemoveConnection(this);
            if (mConnectionType == ConnectionType.Client)
            {
                mLogicWorker.mThreadWorker.RemoveLogicWorker(mLogicWorker);
                mLogicWorker.mThreadWorker = null;
            }
            Reset();
        }

        public bool Connected
        {
            get
            {
                return m_Connected;
            }
        }
    }
}
