/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4Tcp.Common;
using System;
using System.Net;
using System.Runtime.CompilerServices;

namespace AKNet.Udp4Tcp.Client
{
    internal partial class ClientPeer : NetClientInterface, ClientPeerBase
    {
        private readonly ListenNetPackageMgr mPackageManager = new ListenNetPackageMgr();
        private readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
        private readonly CryptoMgr mCryptoMgr = new CryptoMgr();

        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private SOCKET_PEER_STATE mLastSocketPeerState = SOCKET_PEER_STATE.NONE;
        private string Name = string.Empty;
        private uint ID = 0;
               
        private double fReceiveHeartBeatTime = 0.0;
        private double fSendHeartBeatTime = 0.0;
        private double fReConnectServerCdTime = 0.0;

        private readonly AkCircularManyBuffer mSendStreamList = new AkCircularManyBuffer();
        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
        private readonly NetStreamPackage mNetPackage = new NetStreamPackage();

        private readonly ConnectionEventArgs ReceiveArgs = new ConnectionEventArgs();
        private readonly ConnectionEventArgs SendArgs = new ConnectionEventArgs();
        private readonly ConnectionEventArgs ConnectArgs = new ConnectionEventArgs();
        private readonly ConnectionEventArgs DisConnectArgs = new ConnectionEventArgs();

        private bool bConnectIOContexUsed = false;
        private bool bReceiveIOContexUsed = false;
        private bool bSendIOContexUsed = false;
        private bool bDisConnectIOContexUsed = false;
        
        private Connection mConnection = new Connection();
        private IPEndPoint mIPEndPoint = null;
        private string ServerIp;
        private int nServerPort;

        public ClientPeer()
        {
            MainThreadCheck.Check();
            SetSocketState(SOCKET_PEER_STATE.NONE);

            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            SendArgs.Completed += OnIOCompleted;
            ReceiveArgs.Completed += OnIOCompleted;
            ConnectArgs.Completed += OnIOCompleted;
            DisConnectArgs.Completed += OnIOCompleted;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendHeartBeat()
        {
            SendNetData(TcpNetCommand.COMMAND_HEARTBEAT);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetSendHeartBeatTime()
        {
            fSendHeartBeatTime = 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReceiveHeartBeat()
        {
            fReceiveHeartBeatTime = 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSocketState(SOCKET_PEER_STATE mState)
        {
            this.mSocketPeerState = mState;
        }

        public SOCKET_PEER_STATE GetSocketState()
        {
            return mSocketPeerState;
        }

        public void Reset()
        {
            lock (mSendStreamList)
            {
                mSendStreamList.Reset();
            }

            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Reset();
            }

            this.Name = string.Empty;
            this.ID = 0;
            this.fReConnectServerCdTime = 0.0;
            this.fReceiveHeartBeatTime = 0.0;
            this.fSendHeartBeatTime = 0.0;
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

        public void NetPackageExecute(NetPackage mPackage)
        {
            mPackageManager.NetPackageExecute(this, mPackage);
        }

        public void addNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            mPackageManager.addNetListenFunc(nPackageId, fun);
        }

        public void removeNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            mPackageManager.removeNetListenFunc(nPackageId, fun);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mPackageManager.addNetListenFunc(mFunc);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mPackageManager.removeNetListenFunc(mFunc);
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
