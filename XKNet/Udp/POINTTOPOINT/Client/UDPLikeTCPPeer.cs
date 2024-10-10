using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    public class UDPLikeTCPPeer : SocketSendPeer
	{
		private double fReceiveHeartBeatTime = 0.0;
		private double fMySendHeartBeatCdTime = 0.0;

		private double fConnectCdTime = 0.0;
		public const double fConnectMaxCdTime = 2.0;

		private double fDisConnectCdTime = 0.0;
		public const double fDisConnectMaxCdTime = 2.0;

		private object lock_CdTime_obj = new object();

		public override void Update(double elapsed)
		{
			base.Update(elapsed);

			switch (mSocketPeerState)
			{
				case CLIENT_SOCKET_PEER_STATE.CONNECTING:
					lock (lock_CdTime_obj)
					{
						fConnectCdTime += elapsed;
						if (fConnectCdTime >= fConnectMaxCdTime)
						{
							SendConnect();
						}
					}

					break;
				case CLIENT_SOCKET_PEER_STATE.CONNECTED:
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
							NetLog.Log("Client 接收服务器心跳 超时 ");
							SendConnect();
						}
					}

					break;
				case CLIENT_SOCKET_PEER_STATE.DISCONNECTING:
					lock (lock_CdTime_obj)
					{
						fDisConnectCdTime += elapsed;
						if (fDisConnectCdTime >= fDisConnectMaxCdTime)
						{
							ReceiveDisConnect();
						}
					}
					break;
				case CLIENT_SOCKET_PEER_STATE.DISCONNECTED:
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
				mSocketPeerState = CLIENT_SOCKET_PEER_STATE.CONNECTED;
			}
		}

		public void SendConnect()
		{
			lock (lock_CdTime_obj)
			{
				fConnectCdTime = 0.0;
				fReceiveHeartBeatTime = 0.0;
				fMySendHeartBeatCdTime = 0.0;
				mSocketPeerState = CLIENT_SOCKET_PEER_STATE.CONNECTING;
			}

			NetLog.Log("Client: Udp 正在连接服务器: " + this.ip + " : " + this.port);
			NetUdpFixedSizePackage mPackage = GetUdpSystemPackage(UdpNetCommand.COMMAND_CONNECT);
			SendNetPackage(mPackage);
		}

		public void ReceiveConnect()
		{
			this.Reset();

			lock (lock_CdTime_obj)
			{
				mSocketPeerState = CLIENT_SOCKET_PEER_STATE.CONNECTED;
				fConnectCdTime = 0.0;
				fReceiveHeartBeatTime = 0.0;
				fMySendHeartBeatCdTime = 0.0;
			}

			NetLog.Log("Client: Udp连接服务器 成功 ! ");
		}

		public void SendDisConnect()
		{
			this.Reset();

			lock (lock_CdTime_obj)
			{
				mSocketPeerState = CLIENT_SOCKET_PEER_STATE.DISCONNECTING;
				fReceiveHeartBeatTime = 0.0;
				fDisConnectCdTime = 0.0;
			}

			NetLog.Log("Client: Udp 正在 断开服务器: " + this.ip + " : " + this.port);
			NetUdpFixedSizePackage mPackage = GetUdpSystemPackage(UdpNetCommand.COMMAND_DISCONNECT);
			SendNetPackage(mPackage);
		}

		public void ReceiveDisConnect()
		{
			this.Reset();

			lock (lock_CdTime_obj)
			{
				mSocketPeerState = CLIENT_SOCKET_PEER_STATE.DISCONNECTED;
				fDisConnectCdTime = 0.0;
			}

			this.DisConnectedWithNormal();
			NetLog.Log("Client: Udp 断开服务器 成功 ! ");
		}

	}

}