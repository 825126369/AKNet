/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp1Tcp.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp1Tcp.Server
{
    internal partial class ServerMgr : NetServerInterface
	{
        private readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = null;
        private readonly ListenNetPackageMgr mPackageManager = null;

        private readonly InnerCommandSendMgr mInnerCommandSendMgr = null;
        internal readonly ClientPeerPool mClientPeerPool = null;
        private readonly FakeSocketMgrInterface mFakeSocketMgr = null;

        private readonly ClientPeerWrapMgr1 mClientPeerMgr1 = null;
        private readonly ClientPeerWrapMgr2 mClientPeerMgr2 = null;
        private readonly ObjectPoolManager mObjectPoolManager;
        internal readonly CryptoMgr mCryptoMgr;

        private int nPort = 0;
        private Socket mSocket = null;
        private readonly SocketAsyncEventArgs ReceiveArgs;
        private readonly object lock_mSocket_object = new object();
        private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;
        private readonly IPEndPoint mEndPointEmpty = new IPEndPoint(IPAddress.IPv6Any, 0);

        public ServerMgr()
        {
            NetLog.Init();
            MainThreadCheck.Check();
            mCryptoMgr = new CryptoMgr();
            mObjectPoolManager = new ObjectPoolManager();
            mPackageManager = new ListenNetPackageMgr();
            mListenClientPeerStateMgr = new ListenClientPeerStateMgr();
            mInnerCommandSendMgr = new InnerCommandSendMgr(this);
            mClientPeerPool = new ClientPeerPool(this, 0, Config.MaxPlayerCount);

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

            mClientPeerMgr1 = new ClientPeerWrapMgr1(this);
            mClientPeerMgr2 = new ClientPeerWrapMgr2(this);
            mSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, int.MaxValue);
            ReceiveArgs = new SocketAsyncEventArgs();
            ReceiveArgs.Completed += ProcessReceive;
            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.RemoteEndPoint = mEndPointEmpty;

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

        public ClientPeerWrapMgr1 GetClientPeerMgr1()
        {
            return mClientPeerMgr1;
        }

        public ClientPeerWrapMgr2 GetClientPeerMgr2()
		{
			return mClientPeerMgr2;
		}

        public ObjectPoolManager GetObjectPoolManager()
        {
            return mObjectPoolManager;
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