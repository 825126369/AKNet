/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:35
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using System.Net;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Server
{
    internal class ClientPeer : TcpClientPeerCommonBase, TcpClientPeerBase, ClientPeerBase, IPoolItemInterface
	{
		private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;

        private double fSendHeartBeatTime = 0.0;
		private double fReceiveHeartBeatTime = 0.0;

        internal ClientPeerSocketMgr mSocketMgr;
		internal MsgReceiveMgr mMsgReceiveMgr;
		private TcpServer mNetServer;
		private string Name = string.Empty;
        private bool b_SOCKET_PEER_STATE_Changed = false;

        public ClientPeer(TcpServer mNetServer)
		{
			this.mNetServer = mNetServer;
			mSocketMgr = new ClientPeerSocketMgr(this, mNetServer);
			mMsgReceiveMgr = new MsgReceiveMgr(this, mNetServer);
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
					if (fSendHeartBeatTime >= mNetServer.mConfig.fSendHeartBeatMaxTimeOut)
					{
						SendHeartBeat();
						fSendHeartBeatTime = 0.0;
					}

                    double fHeatTime = elapsed;
                    if (fHeatTime > 0.3)
                    {
                        fHeatTime = 0.3;
                    }
                    fReceiveHeartBeatTime += fHeatTime;
					if (fReceiveHeartBeatTime >= mNetServer.mConfig.fReceiveHeartBeatMaxTimeOut)
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
			ResetSendHeartBeatTime();
            ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(nPackageId, null);
            mSocketMgr.SendNetStream(mBufferSegment);
        }

        public void SendNetData(ushort nPackageId, IMessage data)
		{
            ResetSendHeartBeatTime();
            if (data == null)
            {
                SendNetData(nPackageId);
            }
            else
            {
                ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data);
                ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(nPackageId, stream);
                mSocketMgr.SendNetStream(mBufferSegment);
            }
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            ResetSendHeartBeatTime();
            SendNetData(nPackageId, data.AsSpan());
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            ResetSendHeartBeatTime();
            ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(mNetPackage.nPackageId, mNetPackage.GetData());
            this.mSocketMgr.SendNetStream(mBufferSegment);
        }

		public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
		{
			ResetSendHeartBeatTime();
			if (buffer == ReadOnlySpan<byte>.Empty)
			{
				SendNetData(nPackageId);
			}
			else
			{
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

		public void HandleConnectedSocket(Socket mSocket)
		{
			mSocketMgr.HandleConnectedSocket(mSocket);
		}

        public IPEndPoint GetIPEndPoint()
        {
            return mSocketMgr.GetIPEndPoint();
        }

        public string GetIPAddress()
        {
            return mSocketMgr.GetIPEndPoint().Address.ToString();
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
