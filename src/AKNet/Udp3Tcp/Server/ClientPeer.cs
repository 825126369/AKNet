/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:50
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp3Tcp.Server
{
    internal partial class ClientPeer : UdpClientPeerCommonBase, ClientPeerBase
	{
        internal UdpCheckMgr mUdpCheckPool = null;
        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private SOCKET_PEER_STATE mLastSocketPeerState = SOCKET_PEER_STATE.NONE;
        private ServerMgr mServerMgr;

        private string Name = string.Empty;
        private uint ID = 0;
        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;

        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();

        private FakeSocket mSocket = null;
        private readonly object lock_mSocket_object = new object();
        private readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        private readonly AkCircularManySpanBuffer mSendStreamList = null;
        private bool bSendIOContexUsed = false;
        private int nLastSendBytesCount = 0;

        public ClientPeer(ServerMgr mNetServer)
        {
            this.mServerMgr = mNetServer;
            mUdpCheckPool = new UdpCheckMgr(this);
            SetSocketState(SOCKET_PEER_STATE.NONE);

            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            mSendStreamList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize);
        }

        public void Update(double elapsed)
        {
            while (GetReceiveCheckPackage())
            {

            }

            switch (mSocketPeerState)
            {
                case SOCKET_PEER_STATE.CONNECTED:
                    {
                        fMySendHeartBeatCdTime += elapsed;
                        if (fMySendHeartBeatCdTime >= Config.fMySendHeartBeatMaxTime)
                        {
                            fMySendHeartBeatCdTime = 0.0;
                            SendHeartBeat();
                        }

                        // 有可能网络流量大的时候，会while循环卡住
                        double fHeatTime = Math.Min(0.3, elapsed);
                        fReceiveHeartBeatTime += fHeatTime;
                        if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatTimeOut)
                        {
                            fReceiveHeartBeatTime = 0.0;
#if DEBUG
                            NetLog.Log("Server 接收服务器心跳 超时 ");
#endif
                            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                        }
                        break;
                    }
                default:
                    break;
            }

            if (this.mSocketPeerState != this.mLastSocketPeerState)
            {
                this.mLastSocketPeerState = mSocketPeerState;
                mServerMgr.OnSocketStateChanged(this);
            }

            mUdpCheckPool.Update(elapsed);
        }

        public void SetSocketState(SOCKET_PEER_STATE mState)
        {
            this.mSocketPeerState = mState;
        }

        public SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

        private void OnConnectReset()
        {
            this.mUdpCheckPool.Reset();
            lock (mSendStreamList)
            {
                this.mSendStreamList.Reset();
            }
            this.fReceiveHeartBeatTime = 0;
            this.fMySendHeartBeatCdTime = 0;
        }

        private void OnDisConnectReset()
        {
            this.mUdpCheckPool.Reset();
            lock (mSendStreamList)
            {
                this.mSendStreamList.Reset();
            }
            this.fReceiveHeartBeatTime = 0;
            this.fMySendHeartBeatCdTime = 0;
        }

        public void Reset()
        {
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            CloseSocket();

            this.mUdpCheckPool.Reset();
            lock (mSendStreamList)
            {
                this.mSendStreamList.Reset();
            }

            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Reset();
            }

            this.Name = string.Empty;
            this.ID = 0;
            this.fReceiveHeartBeatTime = 0;
            this.fMySendHeartBeatCdTime = 0;
            this.bSendIOContexUsed = false;
        }

        public void Release()
        {
            Reset();

            lock (mSendStreamList)
            {
                this.mSendStreamList.Dispose();
            }

            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Dispose();
            }
        }

        public void SendNetPackage(NetUdpSendFixedSizePackage mPackage)
        {
            bool bCanSendPackage = mPackage.orInnerCommandPackage() ||
                GetSocketState() == SOCKET_PEER_STATE.CONNECTED;

            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                ResetSendHeartBeatCdTime();
                mUdpCheckPool.SetRequestOrderId(mPackage);
                if (mPackage.orInnerCommandPackage())
                {
                    this.SendNetPackage2(mPackage);
                }
                else
                {
                    UdpStatistical.AddSendCheckPackageCount();
                    this.SendNetPackage2(mPackage);
                }
            }
        }

        public ObjectPoolManager GetObjectPoolManager()
        {
            return mServerMgr.GetObjectPoolManager();
        }

        public void NetPackageExecute(NetPackage mPackage)
        {
            mServerMgr.GetPackageManager().NetPackageExecute(this, mPackage);
        }

        public void SetName(string name)
        {
            this.Name = name;
        }

        public string GetName()
        {
            return this.Name;
        }

        public void SetID(uint id)
        {
            this.ID = id;
        }

        public uint GetID()
        {
            return this.ID;
        }
    }
}
