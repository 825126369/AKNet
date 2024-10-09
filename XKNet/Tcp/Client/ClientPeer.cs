using XKNetCommon;
using XKNetTcpServer;

namespace XKNetTcpClient
{
    public class ClientPeer : SocketSendPeer
	{
		private double fReConnectServerCdTime = 0.0;
		public override void Update(double elapsed)
		{
			base.Update(elapsed);
			switch (mSocketPeerState)
			{
				case CLIENT_SOCKET_PEER_STATE.CONNECTED:
					fSendHeartBeatTime += elapsed;
					if (fSendHeartBeatTime >= Config.fSendHeartBeatMaxTimeOut)
					{
						SendHeartBeat();
						fSendHeartBeatTime = 0.0;
					}

					fReceiveHeartBeatTime += elapsed;
					if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatMaxTimeOut)
					{
						fReceiveHeartBeatTime = 0.0;
						fReConnectServerCdTime = 0.0;
						mSocketPeerState = CLIENT_SOCKET_PEER_STATE.RECONNECTING;
                        NetLog.Log(InnerLogMessage.XinTiaoChaoShi);
					}

					break;
				case CLIENT_SOCKET_PEER_STATE.RECONNECTING:
					fReConnectServerCdTime += elapsed;
					if (fReConnectServerCdTime >= Config.fReceiveReConnectMaxTimeOut)
					{
						mSocketPeerState = CLIENT_SOCKET_PEER_STATE.CONNECTING;
						fReConnectServerCdTime = 0.0;
						ReConnectServer();
					}
					break;
				default:
					break;
			}
		}

        public override void Reset()
        {
            base.Reset();
			fReConnectServerCdTime = 0.0f;
		}
    }
}


