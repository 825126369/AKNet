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
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    internal partial class ServerMgr : NetServerInterface
    {
        internal readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        internal readonly ListenNetPackageMgr mPackageManager = null;
        internal readonly NetStreamPackage mNetPackage = null;
        internal event Action<ClientPeerBase> mListenSocketStateFunc = null;
        internal readonly ClientPeerPool mClientPeerPool = null;
        internal readonly CryptoMgr mCryptoMgr = null;

        private int nPort;
        private Socket mListenSocket = null;
        private readonly object lock_mSocket_object = new object();
        private readonly SocketAsyncEventArgs mAcceptIOContex = new SocketAsyncEventArgs();
        private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;

        public ServerMgr()
        {
            NetLog.Init();

            mCryptoMgr = new CryptoMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
            mPackageManager = new ListenNetPackageMgr();
            mNetPackage = new NetStreamPackage();
            mClientPeerPool = new ClientPeerPool(this, 0, Config.MaxPlayerCount);

            mAcceptIOContex.Completed += OnIOCompleted;
            mAcceptIOContex.AcceptSocket = null;
        }

        public void OnSocketStateChanged(ClientPeerBase mClientPeer)
        {
            mListenClientPeerStateMgr.OnSocketStateChanged(mClientPeer);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mListenClientPeerStateMgr.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mListenClientPeerStateMgr.removeListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mListenClientPeerStateMgr.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mListenClientPeerStateMgr.removeListenClientPeerStateFunc(mFunc);
        }

        public void Release()
        {
            CloseNet();
        }

        public void addNetListenFunc(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mPackageManager.addNetListenFunc(id, func);
        }

        public void removeNetListenFunc(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mPackageManager.removeNetListenFunc(id, func);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> func)
        {
            mPackageManager.addNetListenFunc(func);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> func)
        {
            mPackageManager.removeNetListenFunc(func);
        }
    }
}
