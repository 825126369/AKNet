/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using AKNet.Common;
using AKNet.Tcp.Common;
using System.Collections.Concurrent;

namespace AKNet.Tcp.Client
{
    internal class ClientPeer : TcpClientPeerBase, ClientPeerBase
	{
        internal TCPSocketMgr mSocketMgr;
        internal MsgSendMgr mMsgSendMgr;
        internal MsgReceiveMgr mMsgReceiveMgr;

        private double fReConnectServerCdTime = 0.0;
        private double fSendHeartBeatTime = 0.0;
        private double fReceiveHeartBeatTime = 0.0;

        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private bool b_SOCKET_PEER_STATE_Changed = false;
        private event Action<ClientPeerBase> mListenSocketStateFunc = null;
        private string Name = string.Empty;
        public ClientPeer()
		{
            NetLog.Init();
            mSocketMgr = new TCPSocketMgr(this);
			mMsgSendMgr = new MsgSendMgr(this);
			mMsgReceiveMgr = new MsgReceiveMgr(this);
        }

		public void Update(double elapsed)
		{
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("帧 时间 太长: " + elapsed);
            }

            if(b_SOCKET_PEER_STATE_Changed)
            {
                OnSocketStateChanged(this);
                b_SOCKET_PEER_STATE_Changed = false;
            }

            mMsgReceiveMgr.Update(elapsed);
			switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTED:
					fSendHeartBeatTime += elapsed;
					if (fSendHeartBeatTime >= Config.fSendHeartBeatMaxTimeOut)
					{
                        fSendHeartBeatTime = 0.0;
                        SendHeartBeat();
					}

					fReceiveHeartBeatTime += elapsed;
					if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatMaxTimeOut)
					{
						fReceiveHeartBeatTime = 0.0;
						fReConnectServerCdTime = 0.0;
						mSocketPeerState = SOCKET_PEER_STATE.RECONNECTING;
						NetLog.Log("心跳超时");
					}

					break;
				case SOCKET_PEER_STATE.RECONNECTING:
					fReConnectServerCdTime += elapsed;
					if (fReConnectServerCdTime >= Config.fReceiveReConnectMaxTimeOut)
					{
                        fReConnectServerCdTime = 0.0;
                        mSocketPeerState = SOCKET_PEER_STATE.CONNECTING;
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

		
        public void ConnectServer(string Ip, int nPort)
		{
			mSocketMgr.ConnectServer(Ip, nPort);
		}

        public void ReConnectServer()
        {
            mSocketMgr.ReConnectServer();
        }

        public void SetSocketState(SOCKET_PEER_STATE mSocketPeerState)
        {
            if (this.mSocketPeerState != mSocketPeerState)
            {
                this.mSocketPeerState = mSocketPeerState;

                if (MainThreadCheck.orInMainThread())
                {
                    OnSocketStateChanged(this);
                }
                else
                {
                    b_SOCKET_PEER_STATE_Changed = true;
                }
            }
        }

        public SOCKET_PEER_STATE GetSocketState()
        {
			return this.mSocketPeerState;
        }

        public void SendNetData(ushort nPackageId)
        {
            mMsgSendMgr.SendNetData(nPackageId);
        }

        public void SendNetData(ushort nPackageId, IMessage data)
        {
			mMsgSendMgr.SendNetData(nPackageId, data);
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            mMsgSendMgr.SendNetData(nPackageId, data);
        }

        public void Reset()
        {
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);

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
            mListenSocketStateFunc = null;
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

        public string GetIPAddress()
        {
            return mSocketMgr.GetIPEndPoint().Address.ToString();
        }

        public void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func)
        {
			mMsgReceiveMgr.SetNetCommonListenFun(func);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
			mMsgSendMgr.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            mMsgSendMgr.SendNetData(nPackageId, buffer);
        }

        private void OnSocketStateChanged(ClientPeerBase mClientPeer)
        {
            MainThreadCheck.Check();
            this.mListenSocketStateFunc?.Invoke(mClientPeer);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            this.mListenSocketStateFunc += mFunc;
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            this.mListenSocketStateFunc -= mFunc;
        }

        public string GetName()
        {
            return Name;
        }

        public void SetName(string Name)
        {
            this.Name = Name;
        }
    }
}


