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
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class ConnectionPeer : IPoolItemInterface, IDisposable
    {
        private int nCurrentCheckPackageCount = 0;
        public IPEndPoint RemoteEndPoint;

        private readonly object lock_mSocket_object = new object();
        private readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        
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

        public ConnectionPeer()
        {
            //this.mServerMgr = mNetServer;
            
            this.nSearchCount = nMinSearchCount;
            this.nMaxSearchCount = this.nSearchCount * 2;
            this.nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            this.nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
            InitRTO();
        }
        
        public void ThreadUpdate()
        {
            if (!m_Connected) return;

            UdpStatistical.AddSearchCount(this.nSearchCount);
            UdpStatistical.AddFrameCount();

            AddPackage();
            if (mWaitCheckSendQueue.Count == 0) return;

            bool bTimeOut = false;
            int nSearchCount = this.nSearchCount;
            foreach (var mPackage in mWaitCheckSendQueue)
            {
                if (mPackage.nSendCount > 0)
                {
                    if (mPackage.orTimeOut())
                    {
                        UdpStatistical.AddReSendCheckPackageCount();
                        SendUDPPackage(mPackage);
                        ArrangeReSendTimeOut(mPackage);
                        mPackage.nSendCount++;
                        bTimeOut = true;
                    }
                }
                else
                {
                    UdpStatistical.AddFirstSendCheckPackageCount();
                    SendUDPPackage(mPackage);
                    ArrangeReSendTimeOut(mPackage);
                    mPackage.mTcpStanardRTOTimer.BeginRtt();
                    mPackage.nSendCount++;
                }

                if (--nSearchCount <= 0)
                {
                    break;
                }
            }

            if (bTimeOut)
            {
                this.nSearchCount = Math.Max(this.nSearchCount / 2 + 1, nMinSearchCount);
            }

        }

        public void SendInnerNetData(byte id)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(id));
            NetUdpSendFixedSizePackage mPackage = mThreadWorker.mSendPackagePool.Pop();
            mPackage.SetInnerCommandId(id);
            SendUDPPackage(mPackage);
            mThreadWorker.mSendPackagePool.recycle(mPackage);
        }

        public void SendUDPPackage(NetUdpSendFixedSizePackage mPackage)
        {
            bool bCanSendPackage = mPackage.orInnerCommandPackage() || m_Connected;
            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                ResetSendHeartBeatCdTime();
                SetRequestOrderId(mPackage);
                if (mPackage.orInnerCommandPackage())
                {
                    this.SendUDPPackage2(mPackage);
                }
                else
                {
                    UdpStatistical.AddSendCheckPackageCount();
                    this.SendUDPPackage2(mPackage);
                }
            }
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return nCurrentCheckPackageCount;
        }

        public void Reset()
        {
            MainThreadCheck.Check();

            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            mTcpSlidingWindow.WindowReset();
            foreach (var mRemovePackage in mWaitCheckSendQueue)
            {
                mThreadWorker.mSendPackagePool.recycle(mRemovePackage);
            }
            mWaitCheckSendQueue.Clear();

            while (mCacheReceivePackageList.Count > 0)
            {
                int nRemoveIndex = mCacheReceivePackageList.Count - 1;
                NetUdpReceiveFixedSizePackage mRemovePackage = mCacheReceivePackageList[nRemoveIndex];
                mCacheReceivePackageList.RemoveAt(nRemoveIndex);
                mThreadWorker.mReceivePackagePool.recycle(mRemovePackage);
            }

            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        public virtual void Dispose()
        {
            
        }

        //private void HandleConnectionEvent(ref QUIC_CONNECTION_EVENT connectionEvent)
        //{
        //    NetLog.Log("Connection Event: " + connectionEvent.Type.ToString());
        //    switch (connectionEvent.Type)
        //    {
        //        case E_CONNECTION_EVENT_TYPE.CONNECTED:
        //            HandleEventConnected(ref connectionEvent.CONNECTED);
        //            break;
        //        case E_CONNECTION_EVENT_TYPE.CLOSED:
        //            HandleEventShutdownInitiatedByTransport(ref connectionEvent.SHUTDOWN_INITIATED_BY_TRANSPORT);
        //            break;
        //        case E_CONNECTION_EVENT_TYPE.DATA_RECEIVED:
        //            HandleEventShutdownInitiatedByPeer(ref connectionEvent.SHUTDOWN_INITIATED_BY_PEER);
        //            break;
        //    }
        //}

    }
}
