/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:46
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class UdpServer:NetServerInterface
	{
        private event Action<ClientPeerBase> mListenSocketStateFunc = null;

        private readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        private readonly ListenNetPackageMgr mPackageManager = null;

        private readonly InnerCommandSendMgr mInnerCommandSendMgr = null;
        internal readonly ClientPeerPool mClientPeerPool = null;
        private readonly FakeSocketMgrInterface mFakeSocketMgr = null;

        private readonly ClientPeerMgr1 mClientPeerMgr1 = null;
        private readonly ClientPeerMgr2 mClientPeerMgr2 = null;
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
            mClientPeerPool = new ClientPeerPool(this, 0, mConfig.MaxPlayerCount);

            if (Config.nUseFakeSocketMgrType == 1)
            {
                mFakeSocketMgr = new FakeSocketMgr(this);
            }
            else if (Config.nUseFakeSocketMgrType == 2)
            {
                mFakeSocketMgr = new FakeSocketMgr2(this);
            }
            else if (Config.nUseFakeSocketMgrType == 3)
            {
                mFakeSocketMgr = new FakeSocketMgr3(this);
            }
            else if (Config.nUseFakeSocketMgrType == 4)
            {
                mFakeSocketMgr = new FakeSocketMgr4(this);
            }
            else
            {
                NetLog.Assert(false, Config.nUseFakeSocketMgrType);
            }

            mClientPeerMgr1 = new ClientPeerMgr1(this);
            mClientPeerMgr2 = new ClientPeerMgr2(this);
        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("NetServer 帧 时间 太长: " + elapsed);
            }
            mClientPeerMgr1.Update(elapsed);
            mClientPeerMgr2.Update(elapsed);
        }

        public Config GetConfig()
        {
            return mConfig;
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

        public FakeSocketMgrInterface GetFakeSocketMgr()
        {
            return mFakeSocketMgr;
        }

        public ClientPeerMgr1 GetClientPeerMgr1()
        {
            return mClientPeerMgr1;
        }

        public ClientPeerMgr2 GetClientPeerMgr2()
		{
			return mClientPeerMgr2;
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