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
using System.Net;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    internal class ClientPeer_Private : TcpClientPeerBase, ClientPeerBase, IPoolItemInterface
	{
		private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;

        private double fSendHeartBeatTime = 0.0;
		private double fReceiveHeartBeatTime = 0.0;

        internal ClientPeerSocketMgr mSocketMgr;
		internal ClientPeerMsgReceiveMgr mMsgReceiveMgr;
		private TcpServer mNetServer;
		private string Name = string.Empty;
        private bool b_SOCKET_PEER_STATE_Changed = false;

        public ClientPeer_Private(TcpServer mNetServer)
		{
			this.mNetServer = mNetServer;
			mSocketMgr = new ClientPeerSocketMgr(this, mNetServer);
			mMsgReceiveMgr = new ClientPeerMsgReceiveMgr(this, mNetServer);
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

			mMsgReceiveMgr.Update(elapsed);
			switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTED:
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
				mSocketMgr.SendNetStream(mBufferSegment);
			}
		}

        public void SendNetData(ushort nPackageId, byte[] data)
        {
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				ResetSendHeartBeatTime();
				ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(nPackageId, data);
				mSocketMgr.SendNetStream(mBufferSegment);
			}
        }

        public void SendNetData(NetPackage mNetPackage)
        {
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				ResetSendHeartBeatTime();
				ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(mNetPackage.GetPackageId(), mNetPackage.GetData());
				this.mSocketMgr.SendNetStream(mBufferSegment);
			}
        }

		public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
		{
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				ResetSendHeartBeatTime();
				ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(nPackageId, buffer);
				mSocketMgr.SendNetStream(mBufferSegment);
			}
		}

        public void Reset()
		{
			fSendHeartBeatTime = 0.0;
			fReceiveHeartBeatTime = 0.0;
			mSocketMgr.Reset();
			mMsgReceiveMgr.Reset();
			SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		public void Release()
		{
            mSocketMgr.Release();
            mMsgReceiveMgr.Release();
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }


        public void HandleConnectedSocket(Socket mSocket)
		{
			mSocketMgr.HandleConnectedSocket(mSocket);
		}

        public IPEndPoint GetIPEndPoint()
        {
            return mSocketMgr.GetIPEndPoint();
        }

        public void SetName(string Name)
        {
            this.Name = Name;
        }

        public string GetName()
        {
            return this.Name;
        }
    }
}
