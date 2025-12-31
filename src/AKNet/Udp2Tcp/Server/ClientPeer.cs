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
using AKNet.Udp2Tcp.Common;
using System;
using System.Net.Sockets;

namespace AKNet.Udp2Tcp.Server
{
    internal partial class ClientPeer : UdpClientPeerCommonBase, ClientPeerBase
	{
        internal UdpCheckMgr mUdpCheckPool = null;
        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private SOCKET_PEER_STATE mLastSocketPeerState = SOCKET_PEER_STATE.NONE;
        private ServerMgr mServerMgr;
        private string Name = string.Empty;
        private uint ID = 0;
        internal readonly TcpStanardRTOFunc mTcpStanardRTOFunc = new TcpStanardRTOFunc();

        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;

        FakeSocket mSocket = null;
        readonly object lock_mSocket_object = new object();
        readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        readonly AkCircularManySpanBuffer mSendStreamList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize);
        bool bSendIOContexUsed = false;

        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();

        public ClientPeer(ServerMgr mNetServer)
        {
            this.mServerMgr = mNetServer;
            mUdpCheckPool = new UdpCheckMgr(this);
            SetSocketState(SOCKET_PEER_STATE.NONE);

            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
        }

        public void Update(double elapsed)
        {
            if (mLastSocketPeerState != mSocketPeerState)
            {
                mLastSocketPeerState = mSocketPeerState;
                this.mServerMgr.OnSocketStateChanged(this);
            }

            while (GetReceiveCheckPackage())
            {

            }

            while (NetTcpPackageExecute())
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

        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            bool bCanSendPackage = UdpNetCommand.orInnerCommand(mPackage.GetPackageId()) ||
                GetSocketState() == SOCKET_PEER_STATE.CONNECTED;

            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                ResetSendHeartBeatCdTime();

                if (Config.bUdpCheck)
                {
                    mUdpCheckPool.SetRequestOrderId(mPackage);
                    if (UdpNetCommand.orInnerCommand(mPackage.GetPackageId()))
                    {
                        SendNetPackage2(mPackage);
                    }
                    else
                    {
                        UdpStatistical.AddSendCheckPackageCount();
                        mPackage.mTcpStanardRTOTimer.BeginRtt();
                        SendNetPackage2(mPackage);
                    }
                }
                else
                {
                    SendNetPackage2(mPackage);
                }
            }
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

        public ObjectPoolManager GetObjectPoolManager()
        {
            return mServerMgr.GetObjectPoolManager();
        }

        public TcpStanardRTOFunc GetTcpStanardRTOFunc()
        {
            return mTcpStanardRTOFunc;
        }

        public void NetPackageExecute(NetPackage mPackage)
        {
            mServerMgr.GetPackageManager().NetPackageExecute(this, mPackage);
        }
    }
}
