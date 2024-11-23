/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Tcp.Common;
using Google.Protobuf;
using System;

namespace AKNet.Tcp.Client
{
    internal class ClientPeer : TcpClientPeerBase, TcpClientPeerCommonBase, ClientPeerBase
    {
        internal readonly TCPSocketMgr mSocketMgr;
        internal readonly MsgReceiveMgr mMsgReceiveMgr;
        internal readonly CryptoMgr mCryptoMgr;
        internal readonly Config mConfig;
        internal readonly PackageManager mPackageManager = null;

        private double fReConnectServerCdTime = 0.0;
        private double fSendHeartBeatTime = 0.0;
        private double fReceiveHeartBeatTime = 0.0;

        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private bool b_SOCKET_PEER_STATE_Changed = false;
        private event Action<ClientPeerBase> mListenSocketStateFunc = null;
        private string Name = string.Empty;

        public ClientPeer(TcpConfig mUserConfig)
        {
            NetLog.Init();
            if (mUserConfig == null)
            {
                this.mConfig = new Config();
            }
            else
            {
                this.mConfig = new Config(mUserConfig);
            }

            mCryptoMgr = new CryptoMgr(mConfig);
            mPackageManager = new PackageManager();
            mSocketMgr = new TCPSocketMgr(this);
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
					if (fSendHeartBeatTime >= mConfig.fMySendHeartBeatMaxTime)
					{
                        fSendHeartBeatTime = 0.0;
                        SendHeartBeat();
					}

                    double fHeatTime = elapsed;
                    if (fHeatTime > 0.3)
                    {
                        fHeatTime = 0.3;
                    }
                    fReceiveHeartBeatTime += fHeatTime;
                    if (fReceiveHeartBeatTime >= mConfig.fReceiveHeartBeatTimeOut)
                    {
                        fReceiveHeartBeatTime = 0.0;
                        fReConnectServerCdTime = 0.0;
                        mSocketPeerState = SOCKET_PEER_STATE.RECONNECTING;
#if DEBUG
                        NetLog.Log("心跳超时");
#endif
                    }
                    
					break;
				case SOCKET_PEER_STATE.RECONNECTING:
					fReConnectServerCdTime += elapsed;
					if (fReConnectServerCdTime >= mConfig.fReConnectMaxCdTime)
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

        private void ResetSendHeartBeatTime()
        {
            fSendHeartBeatTime = 0f;
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
            ResetSendHeartBeatTime();
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(nPackageId, null);
                mSocketMgr.SendNetStream(mBufferSegment);
            }
        }

        public void SendNetData(ushort nPackageId, IMessage data)
        {
            ResetSendHeartBeatTime();
            if (data == null)
            {
                SendNetData(nPackageId);
            }
            else
            {
                ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data);
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(nPackageId, stream);
                mSocketMgr.SendNetStream(mBufferSegment);
            }
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            ResetSendHeartBeatTime();
            SendNetData(nPackageId, data);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            ResetSendHeartBeatTime();
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(mNetPackage.nPackageId, mNetPackage.GetData());
                mSocketMgr.SendNetStream(mBufferSegment);
            }
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            ResetSendHeartBeatTime();
            if (buffer == ReadOnlySpan<byte>.Empty)
            {
                SendNetData(nPackageId);
            }
            else
            {
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(nPackageId, buffer);
                mSocketMgr.SendNetStream(mBufferSegment);
            }
        }

        public void Reset()
        {
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);

            fReConnectServerCdTime = 0.0f;
            fSendHeartBeatTime = 0.0;
            fReceiveHeartBeatTime = 0.0;

            mSocketMgr.Reset();
            mMsgReceiveMgr.Reset();
        }

		public void Release()
		{
			mSocketMgr.Release();
			mMsgReceiveMgr.Release();
            mListenSocketStateFunc = null;
        }

        public void addNetListenFun(ushort nPackageId, System.Action<ClientPeerBase, NetPackage> fun)
        {
			mPackageManager.addNetListenFun(nPackageId, fun);
        }

		public void removeNetListenFun(ushort nPackageId, System.Action<ClientPeerBase, NetPackage> fun)
		{
            mPackageManager.removeNetListenFun(nPackageId, fun);
		}

        public void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func)
        {
            mPackageManager.SetNetCommonListenFun(func);
        }

        public bool DisConnectServer()
        {
            return mSocketMgr.DisConnectServer();
        }

        public string GetIPAddress()
        {
            return mSocketMgr.GetIPEndPoint().Address.ToString();
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

        public Config GetConfig()
        {
            return this.mConfig;
        }
    }
}


