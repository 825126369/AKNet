using Google.Protobuf;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    internal class ClientPeer : ClientPeerBase
	{
		private SERVER_SOCKET_PEER_STATE mSocketPeerState = SERVER_SOCKET_PEER_STATE.NONE;
		private double fSendHeartBeatTime = 0.0;
		private double fReceiveHeartBeatTime = 0.0;

		internal ClientPeerSocketMgr mSocketMgr;
		internal MsgReceiveMgr mMsgReceiveMgr;
		internal MsgSendMgr mMsgSendMgr;

		public ClientPeer()
		{
			mSocketMgr = new ClientPeerSocketMgr(this);
			mMsgReceiveMgr = new MsgReceiveMgr(this);
			mMsgSendMgr = new MsgSendMgr(this);
		}

		public void SetSocketState(SERVER_SOCKET_PEER_STATE mSocketPeerState)
		{
			this.mSocketPeerState = mSocketPeerState;
		}

        public SERVER_SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

		public void Update(double elapsed)
		{
			switch (mSocketPeerState)
			{
				case SERVER_SOCKET_PEER_STATE.CONNECTED:
					fSendHeartBeatTime += elapsed;
					if (fSendHeartBeatTime >= ServerConfig.fSendHeartBeatMaxTimeOut)
					{
						SendHeartBeat();
						fSendHeartBeatTime = 0.0;
					}

					fReceiveHeartBeatTime += elapsed;
					if (fReceiveHeartBeatTime >= ServerConfig.fReceiveHeartBeatMaxTimeOut)
					{
						mSocketPeerState = SERVER_SOCKET_PEER_STATE.DISCONNECTED;
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

		public void SendNetData(ushort nPackageId, IMessage data = null)
		{
			mMsgSendMgr.SendNetData(nPackageId, data);
		}

		public void Reset()
		{
			fSendHeartBeatTime = 0.0;
			fReceiveHeartBeatTime = 0.0;
		}

		public void ConnectClient(Socket mSocket)
		{
			mSocketMgr.ConnectClient(mSocket);
		}

        public IPEndPoint GetIPEndPoint()
        {
            return mSocketMgr.GetIPEndPoint();
        }

        public uint GetUUID()
        {
            return mSocketMgr.GetUUID();
        }
    }
}
