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

        private readonly AkCircularManyBuffer mMTSendStreamList = new AkCircularManyBuffer();
        private readonly AkCircularManyBuffer mMTReceiveStreamList = new AkCircularManyBuffer();

        private readonly AkCircularManySpanBuffer mSendUDPPackageList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize, 1);
        private readonly AkCircularManySpanBuffer mReceiveUdpPackageList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize, 1);
        private bool m_Connected;
        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;

        private const int nDefaultSendPackageCount = 1024;
        private const int nDefaultCacheReceivePackageCount = 2048;
        private uint nCurrentWaitReceiveOrderId;
        private readonly TcpSlidingWindow mTcpSlidingWindow = new TcpSlidingWindow();
        private readonly Queue<NetUdpSendFixedSizePackage> mWaitCheckSendQueue = new Queue<NetUdpSendFixedSizePackage>();
        private uint nCurrentWaitSendOrderId;
        private long nLastRequestOrderIdTime = 0;
        private uint nLastRequestOrderId = 0;
        private int nContinueSameRequestOrderIdCount = 0;
        private double nLastFrameTime = 0;
        private int nSearchCount = 0;
        private const int nMinSearchCount = 10;
        private int nMaxSearchCount = int.MaxValue;
        private int nRemainNeedSureCount = 0;

        public LogicWorker mLogicWorker = null;
        private ConnectionType mConnectionType;

        readonly List<NetUdpReceiveFixedSizePackage> mCacheReceivePackageList = new List<NetUdpReceiveFixedSizePackage>(nDefaultCacheReceivePackageCount);
        long nLastSendSurePackageTime = 0;
        long nSameOrderIdSureCount = 0;

        UdpClientPeerCommonBase mClientPeer;

        private readonly WeakReference<ConnectionEventArgs> mWRConnectEventArgs = new WeakReference<ConnectionEventArgs>(null);
        private readonly WeakReference<ConnectionEventArgs> mWRDisConnectEventArgs = new WeakReference<ConnectionEventArgs>(null);
        private readonly WeakReference<ConnectionEventArgs> mWRReceiveEventArgs = new WeakReference<ConnectionEventArgs>(null);
        private bool bInit = false;
        public bool HasQueuedWork;
        public LinkedList<OP> mOPList;

        public Connection()
        {
            mEntry = new LinkedListNode<Connection>(this);
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
            }

            this.nSearchCount = nMinSearchCount;
            this.nMaxSearchCount = this.nSearchCount * 2;
            this.nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            this.nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
            InitRTO();
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
                    SendConnect();
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
                SendDisConnect();
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
            arg.LastOperation = ConnectionAsyncOperation.Send;
            arg.ConnectionError = ConnectionError.Success;
            SendTcpStream(arg.GetCanReadSpan());
            return true;
        }

        public void Send(ReadOnlySpan<byte> buffer)
        {
            SendTcpStream(buffer);
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
