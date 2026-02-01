/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:51
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp3Tcp.Server
{
    internal partial class ServerMgr : NetServerInterface
	{
        private readonly NetStreamReceivePackage mLikeTcpNetPackage = new NetStreamReceivePackage();
        private readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        private readonly ListenNetPackageMgr mPackageManager = null;
        private readonly ClientPeerPool mClientPeerPool = null;
        private readonly ObjectPoolManager mObjectPoolManager;
        private readonly CryptoMgr mCryptoMgr;

        private int nPort = 0;
        private Socket mSocket = null;
        private readonly SocketAsyncEventArgs ReceiveArgs;
        private readonly object lock_mSocket_object = new object();
        private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;
        private readonly IPEndPoint mEndPointEmpty = new IPEndPoint(IPAddress.Any, 0);

        private readonly Dictionary<IPEndPoint, FakeSocket> mAcceptSocketDic = null;
        private readonly FakeSocketPool mFakeSocketPool = null;

        private readonly Queue<FakeSocket> mConnectSocketQueue = new Queue<FakeSocket>();
        private readonly List<ClientPeerWrap> mClientList = new List<ClientPeerWrap>();

        public ServerMgr()
        {
            MainThreadCheck.Check();

            mCryptoMgr = new CryptoMgr();
            mObjectPoolManager = new ObjectPoolManager();
            mPackageManager = new ListenNetPackageMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
            mClientPeerPool = new ClientPeerPool(this, 0, Config.MaxPlayerCount);

            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, int.MaxValue);
            ReceiveArgs = new SocketAsyncEventArgs();
            ReceiveArgs.Completed += ProcessReceive;
            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.RemoteEndPoint = mEndPointEmpty;
            
            mFakeSocketPool = new FakeSocketPool(this, 0, Config.MaxPlayerCount);
            mAcceptSocketDic = new Dictionary<IPEndPoint, FakeSocket>(Config.MaxPlayerCount);
        }

        public NetStreamReceivePackage GetLikeTcpNetPackage()
        {
            return mLikeTcpNetPackage;
        }

        public CryptoMgr GetCryptoMgr()
        {
            return mCryptoMgr;
        }

        public ClientPeerPool GetClientPeerPool()
        {
            return mClientPeerPool;
        }

        public ListenNetPackageMgr GetPackageManager()
        {
            return mPackageManager;
        }

        public ObjectPoolManager GetObjectPoolManager()
        {
            return mObjectPoolManager;
        }

        public void Release()
        {
            CloseSocket();
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