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
using System.Runtime.CompilerServices;

namespace AKNet.Tcp.Client
{
    internal partial class ClientPeer : NetClientInterface, ClientPeerBase
    {
        private readonly CryptoMgr mCryptoMgr;
        private readonly ListenNetPackageMgr mPackageManager = null;
        private readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;

        private double fReConnectServerCdTime = 0.0;
        private double fSendHeartBeatTime = 0.0;
        private double fReceiveHeartBeatTime = 0.0;

        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private SOCKET_PEER_STATE mLastSocketPeerState = SOCKET_PEER_STATE.NONE;
        private string Name = string.Empty;
        private uint ID = 0;

        //Receive
        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
        private readonly NetStreamPackage mNetPackage = new NetStreamPackage();

        //Socket
        private Socket mSocket = null;
        private string ServerIp = "";
        private int nServerPort = 0;
        private IPEndPoint mIPEndPoint = null;
        private bool bConnectIOContexUsed = false;
        private bool bDisConnectIOContexUsed = false;
        private bool bSendIOContextUsed = false;
        private bool bReceiveIOContextUsed = false;
        private readonly AkCircularManyBuffer mSendStreamList = new AkCircularManyBuffer();
        private readonly object lock_mSocket_object = new object();
        private readonly SocketAsyncEventArgs mConnectIOContex = new SocketAsyncEventArgs();
        private readonly SocketAsyncEventArgs mDisConnectIOContex = new SocketAsyncEventArgs();
        private readonly SocketAsyncEventArgs mSendIOContex = new SocketAsyncEventArgs();
        private readonly SocketAsyncEventArgs mReceiveIOContex = new SocketAsyncEventArgs();
        private readonly byte[] mIMemoryOwner_Send = new byte[Config.nIOContexBufferLength];
        private readonly byte[] mIMemoryOwner_Receive = new byte[Config.nIOContexBufferLength];

        public ClientPeer()
        {
            mCryptoMgr = new CryptoMgr();
            mPackageManager = new ListenNetPackageMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();

            mSendIOContex.Completed += OnIOCompleted;
            mReceiveIOContex.Completed += OnIOCompleted;
            mReceiveIOContex.SetBuffer(mIMemoryOwner_Receive, 0, mIMemoryOwner_Receive.Length);
            mSendIOContex.SetBuffer(mIMemoryOwner_Send, 0, mIMemoryOwner_Send.Length);
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

            switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTED:
                    {
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
                    }
                    break;
				case SOCKET_PEER_STATE.RECONNECTING:
                    {
                        fReConnectServerCdTime += elapsed;
                        if (fReConnectServerCdTime >= Config.fReConnectMaxCdTime)
                        {
                            fReConnectServerCdTime = 0.0;
                            mSocketPeerState = SOCKET_PEER_STATE.CONNECTING;
                            ReConnectServer();
                        }
                    }
					break;
				default:
					break;
			}

            if (this.mSocketPeerState != this.mLastSocketPeerState)
            {
                this.mLastSocketPeerState = mSocketPeerState;
                mListenClientPeerStateMgr.OnSocketStateChanged(this);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSocketState(SOCKET_PEER_STATE mSocketPeerState)
        {
            this.mSocketPeerState = mSocketPeerState;
        }

        public SOCKET_PEER_STATE GetSocketState()
        {
			return this.mSocketPeerState;
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
            CloseSocket();
            lock (mSendStreamList)
            {
                mSendStreamList.Dispose();
            }

            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Dispose();
            }
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


