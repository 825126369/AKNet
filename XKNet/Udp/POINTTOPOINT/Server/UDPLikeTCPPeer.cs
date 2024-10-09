using XKNetCommon;
using XKNetUdpCommon;

namespace XKNetUdpServer
{
    public class UDPLikeTCPPeer : SocketSendPeer
	{
		private double fReceiveHeartBeatTime = 0.0;
		private double fMySendHeartBeatCdTime = 0.0;

		private object lock_CdTime_obj = new object();

		public override void Update(double elapsed)
		{
			base.Update(elapsed);
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
							mSocketPeerState = SERVER_SOCKET_PEER_STATE.DISCONNECTED;
							fReceiveHeartBeatTime = 0.0;

							NetLog.Log("Server 接收客户端 心跳 超时 ");
						}
					}

					break;
				default:
					break;
			}
		}

		private void SendHeartBeat()
		{
			NetUdpFixedSizePackage mPackage = GetUdpSystemPackage(UdpNetCommand.COMMAND_HEARTBEAT);
			SendNetPackage(mPackage);
		}

		public void ReceiveHeartBeat()
		{
			lock (lock_CdTime_obj)
			{
				fReceiveHeartBeatTime = 0.0;
				mSocketPeerState = SERVER_SOCKET_PEER_STATE.CONNECTED;
			}
		}

		public void ReceiveConnect()
		{
			this.Reset();

			lock (lock_CdTime_obj)
			{
				mSocketPeerState = SERVER_SOCKET_PEER_STATE.CONNECTED;
				fReceiveHeartBeatTime = 0.0;
				fMySendHeartBeatCdTime = 0.0;
			}
			
			NetUdpFixedSizePackage mPackage = GetUdpSystemPackage(UdpNetCommand.COMMAND_CONNECT);
			SendNetPackage(mPackage);
		}

		public void ReceiveDisConnect()
		{
			this.Reset();

			lock (lock_CdTime_obj)
			{
				mSocketPeerState = SERVER_SOCKET_PEER_STATE.DISCONNECTED;
				fReceiveHeartBeatTime = 0.0;
			}
			
			NetUdpFixedSizePackage mPackage = GetUdpSystemPackage(UdpNetCommand.COMMAND_DISCONNECT);
			SendNetPackage(mPackage);
		}
	}
}