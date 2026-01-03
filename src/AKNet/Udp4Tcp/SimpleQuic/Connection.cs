using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Connection : IDisposable, IPoolItemInterface
    {
        private readonly LinkedListNode<Connection> mEntry;
        private SocketMgr.Config mConfig;
        private SocketMgr mSocketMgr = null;

        private int nCurrentCheckPackageCount = 0;
        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;
        
        private readonly WeakReference<ConnectionEventArgs> mWRConnectEventArgs = new WeakReference<ConnectionEventArgs>(null);
        private readonly WeakReference<ConnectionEventArgs> mWRDisConnectEventArgs = new WeakReference<ConnectionEventArgs>(null);
        private readonly WeakReference<ConnectionEventArgs> mWRReceiveEventArgs = new WeakReference<ConnectionEventArgs>(null);

        private readonly AkCircularManyBuffer mMTReceiveStreamList = new AkCircularManyBuffer();
        private readonly Queue<ConnectionEventArgs> mWRSendEventArgsQueue = new Queue<ConnectionEventArgs>();

        private bool bInit = false;
        private bool m_Connected;
        private bool m_OnDestroyDontReceiveData;
        public readonly LinkedList<ConnectionOP> mOPList = new LinkedList<ConnectionOP>();

        private readonly UdpCheckMgr mUdpCheckMgr;
        public LogicWorker mLogicWorker;
        private E_CONNECTION_TYPE mConnectionType;
        public SSocketAsyncEventArgsPool mSendEventArgsPool;

        public LinkedListNode<Connection> GetEntry()
        {
            return mEntry;
        }

        public Connection()
        {
            mEntry = new LinkedListNode<Connection>(this);
            mUdpCheckMgr = new UdpCheckMgr(this);
        }

        public void Init(E_CONNECTION_TYPE nType)
        {
            if (bInit) return;
            bInit = true;
            m_OnDestroyDontReceiveData = false;
            this.mConnectionType = nType;
            if (this.mConnectionType == E_CONNECTION_TYPE.Client)
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
            this.mUdpCheckMgr.Reset();
        }

        private void OnDisConnectReset()
        {
            SimpleQuicFunc.ThreadCheck(this);
            this.fReceiveHeartBeatTime = 0;
            this.fMySendHeartBeatCdTime = 0;
            this.mUdpCheckMgr.Reset();
        }

        public void Reset()
        {
            SimpleQuicFunc.ThreadCheck(this);
            mUdpCheckMgr.Reset();
            mWRConnectEventArgs.SetTarget(null);
            mWRDisConnectEventArgs.SetTarget(null);
            mWRReceiveEventArgs.SetTarget(null);

            mMTReceiveStreamList.Reset();
            mWRSendEventArgsQueue.Clear();

            mOPList.Clear();
            mSendEventArgsPool = null;
            bInit = false;
            m_Connected = false;
            RemoteEndPoint = null;
            mLogicWorker = null;
        }

        public IPEndPoint RemoteEndPoint { get; set; }

        public bool Connected
        {
            get
            {
                return m_Connected;
            }
        }

        public E_CONNECTION_TYPE ConnectionType
        {
            get
            {
                return mConnectionType;
            }
        }

        public Listener OwnerListener { get; set; }
    }
}
