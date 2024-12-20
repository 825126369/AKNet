/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;
using System;

namespace AKNet.Udp3Tcp.Server
{
    internal class UdpServer:NetServerInterface
	{
        private readonly LikeTcpNetPackage mLikeTcpNetPackage = new LikeTcpNetPackage();

        private readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        private readonly ListenNetPackageMgr mPackageManager = null;

        private readonly FakeSocketMgr mFakeSocketMgr = null;
        private readonly ClientPeerMgr mClientPeerMgr = null;

        public readonly ClientPeerPool mClientPeerPool = null;
        private readonly ObjectPoolManager mObjectPoolManager;
        private readonly SocketUdp_Server mSocketMgr;
        private readonly Config mConfig;
        internal readonly CryptoMgr mCryptoMgr;

        public UdpServer(Udp3TcpConfig mUserConfig)
        {
            NetLog.Init();
            MainThreadCheck.Check();

            if (mUserConfig == null)
            {
                mConfig = new Config();
            }
            else
            {
                mConfig = new Config(mUserConfig);
            }

            mCryptoMgr = new CryptoMgr(mConfig);
            mSocketMgr = new SocketUdp_Server(this);
            mObjectPoolManager = new ObjectPoolManager();
            mClientPeerPool = new ClientPeerPool(this, 0, GetConfig().MaxPlayerCount);
            mPackageManager = new ListenNetPackageMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
            mFakeSocketMgr = new FakeSocketMgr(this);
            mClientPeerMgr = new ClientPeerMgr(this);
        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("NetServer 帧 时间 太长: " + elapsed);
            }
            mClientPeerMgr.Update(elapsed);
        }

        public Config GetConfig()
        {
            return mConfig;
        }

        public LikeTcpNetPackage GetLikeTcpNetPackage()
        {
            return mLikeTcpNetPackage;
        }

        public CryptoMgr GetCryptoMgr()
        {
            return mCryptoMgr;
        }

        public ListenNetPackageMgr GetPackageManager()
        {
            return mPackageManager;
        }

        public FakeSocketMgr GetFakeSocketMgr()
        {
            return mFakeSocketMgr;
        }

        public ClientPeerMgr GetClientPeerMgr()
        {
            return mClientPeerMgr;
        }

        public ObjectPoolManager GetObjectPoolManager()
        {
            return mObjectPoolManager;
        }

        public ClientPeerPool GetClientPeerPool()
        {
            return mClientPeerPool;
        }

        public SocketUdp_Server GetSocketMgr()
        {
            return mSocketMgr;
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

        public void Release()
        {
            mSocketMgr.Release();
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mSocketMgr.GetServerState();
        }

        public int GetPort()
        {
            return mSocketMgr.GetPort();
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