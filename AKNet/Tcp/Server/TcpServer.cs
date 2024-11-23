/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:35
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
        internal readonly ReadonlyConfig mConfig = null;

        public TcpServer()
        {
            NetLog.Init();
            mConfig = new ReadonlyConfig();
            mCryptoMgr = new CryptoMgr(mConfig.nECryptoType, mConfig.password1, mConfig.password2);
            mPackageManager = new PackageManager();
            mNetPackage = new TcpNetPackage();

            mSocketMgr = new TCPSocket_Server(this);
            mClientPeerManager = new ClientPeerManager(this);

            mBufferManager = new BufferManager(ReadonlyConfig.nIOContexBufferLength, 2 * mConfig.numConnections);
            mReadWriteIOContextPool = new SimpleIOContextPool(mConfig.numConnections * 2, mConfig.numConnections * 2);
            mClientPeerPool = new ClientPeerPool(this, mConfig.numConnections, mConfig.numConnections);
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
