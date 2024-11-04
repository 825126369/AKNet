/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Server
{
    internal class TcpServer : ServerBase
    {
        internal readonly PackageManager mPackageManager = new PackageManager();
        internal readonly TcpNetPackage mNetPackage = new TcpNetPackage();
        private readonly TCPSocket_Server mSocketMgr = null;
        internal readonly ClientPeerManager mClientPeerManager = null;
        internal event Action<ClientPeerBase> mListenSocketStateFunc = null;
        internal ClientPeerPool mClientPeerPool = null;
        internal BufferManager mBufferManager = null;
        internal ReadWriteIOContextPool mReadWriteIOContextPool = null;
        
        public TcpServer()
        {
            NetLog.Init();
            mSocketMgr = new TCPSocket_Server(this);
            mClientPeerManager = new ClientPeerManager(this);
            mBufferManager = new BufferManager(Config.nIOContexBufferLength, 2 * Config.numConnections);
            mReadWriteIOContextPool = new ReadWriteIOContextPool(Config.numConnections * 2, mBufferManager);
            mClientPeerPool = new ClientPeerPool(this, Config.numConnections);
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
