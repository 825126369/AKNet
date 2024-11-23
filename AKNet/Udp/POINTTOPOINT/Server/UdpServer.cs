/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class UdpServer:ServerBase
	{
        private event Action<ClientPeerBase> mListenSocketStateFunc = null;
        private readonly PackageManager mPackageManager = null;
        private readonly ClientPeerManager mClientPeerManager = null;
        private readonly ObjectPoolManager mObjectPoolManager;
        private readonly SocketUdp_Server mSocketMgr;
        private readonly Config mConfig;
        internal readonly CryptoMgr mCryptoMgr;

        public UdpServer(UdpConfig mUserConfig)
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
            mPackageManager = new PackageManager();
            mClientPeerManager = new ClientPeerManager(this);
        }

        public void Update(double elapsed)
		{
			if (elapsed >= 0.3)
			{
				NetLog.LogWarning("NetServer 帧 时间 太长: " + elapsed);
			}
            mClientPeerManager.Update (elapsed);
		}

        public Config GetConfig()
        {
            return mConfig;
        }

        public CryptoMgr GetCryptoMgr()
        {
            return mCryptoMgr;
        }

        public PackageManager GetPackageManager()
		{
			return mPackageManager;
		}

		public ClientPeerManager GetClientPeerManager ()
		{
			return mClientPeerManager;
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

        public void addNetListenFun(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
			mPackageManager.addNetListenFun(id, func);
        }

        public void removeNetListenFun(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
           mPackageManager.removeNetListenFun(id, func);
        }

        public void Release()
        {
            mSocketMgr.Release();
        }

        public void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func)
        {
            mPackageManager.SetNetCommonListenFun(func);
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mSocketMgr.GetServerState();
        }

        public int GetPort()
        {
            return mSocketMgr.GetPort();
        }

        public void OnSocketStateChanged(ClientPeerBase mClientPeer)
        {
            MainThreadCheck.Check();
            this.mListenSocketStateFunc?.Invoke(mClientPeer);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mListenSocketStateFunc += mFunc;
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mListenSocketStateFunc -= mFunc;
        }
    }

}