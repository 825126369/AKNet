/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:47
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    internal partial class ServerMgr : NetServerInterface
    {
        internal readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
        internal readonly ListenNetPackageMgr mPackageManager = new ListenNetPackageMgr();
        internal readonly NetStreamReceivePackage mNetPackage = new NetStreamReceivePackage();
        internal readonly CryptoMgr mCryptoMgr = new CryptoMgr();
        
        internal readonly ClientPeerPool mClientPeerPool = null;
        private readonly List<ClientPeerWrap> mClientList = new List<ClientPeerWrap>(0);
        private readonly Queue<Socket> mConnectSocketQueue = new Queue<Socket>();

        private int nPort;
        private Socket mListenSocket = null;
        private readonly SocketAsyncEventArgs mAcceptIOContex = new SocketAsyncEventArgs();
        private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;

        public ServerMgr()
        {
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
