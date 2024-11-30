/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;
using System;

namespace AKNet.Udp3Tcp.Client
{
    internal class UDPLikeTCPMgr
	{
		private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;

        private double fReConnectServerCdTime = 0.0;
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
						if (fMySendHeartBeatCdTime >= mClientPeer.GetConfig().fMySendHeartBeatMaxTime)
						{
							SendHeartBeat();
							fMySendHeartBeatCdTime = 0.0;
						}

						double fHeatTime = Math.Min(0.3, elapsed);
						fReceiveHeartBeatTime += fHeatTime;
						if (fReceiveHeartBeatTime >= mClientPeer.GetConfig().fReceiveHeartBeatTimeOut)
						{
							fReceiveHeartBeatTime = 0.0;
							fReConnectServerCdTime = 0.0;
#if DEBUG
							NetLog.Log("Client 接收服务器心跳 超时 ");
#endif
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
						if (fReConnectServerCdTime >= mClientPeer.GetConfig().fReConnectMaxCdTime)
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

        public void ResetSendHeartBeatCdTime()
        {
            fMySendHeartBeatCdTime = 0.0;
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
			NetLog.Log("Client: Udp 正在连接服务器: " + mClientPeer.mSocketMgr.GetIPEndPoint());
			mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
		}

		public void SendDisConnect()
		{
			this.Reset();
			mClientPeer.Reset();
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTING);
			NetLog.Log("Client: Udp 正在 断开服务器: " + mClientPeer.mSocketMgr.GetIPEndPoint());
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