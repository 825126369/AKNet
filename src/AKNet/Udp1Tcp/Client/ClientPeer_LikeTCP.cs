/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp1Tcp.Common;

namespace AKNet.Udp1Tcp.Client
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
            Reset();
			SetSocketState(SOCKET_PEER_STATE.CONNECTING);
			NetLog.Log($"{NetType.Udp1Tcp.ToString()} 客户端 正在连接服务器: {remoteEndPoint}");
			SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
		}

		public void SendDisConnect()
		{
            Reset();
			SetSocketState(SOCKET_PEER_STATE.DISCONNECTING);
			NetLog.Log($"{NetType.Udp1Tcp.ToString()} 客户端 正在 断开服务器: {remoteEndPoint}");
			SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT);
		}

		public void ReceiveConnect()
		{
			if (GetSocketState() != SOCKET_PEER_STATE.CONNECTED)
			{
				this.Reset();
				SetSocketState(SOCKET_PEER_STATE.CONNECTED);
				NetLog.Log($"{NetType.Udp1Tcp.ToString()} 客户端 连接服务器 成功");
			}
		}

		public void ReceiveDisConnect()
		{
			if (GetSocketState() != SOCKET_PEER_STATE.DISCONNECTED)
			{
                Reset();
				SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
				NetLog.Log($"{NetType.Udp1Tcp.ToString()} 客户端 断开服务器 成功!");
			}
		}
    }

}