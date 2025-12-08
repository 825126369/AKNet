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
using AKNet.Udp3Tcp.Common;
using System;
using System.Net;

namespace AKNet.Udp3Tcp.Client
{
    internal partial class ClientPeer : UdpClientPeerCommonBase, NetClientInterface, ClientPeerBase
    {
        internal readonly ListenNetPackageMgr mPackageManager = new ListenNetPackageMgr();
        internal readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = new ListenClientPeerStateMgr();

        internal readonly UdpCheckMgr mUdpCheckPool = null;
        internal readonly CryptoMgr mCryptoMgr = new CryptoMgr();

        private readonly ObjectPoolManager mObjectPoolManager = new ObjectPoolManager();
        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private SOCKET_PEER_STATE mLastSocketPeerState = SOCKET_PEER_STATE.NONE;
        private string Name = string.Empty;
        private uint ID = 0;

        private const double fConnectMaxCdTime = 2.0;
        private const double fDisConnectMaxCdTime = 2.0;
        private double fDisConnectCdTime = 0.0;
        private double fConnectCdTime = 0.0;
        private double fReceiveHeartBeatTime = 0.0;
        private double fMySendHeartBeatCdTime = 0.0;
        private double fReConnectServerCdTime = 0.0;

        public ClientPeer()
        {
            MainThreadCheck.Check();
            mUdpCheckPool = new UdpCheckMgr(this);
        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("NetClient 帧 时间 太长: " + elapsed);
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
            this.Name = string.Empty;
            this.ID = 0;
        }

        public void Release()
        {
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            mUdpCheckPool.Release();
        }

        public void SendNetPackage(NetUdpSendFixedSizePackage mPackage)
        {
            bool bCanSendPackage = mPackage.orInnerCommandPackage() || GetSocketState() == SOCKET_PEER_STATE.CONNECTED;
            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                ResetSendHeartBeatCdTime();

                mUdpCheckPool.SetRequestOrderId(mPackage);
                if (mPackage.orInnerCommandPackage())
                {
                    SendNetPackage(mPackage);
                }
                else
                {
                    UdpStatistical.AddSendCheckPackageCount();
                    SendNetPackage(mPackage);
                }
            }
        }

        public ObjectPoolManager GetObjectPoolManager()
        {
            return mObjectPoolManager;
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
