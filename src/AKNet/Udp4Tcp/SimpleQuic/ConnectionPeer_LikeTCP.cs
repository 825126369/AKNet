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

namespace AKNet.Udp4Tcp.Common
{
    internal partial class ConnectionPeer
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
			if (mConnectionPeerType == ConnectionPeerType.Client)
			{
				this.Reset();
				NetLog.Log("Client: Udp 正在连接服务器: " + RemoteEndPoint);
				SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
			}
		}

		public void SendDisConnect()
		{
			if (mConnectionPeerType == ConnectionPeerType.Client)
			{
				this.Reset();
				NetLog.Log("Client: Udp 正在 断开服务器: " + RemoteEndPoint);
				SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT);
			}
		}

		public void ReceiveConnect()
		{
			if (!m_Connected)
			{
                this.Reset();
                m_Connected = true;
                NetLog.Log("Client: Udp连接服务器 成功 ! ");
			}
		}

		public void ReceiveDisConnect()
		{
			if (m_Connected)
			{
				this.Reset();
                m_Connected = false;
                NetLog.Log("Client: Udp 断开服务器 成功 ! ");
			}
		}

	}

}