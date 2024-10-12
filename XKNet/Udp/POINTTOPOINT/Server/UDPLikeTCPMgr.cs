using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class UDPLikeTCPMgr
    {
        private double fReceiveHeartBeatTime = 0.0;
		private double fMySendHeartBeatCdTime = 0.0;
		private object lock_CdTime_obj = new object();
		
        private NetServer mNetServer = null;
        private ClientPeer mClientPeer = null;
		
		public UDPLikeTCPMgr(NetServer mNetServer, ClientPeer mClientPeer)
		{
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
		}

		public void Update(double elapsed)
		{
			var mSocketPeerState = mClientPeer.GetSocketState();
			switch (mSocketPeerState)
			{
				case SERVER_SOCKET_PEER_STATE.CONNECTED:
					lock (lock_CdTime_obj)
					{
						fMySendHeartBeatCdTime += elapsed;
						if (fMySendHeartBeatCdTime >= Config.fMySendHeartBeatMaxTime)
						{
							SendHeartBeat();
							fMySendHeartBeatCdTime = 0.0;
						}

						fReceiveHeartBeatTime += elapsed;
						if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatTimeOut)
						{
							fReceiveHeartBeatTime = 0.0;
							mClientPeer.SetSocketState(SERVER_SOCKET_PEER_STATE.DISCONNECTED);
						}
					}

					break;
				default:
					break;
			}
		}

        private void SendHeartBeat()
		{
			mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_HEARTBEAT);
		}

		public void ReceiveHeartBeat()
		{
			lock (lock_CdTime_obj)
			{
				fReceiveHeartBeatTime = 0.0;
				mClientPeer.SetSocketState(SERVER_SOCKET_PEER_STATE.CONNECTED);
			}
		}

		public void ReceiveConnect()
		{
			mClientPeer.Reset();

			lock (lock_CdTime_obj)
			{
				mClientPeer.SetSocketState(SERVER_SOCKET_PEER_STATE.CONNECTED);
				fReceiveHeartBeatTime = 0.0;
				fMySendHeartBeatCdTime = 0.0;
			}
			
            mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
        }

		public void ReceiveDisConnect()
		{
			mClientPeer.Reset();

			lock (lock_CdTime_obj)
			{
				mClientPeer.SetSocketState(SERVER_SOCKET_PEER_STATE.DISCONNECTED);
				fReceiveHeartBeatTime = 0.0;
			}
			mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT);
		}
	}
}