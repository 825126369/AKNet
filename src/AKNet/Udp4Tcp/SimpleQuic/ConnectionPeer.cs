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
    internal partial class ConnectionPeer : IPoolItemInterface
    {
        enum E_CONNECTION_EVENT_TYPE : byte
        {
            CONNECTED = 0,
            CLOSED = 1,  
            DATA_RECEIVED = 2,
        }
        
        private readonly Queue<NetUdpReceiveFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpReceiveFixedSizePackage>();
        private int nCurrentCheckPackageCount = 0;
        public IPEndPoint RemoteEndPoint;

        private readonly object lock_mSocket_object = new object();
        private readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        private readonly AkCircularManySpanBuffer mSendUdpPackageList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize, 1);
        private readonly AkCircularManySpanBuffer mReceiveUdpPackageList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize, 1);
        private bool bSendIOContexUsed = false;
        private int nLastSendBytesCount = 0;
        protected bool m_Connected;
        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;

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

        public ConnectionPeer()
        {
            //this.mServerMgr = mNetServer;
            
            this.nSearchCount = nMinSearchCount;
            this.nMaxSearchCount = this.nSearchCount * 2;
            this.nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            this.nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
            InitRTO();
        }

        public void Update()
        {
            GetReceiveCheckPackage();

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
                        SendNetPackage(mPackage);
                        ArrangeReSendTimeOut(mPackage);
                        mPackage.nSendCount++;
                        bTimeOut = true;
                    }
                }
                else
                {
                    UdpStatistical.AddFirstSendCheckPackageCount();
                    SendNetPackage(mPackage);
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
            NetUdpSendFixedSizePackage mPackage = GetObjectPoolManager().UdpSendPackage_Pop();
            mPackage.SetInnerCommandId(id);
            SendUDPPackage(mPackage);
            GetObjectPoolManager().UdpSendPackage_Recycle(mPackage);
        }

        public void SendUDPPackage(NetUdpSendFixedSizePackage mPackage)
        {
            bool bCanSendPackage = mPackage.orInnerCommandPackage() || Connected;
            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                ResetSendHeartBeatCdTime();
                mUdpCheckPool.SetRequestOrderId(mPackage);
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

        private bool GetReceiveCheckPackage()
        {
            NetUdpReceiveFixedSizePackage mPackage = null;
            if (GetReceivePackage(out mPackage))
            {
                UdpStatistical.AddReceivePackageCount();
                NetLog.Assert(mPackage != null, "mPackage == null");
                mUdpCheckPool.ReceiveNetPackage(mPackage);
                return true;
            }
            return false;
        }

        public bool GetReceivePackage(out NetUdpReceiveFixedSizePackage mPackage)
        {
            lock (mWaitCheckPackageQueue)
            {
                if (mWaitCheckPackageQueue.TryDequeue(out mPackage))
                {
                    if (!mPackage.orInnerCommandPackage())
                    {
                        nCurrentCheckPackageCount--;
                    }

                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            MainThreadCheck.Check();
            this.mUdpCheckPool.Reset();
            lock (mWaitCheckPackageQueue)
            {
                while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
                {
                    mServerMgr.GetObjectPoolManager().UdpReceivePackage_Recycle(mPackage);
                }
            }
        }

        private void SendHeartBeat()
        {
            SendInnerNetData(UdpNetCommand.COMMAND_HEARTBEAT);
        }

        public void ResetSendHeartBeatCdTime()
        {
            fMySendHeartBeatCdTime = 0.0;
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
