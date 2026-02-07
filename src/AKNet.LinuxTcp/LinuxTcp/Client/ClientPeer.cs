/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.LinuxTcp.Common;
using System;
using System.Net;

namespace AKNet.LinuxTcp.Client
{
    internal class ClientPeer : UdpClientPeerCommonBase, NetClientInterface, ClientPeerBase
    {
        internal readonly ListenNetPackageMgr mPackageManager = new ListenNetPackageMgr();
        internal readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = new ListenClientPeerStateMgr();

        internal readonly MsgSendMgr mMsgSendMgr;
        internal readonly MsgReceiveMgr mMsgReceiveMgr;
        internal readonly SocketUdp mSocketMgr;
        internal readonly UDPLikeTCPMgr mUDPLikeTCPMgr = null;
        internal readonly UdpCheckMgr mUdpCheckPool;
        internal readonly CryptoMgr mCryptoMgr;
        private readonly ObjectPoolManager mObjectPoolManager;
        private SOCKET_PEER_STATE mSocketPeerState;
        private SOCKET_PEER_STATE mLastSocketPeerState;
        private string Name = string.Empty;
        private uint ID = 0;

        public ClientPeer()
        {
            NetLog.Init();
            MainThreadCheck.Check();
            IPAddressHelper.GetMtu();

            mSocketPeerState = mLastSocketPeerState = SOCKET_PEER_STATE.DISCONNECTED;

            mCryptoMgr = new CryptoMgr();
            mObjectPoolManager = new ObjectPoolManager();
            mMsgSendMgr = new MsgSendMgr(this);
            mMsgReceiveMgr = new MsgReceiveMgr(this);
            mSocketMgr = new SocketUdp(this);
            mUDPLikeTCPMgr = new UDPLikeTCPMgr(this);
            mUdpCheckPool = new UdpCheckMgr(this);
        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("NetClient 帧 时间 太长: " + elapsed);
            }

            mMsgReceiveMgr.Update(elapsed);
            mUDPLikeTCPMgr.Update(elapsed);
            mUdpCheckPool.Update(elapsed);

            if (this.mSocketPeerState != this.mLastSocketPeerState)
            {
                this.mLastSocketPeerState = mSocketPeerState;
                mListenClientPeerStateMgr.OnSocketStateChanged(this);
            }
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
            mSocketMgr.Reset();
            mMsgReceiveMgr.Reset();
            mUdpCheckPool.Reset();
            this.Name = string.Empty;
            this.ID = 0;
        }

        public void Release()
        {
            mSocketMgr.Release();
            mMsgReceiveMgr.Release();
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

        public void ConnectServer(string Ip, int nPort)
        {
            mSocketMgr.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return mSocketMgr.DisConnectServer();
        }

        public void ReConnectServer()
        {
            mSocketMgr.ReConnectServer();
        }

        public void SendInnerNetData(byte id)
        {
            mMsgSendMgr.SendInnerNetData(id);
        }

        public void SendNetData(ushort nPackageId)
        {
            mMsgSendMgr.SendNetData(nPackageId);
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            mMsgSendMgr.SendNetData(nPackageId, data);
        }

        public void SendNetPackage(sk_buff skb)
        {
            mUDPLikeTCPMgr.ResetSendHeartBeatCdTime();
            this.mSocketMgr.SendNetPackage(skb.GetSendBuffer());
        }

        public IPEndPoint GetIPEndPoint()
        {
            return mSocketMgr.GetIPEndPoint();
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            mMsgSendMgr.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            mMsgSendMgr.SendNetData(nPackageId, buffer);
        }

        public void ResetSendHeartBeatCdTime()
        {
            this.mUDPLikeTCPMgr.ResetSendHeartBeatCdTime();
        }

        public void ReceiveHeartBeat()
        {
            this.mUDPLikeTCPMgr.ReceiveHeartBeat();
        }

        public void ReceiveConnect(sk_buff skb)
        {
            this.mUDPLikeTCPMgr.ReceiveConnect(skb);
        }

        public void ReceiveDisConnect()
        {
            this.mUDPLikeTCPMgr.ReceiveDisConnect();
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
