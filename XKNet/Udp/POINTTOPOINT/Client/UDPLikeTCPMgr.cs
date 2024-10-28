using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    internal class UDPLikeTCPMgr
	{
		private double fReceiveHeartBeatTime = 0.0;
		private double fMySendHeartBeatCdTime = 0.0;

        private double fReConnectServerCdTime = 0.0;
		private const double fReConnectMaxCdTime = 2.0;

        private double fConnectCdTime = 0.0;
		public const double fConnectMaxCdTime = 2.0;

		private double fDisConnectCdTime = 0.0;
		public const double fDisConnectMaxCdTime = 2.0;

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
					{
						fConnectCdTime += elapsed;
						if (fConnectCdTime >= fConnectMaxCdTime)
						{
							mClientPeer.mSocketMgr.ConnectServer();
						}
						break;
					}
				case SOCKET_PEER_STATE.CONNECTED:
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
							mClientPeer.SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
						}
						break;
					}
				case SOCKET_PEER_STATE.DISCONNECTING:
					{
						fDisConnectCdTime += elapsed;
						if (fDisConnectCdTime >= fDisConnectMaxCdTime)
						{
							SendDisConnect();
						}
						break;
					}
				case SOCKET_PEER_STATE.DISCONNECTED:
					break;
				case SOCKET_PEER_STATE.RECONNECTING:
					{
						fReConnectServerCdTime += elapsed;
						if (fReConnectServerCdTime >= fReConnectMaxCdTime)
						{
							fReConnectServerCdTime = 0.0;
							mClientPeer.mSocketMgr.ReConnectServer();
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

		public void ReceiveHeartBeat()
		{
			fReceiveHeartBeatTime = 0.0;
		}

		public void SendConnect()
		{
            this.Reset();
            mClientPeer.Reset();
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTING);
			NetLog.Log("Client: Udp 正在连接服务器: " + mClientPeer.mSocketMgr.ip + " : " + mClientPeer.mSocketMgr.port);
			mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
		}

		public void SendDisConnect()
		{
			this.Reset();
			mClientPeer.Reset();
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTING);
			NetLog.Log("Client: Udp 正在 断开服务器: " + mClientPeer.mSocketMgr.ip + " : " + mClientPeer.mSocketMgr.port);
			mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT);
		}

		public void ReceiveConnect()
		{
			if (mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED)
			{
                this.Reset();
                mClientPeer.Reset();
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
				NetLog.Log("Client: Udp连接服务器 成功 ! ");
			}
		}

		public void ReceiveDisConnect()
		{
			if (mClientPeer.GetSocketState() != SOCKET_PEER_STATE.DISCONNECTED)
			{
				this.Reset();
				mClientPeer.Reset();
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
				mClientPeer.mSocketMgr.DisConnectedWithNormal();
				NetLog.Log("Client: Udp 断开服务器 成功 ! ");
			}
		}

		private void Reset()
		{
            fConnectCdTime = 0.0;
            fDisConnectCdTime = 0.0;
            fReConnectServerCdTime = 0.0;
            fReceiveHeartBeatTime = 0.0;
            fMySendHeartBeatCdTime = 0.0;
        }



	}

}