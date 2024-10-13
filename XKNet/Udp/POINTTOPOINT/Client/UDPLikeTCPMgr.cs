using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    internal class UDPLikeTCPMgr
	{
		private double fReceiveHeartBeatTime = 0.0;
		private double fMySendHeartBeatCdTime = 0.0;

		private double fConnectCdTime = 0.0;
		public const double fConnectMaxCdTime = 2.0;

		private double fDisConnectCdTime = 0.0;
		public const double fDisConnectMaxCdTime = 2.0;

		private double fReConnectServerCdTime = 0.0;


        private object lock_CdTime_obj = new object();

		private ClientPeer mClientPeer;
		public UDPLikeTCPMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
        }

		public void Update(double elapsed)
		{
			var mSocketPeerState = mClientPeer.GetSocketState();
			switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTING:
					lock (lock_CdTime_obj)
					{
						fConnectCdTime += elapsed;
						if (fConnectCdTime >= fConnectMaxCdTime)
						{
							SendConnect();
						}
					}

					break;
				case SOCKET_PEER_STATE.CONNECTED:
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
							fReConnectServerCdTime = 0.0;
							NetLog.Log("Client 接收服务器心跳 超时 ");
							SendConnect();
						}
					}

					break;
				case SOCKET_PEER_STATE.DISCONNECTING:
					lock (lock_CdTime_obj)
					{
						fDisConnectCdTime += elapsed;
						if (fDisConnectCdTime >= fDisConnectMaxCdTime)
						{
							ReceiveDisConnect();
						}
					}
					break;
				case SOCKET_PEER_STATE.DISCONNECTED:
					break;
				case SOCKET_PEER_STATE.RECONNECTING:
					fReConnectServerCdTime += elapsed;
					if (fReConnectServerCdTime >= Config.fReceiveReConnectMaxTimeOut)
					{
						mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTING);
						fReConnectServerCdTime = 0.0;
						mClientPeer.mSocketMgr.ReConnectServer();
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
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
			}
		}

		public void SendConnect()
		{
			lock (lock_CdTime_obj)
			{
				fConnectCdTime = 0.0;
				fReceiveHeartBeatTime = 0.0;
				fMySendHeartBeatCdTime = 0.0;
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTING);
			}

			mClientPeer.Reset();
			NetLog.Log("Client: Udp 正在连接服务器: " + mClientPeer.mSocketMgr.ip + " : " + mClientPeer.mSocketMgr.port);
			mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
		}

		public void ReceiveConnect()
		{
			lock (lock_CdTime_obj)
			{
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
				fConnectCdTime = 0.0;
				fReceiveHeartBeatTime = 0.0;
				fMySendHeartBeatCdTime = 0.0;
                fReConnectServerCdTime = 0.0;
            }

			NetLog.Log("Client: Udp连接服务器 成功 ! ");
		}

		public void SendDisConnect()
		{
			lock (lock_CdTime_obj)
			{
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTING);
				fReceiveHeartBeatTime = 0.0;
				fDisConnectCdTime = 0.0;
			}

			NetLog.Log("Client: Udp 正在 断开服务器: " + mClientPeer.mSocketMgr.ip + " : " + mClientPeer.mSocketMgr.port);
			mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT);
		}

		public void ReceiveDisConnect()
		{
			lock (lock_CdTime_obj)
			{
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
				fDisConnectCdTime = 0.0;
			}

			mClientPeer.mSocketMgr.DisConnectedWithNormal();
			NetLog.Log("Client: Udp 断开服务器 成功 ! ");
		}

	}

}