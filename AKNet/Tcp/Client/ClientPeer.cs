/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:38
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Tcp.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Tcp.Client
{
    internal partial class ClientPeer : NetClientInterface, ClientPeerBase
    {
        internal readonly CryptoMgr mCryptoMgr;
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
            NetLog.Init();
            mCryptoMgr = new CryptoMgr();
            mPackageManager = new ListenNetPackageMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();

            mConnectIOContex = new SocketAsyncEventArgs();
            mDisConnectIOContex = new SocketAsyncEventArgs();
            mSendIOContex = new SocketAsyncEventArgs();
            mReceiveIOContex = new SocketAsyncEventArgs();

            mSendIOContex.Completed += OnIOCompleted;
            mReceiveIOContex.Completed += OnIOCompleted;
            mReceiveIOContex.SetBuffer(mIMemoryOwner_Receive.Memory);
            mConnectIOContex.Completed += OnIOCompleted;
            mDisConnectIOContex.Completed += OnIOCompleted;

            SetSocketState(SOCKET_PEER_STATE.NONE);
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
            
			switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTED:
                    int nPackageCount = 0;
                    while (NetPackageExecute())
                    {
                        nPackageCount++;
                    }
                    if (nPackageCount > 0)
                    {
                        ReceiveHeartBeat();
                    }


                    fSendHeartBeatTime += elapsed;
					if (fSendHeartBeatTime >= Config.fMySendHeartBeatMaxTime)
					{
                        fSendHeartBeatTime = 0.0;
                        SendHeartBeat();
					}

                    double fHeatTime = Math.Min(0.3, elapsed);
                    fReceiveHeartBeatTime += fHeatTime;
                    if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatTimeOut)
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
					if (fReConnectServerCdTime >= Config.fReConnectMaxCdTime)
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
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(nPackageId, ReadOnlySpan<byte>.Empty);
                SendNetStream(mBufferSegment);
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
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(nPackageId, data);
                SendNetStream(mBufferSegment);
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
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(mNetPackage.GetPackageId(), mNetPackage.GetData());
                SendNetStream(mBufferSegment);
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
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(nPackageId, buffer);
                SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void Reset()
        {
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            //Socket
            CloseSocket();
            lock (mSendStreamList)
            {
                mSendStreamList.Reset();
            }

            //接受
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Reset();
            }

            
            fReConnectServerCdTime = 0.0f;
            fSendHeartBeatTime = 0.0;
            fReceiveHeartBeatTime = 0.0;
            
            this.Name = string.Empty;
            this.ID = 0;
        }

		public void Release()
		{
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            mIMemoryOwner_Send.Dispose();
            mIMemoryOwner_Receive.Dispose();
            mSendStreamList.Dispose();
            mReceiveStreamList.Dispose();
            CloseSocket();
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


