/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:45
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Server
{
    internal class TcpServer : NetServerInterface
    {
        private readonly TCPSocket_Server mSocketMgr = null;

        internal readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        internal readonly ListenNetPackageMgr mPackageManager = null;
        internal readonly TcpNetPackage mNetPackage = null;
        internal readonly ClientPeerManager mClientPeerManager = null;
        internal event Action<ClientPeerBase> mListenSocketStateFunc = null;
        internal readonly ClientPeerPool mClientPeerPool = null;
        internal readonly CryptoMgr mCryptoMgr = null;
        internal readonly Config mConfig = new Config();
        public TcpServer()
        {
            NetLog.Init();

            mCryptoMgr = new CryptoMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
            mPackageManager = new ListenNetPackageMgr();
            mNetPackage = new TcpNetPackage();
            mSocketMgr = new TCPSocket_Server(this);
            mClientPeerManager = new ClientPeerManager(this);
            mClientPeerPool = new ClientPeerPool(this, 0, Config.MaxPlayerCount);
        }

        public Config GetConfig()
        {
            return mConfig;
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mSocketMgr.GetServerState();
        }

        public int GetPort()
        {
            return mSocketMgr.GetPort();
        }

        public void InitNet()
        {
            mSocketMgr.InitNet();
        }

        public void InitNet(int nPort)
        {
            mSocketMgr.InitNet(nPort);
        }

        public void InitNet(string Ip, int nPort)
        {
            mSocketMgr.InitNet(Ip, nPort);
        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("帧 时间 太长: " + elapsed);
            }
            mClientPeerManager.Update(elapsed);
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
            mSocketMgr.CloseNet();
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
