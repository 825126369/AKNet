/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:43
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.LinuxTcp.Common;
using System;

namespace AKNet.LinuxTcp.Server
{
    internal class UDPLikeTCPMgr
    {
        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;
        private UdpServer mNetServer = null;
        private ClientPeerPrivate mClientPeer = null;
		
		public UDPLikeTCPMgr(UdpServer mNetServer, ClientPeerPrivate mClientPeer)
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
						if (fMySendHeartBeatCdTime >= mClientPeer.GetConfig().fMySendHeartBeatMaxTime)
						{
							fMySendHeartBeatCdTime = 0.0;
							SendHeartBeat();
						}

						// 有可能网络流量大的时候，会while循环卡住
						double fHeatTime = Math.Min(0.3, elapsed);
						fReceiveHeartBeatTime += fHeatTime;
						if (fReceiveHeartBeatTime >= mClientPeer.GetConfig().fReceiveHeartBeatTimeOut)
						{
							fReceiveHeartBeatTime = 0.0;
#if DEBUG
							NetLog.Log("Server 接收服务器心跳 超时 ");
#endif
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

		public void ReceiveConnect(sk_buff skb)
		{
			mClientPeer.Reset();
			mClientPeer.mUdpCheckPool.InitConnect();
			mClientPeer.mUdpCheckPool.FinishConnect(skb);

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

		public void Reset()
		{
            fReceiveHeartBeatTime = 0;
			fMySendHeartBeatCdTime = 0;
        }
	}
}