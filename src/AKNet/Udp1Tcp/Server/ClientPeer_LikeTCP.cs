/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp1Tcp.Common;

namespace AKNet.Udp1Tcp.Server
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

		public void ReceiveConnect()
		{
            OnConnectReset();
			SetSocketState(SOCKET_PEER_STATE.CONNECTED);
			SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
		}

		public void ReceiveDisConnect()
		{
            OnDisConnectReset();
			SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
			SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT);
		}
	}
}