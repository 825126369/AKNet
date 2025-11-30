/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Tcp.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    internal partial class ClientPeer : ClientPeerBase, IPoolItemInterface
	{
		private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private SOCKET_PEER_STATE mLastSocketPeerState = SOCKET_PEER_STATE.NONE;

        private double fSendHeartBeatTime = 0.0;
		private double fReceiveHeartBeatTime = 0.0;
		
		private ServerMgr mNetServer;
		private string Name = string.Empty;
        private uint ID = 0;

        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();

        private readonly byte[] mIMemoryOwner_Send = new byte[Config.nIOContexBufferLength];
        private readonly byte[] mIMemoryOwner_Receive = new byte[Config.nIOContexBufferLength];
        private readonly SocketAsyncEventArgs mReceiveIOContex = new SocketAsyncEventArgs();
        private readonly SocketAsyncEventArgs mSendIOContex = new SocketAsyncEventArgs();
        private readonly AkCircularManyBuffer mSendStreamList = new AkCircularManyBuffer();
        private readonly object lock_mSocket_object = new object();
        private Socket mSocket = null;
        private bool bSendIOContextUsed = false;

        public ClientPeer(ServerMgr mNetServer)
		{
			this.mNetServer = mNetServer;

            mReceiveIOContex.SetBuffer(mIMemoryOwner_Receive, 0, mIMemoryOwner_Receive.Length);
            mSendIOContex.SetBuffer(mIMemoryOwner_Send, 0, mIMemoryOwner_Send.Length);
            mSendIOContex.Completed += OnIOCompleted;
            mReceiveIOContex.Completed += OnIOCompleted;
            bSendIOContextUsed = false;

            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

		public void SetSocketState(SOCKET_PEER_STATE mSocketPeerState)
		{
            this.mSocketPeerState = mSocketPeerState;
        }

        public SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

		public void Update(double elapsed)
		{
			switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTED:
                    int nPackageCount = 0;
                    while (NetPackageExecute())
                    {
                        nPackageCount++;
                    }

                    if (nPackageCount > 0)
                    {
                        ReceiveHeartBeat();
                    }

                    //if (nPackageCount > 100)
                    //{
                    //	NetLog.LogWarning("Server ClientPeer 处理逻辑包的数量： " + nPackageCount);
                    //}

                    fSendHeartBeatTime += elapsed;
					if (fSendHeartBeatTime >= Config.fMySendHeartBeatMaxTime)
					{
						SendHeartBeat();
						fSendHeartBeatTime = 0.0;
					}

                    double fHeatTime = Math.Min(0.3, elapsed);
                    fReceiveHeartBeatTime += fHeatTime;
					if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatTimeOut)
					{
						mSocketPeerState = SOCKET_PEER_STATE.DISCONNECTED;
						fReceiveHeartBeatTime = 0.0;
#if DEBUG
						NetLog.Log("心跳超时");
#endif
					}

					break;
				default:
					break;
			}

            if (this.mSocketPeerState != this.mLastSocketPeerState)
            {
                this.mLastSocketPeerState = mSocketPeerState;
                mNetServer.OnSocketStateChanged(this);
            }
        }

		private void SendHeartBeat()
		{
			SendNetData(TcpNetCommand.COMMAND_HEARTBEAT);
		}

        private void ResetSendHeartBeatTime()
        {
            fSendHeartBeatTime = 0f;
        }

        public void ReceiveHeartBeat()
		{
			fReceiveHeartBeatTime = 0.0;
		}

		public void Reset()
		{
			fSendHeartBeatTime = 0.0;
			fReceiveHeartBeatTime = 0.0;
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Reset();
            }

            CloseSocket();
            lock (mSendStreamList)
            {
                mSendStreamList.Reset();
            }

            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
			this.Name = string.Empty;
			this.ID = 0;
		}

		public void Release()
		{
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            CloseSocket();

            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Dispose();
            }
            
            lock (mSendStreamList)
            {
                mSendStreamList.Dispose();
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
    }
}
