/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Server
{
    internal class TcpServer : ServerBase
    {
        internal readonly PackageManager mPackageManager = null;
        internal readonly TcpNetPackage mNetPackage = null;
        private readonly TCPSocket_Server mSocketMgr = null;
        internal readonly ClientPeerManager mClientPeerManager = null;
        internal event Action<ClientPeerBase> mListenSocketStateFunc = null;
        internal readonly ClientPeerPool mClientPeerPool = null;
        internal readonly BufferManager mBufferManager = null;
        internal readonly SimpleIOContextPool mReadWriteIOContextPool = null;
        internal readonly CryptoMgr mCryptoMgr = null;
        internal readonly Config mConfig = null;

        public TcpServer(TcpConfig mUserConfig = null)
        {
            NetLog.Init();
            if (mUserConfig == null)
            {
                this.mConfig = new Config();
            }
            else
            {
                this.mConfig = new Config(mUserConfig);
            }

            mCryptoMgr = new CryptoMgr(mConfig);
            mPackageManager = new PackageManager();
            mNetPackage = new TcpNetPackage();

            mSocketMgr = new TCPSocket_Server(this);
            mClientPeerManager = new ClientPeerManager(this);

            mBufferManager = new BufferManager(Config.nIOContexBufferLength, 2 * mConfig.MaxPlayerCount);
            mReadWriteIOContextPool = new SimpleIOContextPool(mConfig.MaxPlayerCount * 2, mConfig.MaxPlayerCount * 2);
            mClientPeerPool = new ClientPeerPool(this, 0, mConfig.MaxPlayerCount);
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mSocketMgr.GetServerState();
        }

        public void addNetListenFun(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
           mPackageManager.addNetListenFun(id, func);
        }

        public void InitNet(string Ip, int nPort)
        {
            mSocketMgr.InitNet(Ip, nPort);
        }

        public void removeNetListenFun(ushort id, Action<ClientPeerBase, NetPackage> func)
        {
            mPackageManager.removeNetListenFun(id, func);
        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("XKNet.Tcp.Server 帧 时间 太长: " + elapsed);
            }
            mClientPeerManager.Update(elapsed);
        }

        public void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func)
        {
            mPackageManager.SetNetCommonListenFun(func);
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

        public void Release()
        {
            mSocketMgr.CloseNet();
        }
    }
}
