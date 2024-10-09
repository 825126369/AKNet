using Google.Protobuf;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    public abstract class ClientPeerBase
	{
		protected ServerBase mNetServer;
		protected SERVER_SOCKET_PEER_STATE mSocketPeerState = SERVER_SOCKET_PEER_STATE.NONE;
		private double fSendHeartBeatTime = 0.0;
		private double fReceiveHeartBeatTime = 0.0;

		protected ClientPeerBase(ServerBase mNetServer)
		{
			this.mNetServer = mNetServer;
		}

		public SERVER_SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

		public virtual void Update(double elapsed)
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

		protected void ReceiveHeartBeat()
		{
			fReceiveHeartBeatTime = 0.0;
		}

		public virtual void SendNetData(ushort nPackageId, IMessage data = null) { }

		internal virtual void Reset()
		{
			fSendHeartBeatTime = 0.0;
			fReceiveHeartBeatTime = 0.0;
		}
	}
}
