using Google.Protobuf;
using System;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    internal class ClientPeer : TcpClientPeerBase, ClientPeerBase
	{
		private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;

        private double fSendHeartBeatTime = 0.0;
		private double fReceiveHeartBeatTime = 0.0;

		internal ClientPeerSocketMgr mSocketMgr;
		internal MsgReceiveMgr mMsgReceiveMgr;
		internal MsgSendMgr mMsgSendMgr;
		private TcpServer mNetServer;
		private string Name = string.Empty;
		public ClientPeer(TcpServer mNetServer)
		{
			this.mNetServer = mNetServer;
			mSocketMgr = new ClientPeerSocketMgr(this, mNetServer);
			mMsgReceiveMgr = new MsgReceiveMgr(this, mNetServer);
			mMsgSendMgr = new MsgSendMgr(this, mNetServer);
		}

		public void SetSocketState(SOCKET_PEER_STATE mSocketPeerState)
		{
			if (this.mSocketPeerState != mSocketPeerState)
			{
				this.mSocketPeerState = mSocketPeerState;
				this.mNetServer.OnSocketStateChanged(this);
			}
		}

        public SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

		public void Update(double elapsed)
		{
			mMsgReceiveMgr.Update(elapsed);
			switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTED:
					fSendHeartBeatTime += elapsed;
					if (fSendHeartBeatTime >= Config.fSendHeartBeatMaxTimeOut)
					{
						SendHeartBeat();
						fSendHeartBeatTime = 0.0;
					}

					fReceiveHeartBeatTime += elapsed;
					if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatMaxTimeOut)
					{
						mSocketPeerState = SOCKET_PEER_STATE.DISCONNECTED;
						fReceiveHeartBeatTime = 0.0;
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

		public void ReceiveHeartBeat()
		{
			fReceiveHeartBeatTime = 0.0;
		}

        public void SendNetData(ushort nPackageId)
        {
            mMsgSendMgr.SendNetData(nPackageId);
        }

        public void SendNetData(ushort nPackageId, IMessage data)
		{
			mMsgSendMgr.SendNetData(nPackageId, data);
		}

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            mMsgSendMgr.SendNetData(nPackageId, data);
        }

		public void Reset()
		{
			fSendHeartBeatTime = 0.0;
			fReceiveHeartBeatTime = 0.0;
			mSocketMgr.Reset();
			mMsgReceiveMgr.Reset();
			mMsgSendMgr.Reset();
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

        public void SendNetData(NetPackage mNetPackage)
        {
           mMsgSendMgr.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
			mMsgSendMgr.SendNetData(nPackageId, buffer);
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
