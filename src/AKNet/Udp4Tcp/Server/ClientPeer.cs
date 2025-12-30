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
using AKNet.Udp4Tcp.Common;
using System;

namespace AKNet.Udp4Tcp.Server
{
    internal partial class ClientPeer : UdpClientPeerCommonBase, ClientPeerBase
	{
        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private SOCKET_PEER_STATE mLastSocketPeerState = SOCKET_PEER_STATE.NONE;
        private ServerMgr mServerMgr;
        private string Name = string.Empty;
        private uint ID = 0;
        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;
        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
        private Connection mConnection = null;

        private readonly ConnectionEventArgs ReceiveArgs = new ConnectionEventArgs();
        private readonly ConnectionEventArgs SendArgs = new ConnectionEventArgs();
        private readonly ConnectionEventArgs DisConnectArgs = new ConnectionEventArgs();
        private bool bReceiveIOContexUsed = false;
        private bool bSendIOContexUsed = false;
        private bool bDisConnectIOContexUsed = false;

        public ClientPeer(ServerMgr mNetServer)
        {
            this.mServerMgr = mNetServer;
            SetSocketState(SOCKET_PEER_STATE.NONE);
        }

        public void Update(double elapsed)
        {
            switch (mSocketPeerState)
            {
                case SOCKET_PEER_STATE.CONNECTED:
                    {
                        while (NetPackageExecute())
                        {

                        }

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
        }

        public void SetSocketState(SOCKET_PEER_STATE mState)
        {
            this.mSocketPeerState = mState;
        }

        public SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

        private void SendHeartBeat()
        {
            SendNetData(TcpNetCommand.COMMAND_HEARTBEAT);
        }

        public void ResetSendHeartBeatCdTime()
        {
            fMySendHeartBeatCdTime = 0.0;
        }

        public void ReceiveHeartBeat()
        {
            fReceiveHeartBeatTime = 0.0;
        }

        public void Reset()
        {
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            CloseSocket();

            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Reset();
            }

            this.Name = string.Empty;
            this.ID = 0;
            this.fReceiveHeartBeatTime = 0;
            this.fMySendHeartBeatCdTime = 0;
        }

        public void Release()
        {
            Reset();
            CloseSocket();
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
