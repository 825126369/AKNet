using Google.Protobuf;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Client
{
    internal class ClientPeer : ClientPeerBase
	{
		internal TCPSocketMgr mSocketMgr;
        internal MsgSendMgr mMsgSendMgr;
        internal MsgReceiveMgr mMsgReceiveMgr;

        private double fReConnectServerCdTime = 0.0;
        private double fSendHeartBeatTime = 0.0;
        private double fReceiveHeartBeatTime = 0.0;

        private CLIENT_SOCKET_PEER_STATE mSocketPeerState = CLIENT_SOCKET_PEER_STATE.NONE;

        public ClientPeer()
		{
			mSocketMgr = new TCPSocketMgr(this);
			mMsgSendMgr = new MsgSendMgr(this);
			mMsgReceiveMgr = new MsgReceiveMgr(this);
        }

		public void Update(double elapsed)
		{
			mMsgReceiveMgr.Update(elapsed);
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
						NetLog.Log("心跳超时");
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

        private void SendHeartBeat()
        {
            SendNetData(TcpNetCommand.COMMAND_HEARTBEAT);
        }

        public void ReceiveHeartBeat()
		{
			fReceiveHeartBeatTime = 0f;
		}

		
        public void ConnectServer(string Ip, ushort nPort)
		{
			mSocketMgr.ConnectServer(Ip, nPort);
		}

        public void ReConnectServer()
        {
            mSocketMgr.ReConnectServer();
        }

		public void SetSocketState(CLIENT_SOCKET_PEER_STATE mSocketPeerState)
		{
			this.mSocketPeerState = mSocketPeerState;
        }

        public CLIENT_SOCKET_PEER_STATE GetSocketState()
        {
			return this.mSocketPeerState;
        }

        public void SendNetData(ushort nPackageId, IMessage data = null)
        {
			mMsgSendMgr.SendNetData(nPackageId, data);
        }

        public void SendLuaNetData(ushort nPackageId, byte[] buffer = null)
        {
            mMsgSendMgr.SendLuaNetData(nPackageId, buffer);
        }

		public void Reset()
		{
			fReConnectServerCdTime = 0.0f;
			fSendHeartBeatTime = 0.0;
			fReceiveHeartBeatTime = 0.0;

			mSocketMgr.Reset();
			mMsgReceiveMgr.Reset();
			mMsgSendMgr.Reset();
		}

		public void Release()
		{
			mSocketMgr.Release();
			mMsgReceiveMgr.Release();
			mMsgSendMgr.Release();
		}

        public void addNetListenFun(ushort nPackageId, System.Action<ClientPeerBase, NetPackage> fun)
        {
			mMsgReceiveMgr.addNetListenFun(nPackageId, fun);
        }

		public void removeNetListenFun(ushort nPackageId, System.Action<ClientPeerBase, NetPackage> fun)
		{
			mMsgReceiveMgr.removeNetListenFun(nPackageId, fun);
		}

        public bool DisConnectServer()
        {
            return mSocketMgr.DisConnectServer();
        }
    }
}


