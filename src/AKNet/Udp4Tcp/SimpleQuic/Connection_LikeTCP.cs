/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Connection
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
			if (mConnectionType == E_CONNECTION_TYPE.Client)
			{
				this.OnConnectReset();
				NetLog.Log("Client: Udp 正在连接服务器: " + RemoteEndPoint);
				SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
			}
		}

		public void SendDisConnect()
		{
			if (mConnectionType == E_CONNECTION_TYPE.Client)
			{
				this.OnDisConnectReset();
				NetLog.Log("Client: Udp 正在 断开服务器: " + RemoteEndPoint);
				SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT);
			}
		}

		public void ReceiveConnect()
		{
			if (!m_Connected)
			{
				m_Connected = true;
                this.OnConnectReset();
                _connectedTcs.TrySetResult();
                if (mConnectionType == E_CONNECTION_TYPE.Server)
				{
					this.SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
				}
			}
		}

		public void ReceiveDisConnect()
		{
			if (m_Connected)
			{
                m_Connected = false;
                this.OnDisConnectReset();
                _disConnectedTcs.TrySetResult();
                if (mConnectionType == E_CONNECTION_TYPE.Server)
				{
                    SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT);
                }
            }
		}

	}

}