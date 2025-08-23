/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2Tcp.Common;
using System;

namespace AKNet.Udp2Tcp.Server
{
    internal class UdpServer:NetServerInterface
	{
        private readonly TcpNetPackage mLikeTcpNetPackage = new TcpNetPackage();

        private readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        private readonly ListenNetPackageMgr mPackageManager = null;

        private readonly InnerCommandSendMgr mInnerCommandSendMgr = null;
        internal readonly ClientPeerPool mClientPeerPool = null;
        private readonly FakeSocketMgr mFakeSocketMgr = null;
        private readonly ClientPeerMgr mClientPeerMgr = null;
        private readonly ObjectPoolManager mObjectPoolManager;
        private readonly SocketUdp_Server mSocketMgr;
        private readonly Config mConfig;
        internal readonly CryptoMgr mCryptoMgr;

        public UdpServer()
        {
            NetLog.Init();
            MainThreadCheck.Check();
            mConfig = new Config();
            mCryptoMgr = new CryptoMgr();
            mSocketMgr = new SocketUdp_Server(this);
            mObjectPoolManager = new ObjectPoolManager();
            mPackageManager = new ListenNetPackageMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
            mInnerCommandSendMgr = new InnerCommandSendMgr(this);
            mFakeSocketMgr = new FakeSocketMgr(this);
            mClientPeerMgr = new ClientPeerMgr(this);
            mClientPeerPool = new ClientPeerPool(this, 0, mConfig.MaxPlayerCount);
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

        public TcpNetPackage GetLikeTcpNetPackage()
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

        public InnerCommandSendMgr GetInnerCommandSendMgr()
        {
            return mInnerCommandSendMgr;
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