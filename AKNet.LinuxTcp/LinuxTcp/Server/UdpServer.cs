/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:51
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.LinuxTcp.Common;
using System;

namespace AKNet.LinuxTcp.Server
{
    internal class UdpServer:NetServerInterface
	{
        private readonly TcpNetPackage mLikeTcpNetPackage = new TcpNetPackage();

        private readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        private readonly ListenNetPackageMgr mPackageManager = null;
        internal readonly ClientPeerPool mClientPeerPool = null;
        private readonly FakeSocketMgr mFakeSocketMgr = null;
        private readonly ClientPeerMgr mClientPeerMgr = null;
        
        private readonly SocketUdp_Server mSocketMgr;
        private readonly Config mConfig = new Config();
        internal readonly CryptoMgr mCryptoMgr;
        public UdpServer()
        {
            NetLog.Init();
            MainThreadCheck.Check();
            IPAddressHelper.GetMtu();

            mCryptoMgr = new CryptoMgr();
            mSocketMgr = new SocketUdp_Server(this);
            mFakeSocketMgr = new FakeSocketMgr(this);
            mClientPeerMgr = new ClientPeerMgr(this);
            mClientPeerPool = new ClientPeerPool(this, 0, mConfig.MaxPlayerCount);
            mPackageManager = new ListenNetPackageMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
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