/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp5Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace AKNet.Udp5Tcp.Server
{
    internal partial class NetServerMain : NetServerInterface
	{
        private readonly NetStreamReceivePackage mNetStreamPackage = new NetStreamReceivePackage();
        private readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        private readonly ListenNetPackageMgr mPackageManager = null;
        private readonly ClientPeerPool mClientPeerPool = null;
        private readonly CryptoMgr mCryptoMgr;

        private int nPort = 0;
        private Listener mListener = null;
        private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;
        private readonly Queue<Connection> mConnectSocketQueue = new Queue<Connection>();
        private readonly List<ClientPeerWrap> mClientList = new List<ClientPeerWrap>();
        private readonly ConfigInstance mConfigInstance;
        public NetServerMain(ConfigInstance mConfig = null)
        {
            MainThreadCheck.Check();
            this.mConfigInstance = mConfig ?? new ConfigInstance();

            mCryptoMgr = new CryptoMgr();
            mPackageManager = new ListenNetPackageMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
            mClientPeerPool = new ClientPeerPool(this, 0, Config.MaxPlayerCount);
            mListener = new Listener();
        }

        public NetStreamReceivePackage GetNetStreamPackage()
        {
            return mNetStreamPackage;
        }

        public CryptoMgr GetCryptoMgr()
        {
            return mCryptoMgr;
        }

        public ClientPeerPool GetClientPeerPool()
        {
            return mClientPeerPool;
        }

        public ListenNetPackageMgr GetPackageManager()
        {
            return mPackageManager;
        }

        public void Release()
        {
            CloseSocket();
        }

        public void addNetListenFunc(ushort id, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mPackageManager.addNetListenFunc(id, mFunc);
        }

        public void removeNetListenFunc(ushort id, Action<ClientPeerBase, NetPackage> mFunc)
        {
            mPackageManager.removeNetListenFunc(id, mFunc);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mPackageManager.addNetListenFunc(mFunc);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mPackageManager.removeNetListenFunc(mFunc);
        }

        public void OnSocketStateChanged(ClientPeerBase mClientPeer)
        {
            mListenClientPeerStateMgr.OnSocketStateChanged(mClientPeer);
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
    }

}