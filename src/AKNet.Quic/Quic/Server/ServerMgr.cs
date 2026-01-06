/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using AKNet.Quic.Common;
using System.Net.Quic;

namespace AKNet.Quic.Server
{
    internal partial class ServerMgr : NetServerInterface
    {
        internal readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        internal readonly ListenNetPackageMgr mPackageManager = null;
        internal readonly NetStreamReceivePackage mNetPackage = new NetStreamReceivePackage();
        internal event Action<ClientPeerBase> mListenSocketStateFunc = null;
        internal readonly ClientPeerPool mClientPeerPool;
        internal readonly CryptoMgr mCryptoMgr = null;

        private readonly List<ClientPeerWrap> mClientList = new List<ClientPeerWrap>(0);
        private readonly Queue<QuicConnection> mConnectSocketQueue = new Queue<QuicConnection>();

        QuicListener mQuicListener = null;
        private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;
        private int nPort;

        public ServerMgr()
        {
            NetLog.Init();
            mCryptoMgr = new CryptoMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
            mPackageManager = new ListenNetPackageMgr();
            mClientPeerPool = new ClientPeerPool(this, 0, Config.MaxPlayerCount);
        }

        public void Release()
        {
            CloseNet();
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
