/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;
using System;

namespace AKNet.Udp3Tcp.Client
{
    internal partial class ClientPeer
    {
		private void SendHeartBeat()
		{
			SendInnerNetData(UdpNetCommand.COMMAND_HEARTBEAT);
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