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

        private readonly ServerMgr mServerMgr;
        private readonly Queue<NetUdpReceiveFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpReceiveFixedSizePackage>();
        private int nCurrentCheckPackageCount = 0;
        public IPEndPoint RemoteEndPoint;
        public UdpCheckMgr mUdpCheckPool = null;

        private readonly object lock_mSocket_object = new object();
        private readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        private readonly AkCircularManySpanBuffer mSendStreamList = null;
        private bool bSendIOContexUsed = false;
        private int nLastSendBytesCount = 0;
        private bool Connected;
        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;

        public ConnectionPeer(ServerMgr mNetServer)
        {
            this.mServerMgr = mNetServer;
            this.mUdpCheckPool = new UdpCheckMgr(this);

            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            mSendStreamList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize);


            mReSendPackageMgr = new ReSendPackageMgr(mClientPeer, this);
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        public void Update()
        {
            mUdpCheckPool.Update();
            GetReceiveCheckPackage();
        }

        public void AddTcpStream(ReadOnlySpan<byte> mBuffer)
        {
            mUdpCheckPool.SendTcpStream(mBuffer);
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
