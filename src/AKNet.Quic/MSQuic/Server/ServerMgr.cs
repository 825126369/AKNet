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
using AKNet.MSQuic.Common;

namespace AKNet.MSQuic.Server
{
    internal partial class ServerMgr: QuicServerInterface
    {
        internal readonly QuicListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        internal readonly QuicListenNetPackageMgr mPackageManager = null;
        internal readonly QuicStreamReceivePackage mNetPackage = new QuicStreamReceivePackage();
        internal event Action<QuicClientPeerBase> mListenSocketStateFunc = null;
        internal readonly QuicStreamEncryption mCryptoMgr = new QuicStreamEncryption();

        private readonly List<ClientPeer> mClientList = new List<ClientPeer>(0);
        private readonly Queue<QuicConnection> mConnectSocketQueue = new Queue<QuicConnection>();

        QuicListener mQuicListener = null;
        private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;
        private int nPort;

        public ServerMgr()
        {
            NetLog.Init();
            mListenClientPeerStateMgr = new QuicListenClientPeerStateMgr();
            mPackageManager = new QuicListenNetPackageMgr();
        }

        public void Release()
        {
            CloseNet();
        }

        public void OnSocketStateChanged(QuicClientPeerBase mClientPeer)
        {
            mListenClientPeerStateMgr.OnSocketStateChanged(mClientPeer);
        }



        public void addListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc)
        {
            mListenClientPeerStateMgr.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc)
        {
            mListenClientPeerStateMgr.removeListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mListenClientPeerStateMgr.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mListenClientPeerStateMgr.removeListenClientPeerStateFunc(mFunc);
        }




        public void addNetListenFunc(ushort id, Action<QuicClientPeerBase, QuicNetPackage> mFunc)
        {
            mPackageManager.addNetListenFunc(id, mFunc);
        }

        public void removeNetListenFunc(ushort id, Action<QuicClientPeerBase, QuicNetPackage> mFunc)
        {
            mPackageManager.removeNetListenFunc(id, mFunc);
        }

        public void addNetListenFunc(Action<QuicClientPeerBase, QuicNetPackage> mFunc)
        {
            mPackageManager.addNetListenFunc(mFunc);
        }

        public void removeNetListenFunc(Action<QuicClientPeerBase, QuicNetPackage> mFunc)
        {
            mPackageManager.removeNetListenFunc(mFunc);
        }
    }
}
