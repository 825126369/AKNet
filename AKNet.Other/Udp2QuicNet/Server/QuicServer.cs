/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.QuicNet.Common;

namespace AKNet.QuicNet.Server
{
    internal class QuicServer : NetServerInterface
    {
        private readonly QuicListenerMgr mSocketMgr = null;
        internal readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        internal readonly ListenNetPackageMgr mPackageManager = null;
        internal readonly TcpNetPackage mNetPackage = null;
        internal readonly ClientPeerManager mClientPeerManager = null;
        internal event Action<ClientPeerBase> mListenSocketStateFunc = null;
        internal readonly ClientPeerPool mClientPeerPool = null;
        internal readonly BufferManager mBufferManager = null;
        internal readonly SimpleIOContextPool mReadWriteIOContextPool = null;
        internal readonly CryptoMgr mCryptoMgr = null;
        internal readonly Config mConfig = null;

        public QuicServer()
        {
            this.mConfig = new Config();
            mCryptoMgr = new CryptoMgr(mConfig);
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
            mPackageManager = new ListenNetPackageMgr();
            mNetPackage = new TcpNetPackage();

            mSocketMgr = new QuicListenerMgr(this);
            mClientPeerManager = new ClientPeerManager(this);

            mBufferManager = new BufferManager(Config.nIOContexBufferLength, 2 * mConfig.MaxPlayerCount);
            mReadWriteIOContextPool = new SimpleIOContextPool(mConfig.MaxPlayerCount * 2, mConfig.MaxPlayerCount * 2);
            mClientPeerPool = new ClientPeerPool(this, 0, mConfig.MaxPlayerCount);
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
