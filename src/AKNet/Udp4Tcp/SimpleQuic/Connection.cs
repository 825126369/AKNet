using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Connection : IDisposable, IPoolItemInterface
    {
        SocketMgr.Config mConfig;
        readonly LogicWorker[] mLogicWorkerList = new LogicWorker[1];
        private bool bInit = false;
        private SocketMgr mSocketMgr = new SocketMgr();

        private int nCurrentCheckPackageCount = 0;
        public IPEndPoint RemoteEndPoint;

        private readonly AkCircularManyBuffer mMTSendStreamList = new AkCircularManyBuffer();
        private readonly AkCircularManyBuffer mMTReceiveStreamList = new AkCircularManyBuffer();

        private readonly AkCircularManySpanBuffer mSendUDPPackageList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize, 1);
        private readonly AkCircularManySpanBuffer mReceiveUdpPackageList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize, 1);
        protected bool m_Connected;
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

        public ThreadWorker mThreadWorker = null;
        public LogicWorker mLogicWorker = null;
        public SocketItem mSocketItem = null;
        private ConnectionPeerType mConnectionPeerType;

        readonly List<NetUdpReceiveFixedSizePackage> mCacheReceivePackageList = new List<NetUdpReceiveFixedSizePackage>(nDefaultCacheReceivePackageCount);
        long nLastSendSurePackageTime = 0;
        long nSameOrderIdSureCount = 0;

        UdpClientPeerCommonBase mClientPeer;

        protected readonly WeakReference<ConnectionEventArgs> mWRConnectEventArgs = new WeakReference<ConnectionEventArgs>(null);
        protected readonly WeakReference<ConnectionEventArgs> mWRDisConnectEventArgs = new WeakReference<ConnectionEventArgs>(null);
        protected readonly WeakReference<ConnectionEventArgs> mWRReceiveEventArgs = new WeakReference<ConnectionEventArgs>(null);


        public Connection()
        {
            ThreadWorkerMgr.Init();
            for (int i = 0; i < mLogicWorkerList.Length; i++)
            {
                int nThreadWorkerIndex = RandomTool.RandomArrayIndex(0, Environment.ProcessorCount);
                mLogicWorkerList[i] = new LogicWorker(nThreadWorkerIndex);
            }

            this.nSearchCount = nMinSearchCount;
            this.nMaxSearchCount = this.nSearchCount * 2;
            this.nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            this.nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
            InitRTO();
        }

        public void Dispose()
        {
            
        }
        
        public bool ConnectAsync(ConnectionEventArgs arg)
        {
            bool bIOPending = true;
            if (m_Connected)
            {
                bIOPending = false;
                arg.LastOperation = ConnectionAsyncOperation.Connect;
                arg.ConnectionError = ConnectionError.Success;
            }
            else
            {
                SocketMgr.Config mConfig = new SocketMgr.Config();
                mConfig.bServer = false;
                mConfig.mEndPoint = arg.RemoteEndPoint;
                mConfig.mReceiveFunc = WorkerThreadReceiveNetPackage;
                this.mConfig = mConfig;

                if (SimpleQuicFunc.SUCCEEDED(mSocketMgr.InitNet(mConfig)))
                {
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
    }
}
