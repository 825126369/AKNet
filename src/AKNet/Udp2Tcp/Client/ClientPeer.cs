/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp2Tcp.Client
{
    internal partial class ClientPeer : UdpClientPeerCommonBase, NetClientInterface, ClientPeerBase
    {
        internal readonly ListenNetPackageMgr mPackageManager = new ListenNetPackageMgr();
        internal readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = new ListenClientPeerStateMgr();

        internal readonly UdpCheckMgr mUdpCheckPool = null;
        internal readonly TcpStanardRTOFunc mTcpStanardRTOFunc = new TcpStanardRTOFunc();
        internal readonly CryptoMgr mCryptoMgr;

        private readonly ObjectPoolManager mObjectPoolManager;
        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private SOCKET_PEER_STATE mLastSocketPeerState = SOCKET_PEER_STATE.NONE;
        private string Name = string.Empty;
        private uint ID = 0;

        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;
        private double fReConnectServerCdTime = 0.0;
        private double fConnectCdTime = 0.0;
        public const double fConnectMaxCdTime = 2.0;
        private double fDisConnectCdTime = 0.0;
        public const double fDisConnectMaxCdTime = 2.0;

        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
        protected readonly NetStreamPackage mNetPackage = new NetStreamPackage();
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private int nCurrentCheckPackageCount = 0;

        private readonly SocketAsyncEventArgs ReceiveArgs = new SocketAsyncEventArgs();
        private readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        private readonly object lock_mSocket_object = new object();
        private readonly AkCircularSpanBuffer mSendStreamList = new AkCircularSpanBuffer();
        private Socket mSocket = null;
        private IPEndPoint remoteEndPoint = null;
        private string ServerIp;
        private int ServerPort;

        bool bReceiveIOContexUsed = false;
        bool bSendIOContexUsed = false;

        public ClientPeer()
        {
            NetLog.Init();
            MainThreadCheck.Check();
            mCryptoMgr = new CryptoMgr();
            mObjectPoolManager = new ObjectPoolManager();
            mUdpCheckPool = new UdpCheckMgr(this);

            SetSocketState(SOCKET_PEER_STATE.NONE);
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, int.MaxValue);

            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.Completed += ProcessReceive;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            SendArgs.Completed += ProcessSend;

            bReceiveIOContexUsed = false;
            bSendIOContexUsed = false;

        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("NetClient 帧 时间 太长: " + elapsed);
            }

            while (NetCheckPackageExecute())
            {

            }

            while (NetTcpPackageExecute())
            {

            }

            switch (mSocketPeerState)
            {
                case SOCKET_PEER_STATE.CONNECTING:
                    {
                        fConnectCdTime += elapsed;
                        if (fConnectCdTime >= fConnectMaxCdTime)
                        {
                            ConnectServer();
                        }
                        break;
                    }
                case SOCKET_PEER_STATE.CONNECTED:
                    {
                        fMySendHeartBeatCdTime += elapsed;
                        if (fMySendHeartBeatCdTime >= Config.fMySendHeartBeatMaxTime)
                        {
                            SendHeartBeat();
                            fMySendHeartBeatCdTime = 0.0;
                        }

                        double fHeatTime = Math.Min(0.3, elapsed);
                        fReceiveHeartBeatTime += fHeatTime;
                        if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatTimeOut)
                        {
                            fReceiveHeartBeatTime = 0.0;
                            fReConnectServerCdTime = 0.0;
#if DEBUG
                            NetLog.Log("Client 接收服务器心跳 超时 ");
#endif
                            SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
                        }
                        break;
                    }
                case SOCKET_PEER_STATE.DISCONNECTING:
                    {
                        fDisConnectCdTime += elapsed;
                        if (fDisConnectCdTime >= fDisConnectMaxCdTime)
                        {
                            SendDisConnect();
                        }
                        break;
                    }
                case SOCKET_PEER_STATE.DISCONNECTED:
                    break;
                case SOCKET_PEER_STATE.RECONNECTING:
                    {
                        fReConnectServerCdTime += elapsed;
                        if (fReConnectServerCdTime >= Config.fReConnectMaxCdTime)
                        {
                            fReConnectServerCdTime = 0.0;
                            ReConnectServer();
                        }
                        break;
                    }
                default:
                    break;
            }

            if (this.mSocketPeerState != this.mLastSocketPeerState)
            {
                this.mLastSocketPeerState = mSocketPeerState;
                mListenClientPeerStateMgr.OnSocketStateChanged(this);
            }

            mUdpCheckPool.Update(elapsed);
        }

        public void SetSocketState(SOCKET_PEER_STATE mState)
        {
            this.mSocketPeerState = mState;
        }

        public SOCKET_PEER_STATE GetSocketState()
        {
            return mSocketPeerState;
        }

        public void Reset()
        {
            mUdpCheckPool.Reset();
            lock (mWaitCheckPackageQueue)
            {
                while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
                {
                    GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }

            lock (mSendStreamList)
            {
                mSendStreamList.Reset();
            }

            this.Name = string.Empty;
            this.ID = 0;
            this.fConnectCdTime = 0.0;
            this.fDisConnectCdTime = 0.0;
            this.fReConnectServerCdTime = 0.0;
            this.fReceiveHeartBeatTime = 0.0;
            this.fMySendHeartBeatCdTime = 0.0;
        }

        public void Release()
        {
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            DisConnectServer();
            CloseSocket();
            mUdpCheckPool.Release();

            lock (mWaitCheckPackageQueue)
            {
                while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
                {
                    GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }

            lock (mSendStreamList)
            {
                mSendStreamList.Reset();
            }
        }

        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            bool bCanSendPackage = UdpNetCommand.orInnerCommand(mPackage.GetPackageId()) ||
                GetSocketState() == SOCKET_PEER_STATE.CONNECTED;

            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                ResetSendHeartBeatCdTime();

                if (Config.bUdpCheck)
                {
                    mUdpCheckPool.SetRequestOrderId(mPackage);
                    if (UdpNetCommand.orInnerCommand(mPackage.GetPackageId()))
                    {
                        SendNetPackage2(mPackage);
                    }
                    else
                    {
                        UdpStatistical.AddSendCheckPackageCount();
                        mPackage.mTcpStanardRTOTimer.BeginRtt();
                        SendNetPackage2(mPackage);
                    }
                }
                else
                {
                    SendNetPackage2(mPackage);
                }
            }
        }

        public ObjectPoolManager GetObjectPoolManager()
        {
            return mObjectPoolManager;
        }

        public TcpStanardRTOFunc GetTcpStanardRTOFunc()
        {
            return mTcpStanardRTOFunc;
        }

        public CryptoMgr GetCryptoMgr()
        {
            return mCryptoMgr;
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

        private void OnSocketStateChanged()
        {
            mListenClientPeerStateMgr.OnSocketStateChanged(this);
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
