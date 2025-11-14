/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2MSQuic.Common;
using System;
using System.Net;

namespace AKNet.Udp2MSQuic.Client
{
    internal class ClientPeer : QuicClientPeerBase, ClientPeerBase
    {
        internal readonly QuicConnectionMgr mSocketMgr;
        internal readonly MsgReceiveMgr mMsgReceiveMgr;
        internal readonly CryptoMgr mCryptoMgr;
        internal readonly Config mConfig;
        internal readonly ListenNetPackageMgr mPackageManager = null;
        internal readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;

        private double fReConnectServerCdTime = 0.0;
        private double fSendHeartBeatTime = 0.0;
        private double fReceiveHeartBeatTime = 0.0;

        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private bool b_SOCKET_PEER_STATE_Changed = false;
        private string Name = string.Empty;
        private uint ID = 0;
        public ClientPeer()
        {
            this.mConfig = new Config();
            mCryptoMgr = new CryptoMgr();
            mPackageManager = new ListenNetPackageMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
            mSocketMgr = new QuicConnectionMgr(this);
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
                mListenClientPeerStateMgr.OnSocketStateChanged(this);
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

                    double fHeatTime = Math.Min(0.3, elapsed);
                    fReceiveHeartBeatTime += fHeatTime;
                    if (fReceiveHeartBeatTime >= mConfig.fReceiveHeartBeatTimeOut)
                    {
                        fReceiveHeartBeatTime = 0.0;
                        fReConnectServerCdTime = 0.0;
                        mSocketPeerState = SOCKET_PEER_STATE.RECONNECTING;
#if DEBUG
                        NetLog.Log($"{this.Name}: 心跳超时");
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
            //NetLog.Log($"{this.Name}: 发送心跳");
            SendNetData(TcpNetCommand.COMMAND_HEARTBEAT);
        }

        private void ResetSendHeartBeatTime()
        {
            //NetLog.Log($"{this.Name}: 接收心跳");
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
                    mListenClientPeerStateMgr.OnSocketStateChanged(this);
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
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                var mBufferSegment = mCryptoMgr.Encode(nPackageId, ReadOnlySpan<byte>.Empty);
                mSocketMgr.SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                var mBufferSegment = mCryptoMgr.Encode(nPackageId, data);
                mSocketMgr.SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                var mBufferSegment = mCryptoMgr.Encode(mNetPackage.GetPackageId(), mNetPackage.GetData());
                mSocketMgr.SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                var mBufferSegment = mCryptoMgr.Encode(nPackageId, buffer);
                mSocketMgr.SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
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
            this.Name = string.Empty;
            this.ID = 0;
        }

		public void Release()
		{
			mSocketMgr.Release();
        }

        public bool DisConnectServer()
        {
            return mSocketMgr.DisConnectServer();
        }

        public IPEndPoint GetIPEndPoint()
        {
            return mSocketMgr.GetIPEndPoint();
        }

        public Config GetConfig()
        {
            return this.mConfig;
        }

        public void addNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            mPackageManager.addNetListenFunc(nPackageId, fun);
        }

        public void removeNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            mPackageManager.removeNetListenFunc(nPackageId, fun);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> func)
        {
            mPackageManager.addNetListenFunc(func);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> func)
        {
            mPackageManager.removeNetListenFunc(func);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mListenClientPeerStateMgr.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mListenClientPeerStateMgr.removeListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mListenClientPeerStateMgr.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mListenClientPeerStateMgr.removeListenClientPeerStateFunc(mFunc);
        }

        public void SetName(string name)
        {
            this.Name = name;
        }

        public string GetName()
        {
            return this.Name;
        }

        public void SetID(uint id)
        {
            this.ID = id;
        }

        public uint GetID()
        {
            return this.ID;
        }
    }
}


