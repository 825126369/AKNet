/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Tcp.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    internal partial class ClientPeer : PrivateInterface, TcpClientPeerBase, ClientPeerBase, IPoolItemInterface
	{
		private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private double fSendHeartBeatTime = 0.0;
		private double fReceiveHeartBeatTime = 0.0;
		private TcpServer mNetServer;
		private string Name = string.Empty;
        private bool b_SOCKET_PEER_STATE_Changed = false;
        private readonly AkCircularManyBuffer mReceiveStreamList = new AkCircularManyBuffer();
        private readonly object lock_mReceiveStreamList_object = new object();

        private readonly SocketAsyncEventArgs mReceiveIOContex = new SocketAsyncEventArgs();
        private readonly SocketAsyncEventArgs mSendIOContex = new SocketAsyncEventArgs();
        private bool bSendIOContextUsed = false;
        private readonly AkCircularManyBuffer mSendStreamList = new AkCircularManyBuffer();
        private Socket mSocket = null;
        private readonly object lock_mSocket_object = new object();

        public ClientPeer(TcpServer mNetServer)
		{
			this.mNetServer = mNetServer;

            if (mSendIOContex.Buffer == null)
            {
                mSendIOContex.SetBuffer(new byte[Config.nIOContexBufferLength], 0, Config.nIOContexBufferLength);
            }

            if (mReceiveIOContex.Buffer == null)
            {
                mReceiveIOContex.SetBuffer(new byte[Config.nIOContexBufferLength], 0, Config.nIOContexBufferLength);
            }

            mReceiveIOContex.Completed += OnIOCompleted;
            mSendIOContex.Completed += OnIOCompleted;
            bSendIOContextUsed = false;
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

		public void SetSocketState(SOCKET_PEER_STATE mSocketPeerState)
		{
			if (this.mSocketPeerState != mSocketPeerState)
			{
				this.mSocketPeerState = mSocketPeerState;

				if (MainThreadCheck.orInMainThread())
				{
					this.mNetServer.OnSocketStateChanged(this);
				}
				else
				{
					b_SOCKET_PEER_STATE_Changed = true;
				}
			}
		}

        public SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

		public void Update(double elapsed)
		{
			if (b_SOCKET_PEER_STATE_Changed)
			{
				mNetServer.OnSocketStateChanged(this);
				b_SOCKET_PEER_STATE_Changed = false;
			}

			switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTED:
					fSendHeartBeatTime += elapsed;
					if (fSendHeartBeatTime >= mNetServer.mConfig.fMySendHeartBeatMaxTime)
					{
						SendHeartBeat();
						fSendHeartBeatTime = 0.0;
					}

					double fHeatTime = Math.Min(0.3, elapsed);
					fReceiveHeartBeatTime += fHeatTime;
					if (fReceiveHeartBeatTime >= mNetServer.mConfig.fReceiveHeartBeatTimeOut)
					{
						mSocketPeerState = SOCKET_PEER_STATE.DISCONNECTED;
						fReceiveHeartBeatTime = 0.0;
#if DEBUG
						NetLog.Log("心跳超时");
#endif
					}

					int nPackageCount = 0;
					while (NetPackageExecute())
					{
						nPackageCount++;
					}

					if (nPackageCount > 0)
					{
						ReceiveHeartBeat();
					}

					break;
				default:
					break;
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

		public void SendNetData(ushort nPackageId)
		{
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				ResetSendHeartBeatTime();
				ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(nPackageId, ReadOnlySpan<byte>.Empty);
				SendNetStream(mBufferSegment);
			}
		}

        public void SendNetData(ushort nPackageId, byte[] data)
        {
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				ResetSendHeartBeatTime();
				ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(nPackageId, data);
				SendNetStream(mBufferSegment);
			}
        }

        public void SendNetData(NetPackage mNetPackage)
        {
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				ResetSendHeartBeatTime();
				ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(mNetPackage.GetPackageId(), mNetPackage.GetData());
				SendNetStream(mBufferSegment);
			}
        }

		public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
		{
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				ResetSendHeartBeatTime();
				ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(nPackageId, buffer);
				SendNetStream(mBufferSegment);
			}
		}

        public void Reset()
		{
            CloseSocket();
            fSendHeartBeatTime = 0.0;
			fReceiveHeartBeatTime = 0.0;
			SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Reset();
            }

            lock (mSendStreamList)
            {
                mSendStreamList.Reset();
            }
        }

        public string GetIPAddress()
        {
            return GetIPEndPoint().Address.ToString();
        }

        public void SetName(string Name)
        {
            this.Name = Name;
        }

        public string GetName()
        {
            return this.Name;
        }

        public Config GetConfig()
        {
            return mNetServer.mConfig;
        }
    }
}
