using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class UDPLikeTCPMgr
    {
        private double fReceiveHeartBeatTime = 0.0;
		private double fMySendHeartBeatCdTime = 0.0;
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;
		
		public UDPLikeTCPMgr(UdpServer mNetServer, ClientPeer mClientPeer)
		{
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
		}

		public void Update(double elapsed)
		{
			var mSocketPeerState = mClientPeer.GetSocketState();
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

						fReceiveHeartBeatTime += elapsed;
						if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatTimeOut)
						{
							fReceiveHeartBeatTime = 0.0;
							mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
						}
						break;
					}
				default:
					break;
			}
		}

        private void SendHeartBeat()
		{
			mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_HEARTBEAT);
		}

        public void ResetSendHeartBeatCdTime()
        {
            fMySendHeartBeatCdTime = 0.0;
        }

        public void ReceiveHeartBeat()
		{
            fReceiveHeartBeatTime = 0.0;
        }

		public void ReceiveConnect()
		{
			mClientPeer.Reset();
			fReceiveHeartBeatTime = 0.0;
			fMySendHeartBeatCdTime = 0.0;
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
			mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
		}

		public void ReceiveDisConnect()
		{
			mClientPeer.Reset();
			fMySendHeartBeatCdTime = 0.0;
			fReceiveHeartBeatTime = 0.0;
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
			mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT);
		}
	}
}