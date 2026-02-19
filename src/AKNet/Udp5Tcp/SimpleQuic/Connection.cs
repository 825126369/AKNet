/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace AKNet.Udp5Tcp.Common
{
    internal partial class Connection : IDisposable, IPoolItemInterface
    {
        private readonly LinkedListNode<Connection> mEntry;
        private SocketMgr.Config mConfig;
        private SocketMgr mSocketMgr = null;

        private int nCurrentCheckPackageCount = 0;
        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;

        private readonly AkCircularManyBuffer mMTReceiveStreamList = new AkCircularManyBuffer();
        private readonly LinkedList<NetStreamSendPackage> mMTSendStreamList = new LinkedList<NetStreamSendPackage>();

        private readonly Queue<NetUdpReceiveFixedSizePackage> mReceiveWaitCheckPackageQueue = new Queue<NetUdpReceiveFixedSizePackage>();

        private bool bInit = false;
        private bool m_Connected;
        private bool m_Disposed;
        private bool m_OnDestroyDontReceiveData;
        public readonly LinkedList<ConnectionOP> mOPList = new LinkedList<ConnectionOP>();

        private readonly UdpCheckMgr mUdpCheckMgr;
        public LogicWorker mLogicWorker;
        private E_CONNECTION_TYPE mConnectionType;

        public LinkedListNode<Connection> GetEntry()
        {
            return mEntry;
        }

        public Connection()
        {
            mEntry = new LinkedListNode<Connection>(this);
            mUdpCheckMgr = new UdpCheckMgr(this);

            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
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
                var mLogicWorker = new LogicWorker();
                mLogicWorker.Init(ThreadWorkerMgr.GetRandomThreadWorker());
                mLogicWorker.AddConnection(this);
            }
        }

        public void SetLogicWorker(LogicWorker mLogicWorker)
        {
            this.mLogicWorker = mLogicWorker; 
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
            mMTReceiveStreamList.Reset();
            mMTSendStreamList.Clear();

            lock (mReceiveWaitCheckPackageQueue)
            {
                while (mReceiveWaitCheckPackageQueue.TryDequeue(out var v))
                {
                    mLogicWorker.UdpReceivePackage_Recycle(v);
                }
            }

            mOPList.Clear();
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
