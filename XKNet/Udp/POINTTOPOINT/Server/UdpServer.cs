/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class UdpServer:ServerBase
	{
        private event Action<ClientPeerBase> mListenSocketStateFunc = null;
        private readonly PackageManager mPackageManager = null;
        private readonly ClientPeerManager mClientPeerManager = null;
        private readonly ObjectPoolManager mObjectPoolManager;
        private readonly SocketUdp_Server mSocketMgr;
        public UdpServer()
        {
            NetLog.Init();
            MainThreadCheck.Check();
            mObjectPoolManager = new ObjectPoolManager();
            mPackageManager = new PackageManager();
            mClientPeerManager = new ClientPeerManager(this);
            mSocketMgr = new SocketUdp_Server(this);
        }

        public void Update(double elapsed)
		{
			if (elapsed >= 0.3)
			{
				NetLog.LogWarning("NetServer 帧 时间 太长: " + elapsed);
			}
            mClientPeerManager.Update (elapsed);
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

        public void InitNet()
        {
            mSocketMgr.InitNet();   
        }

        public void InitNet(int nPort)
        {
            mSocketMgr.InitNet(nPort);
        }

        public void OnSocketStateChanged(ClientPeerBase mClientPeer)
        {
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