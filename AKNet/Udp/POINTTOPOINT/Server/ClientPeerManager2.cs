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
using System.Collections.Generic;
using System.Net;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerManager2
	{
        public readonly ClientPeerPool mClientPeerPool = null;
        private readonly Dictionary<string, ClientPeer> mClientDic = new Dictionary<string, ClientPeer>();
        private readonly List<string> mRemovePeerList = new List<string>();
        private readonly Queue<NetUdpFixedSizePackage> mPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private readonly DisConnectSendMgr mDisConnectSendMgr = null;
        private UdpServer mNetServer = null;

        private readonly Queue<FakeSocket> mConnectSocketQueue = new Queue<FakeSocket>();
        private readonly List<ClientPeer> mClientList = new List<ClientPeer>();

        public ClientPeerManager2(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            mClientPeerPool = new ClientPeerPool(mNetServer, 0, mNetServer.GetConfig().MaxPlayerCount);
            mDisConnectSendMgr = new DisConnectSendMgr(mNetServer);
        }

        public void Update(double elapsed)
        {
            while (CreateClientPeer())
            {

            }

            for (int i = mClientList.Count - 1; i >= 0; i--)
            {
                ClientPeer mClientPeer = mClientList[i];
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    mClientPeer.Update(elapsed);
                }
                else
                {
                    mClientList.RemoveAt(i);
                    PrintRemoveClientMsg(mClientPeer);
                    mClientPeerPool.recycle(mClientPeer);
                }
            }
        }

        public void MultiThreadingHandleConnectedSocket(FakeSocket mSocket)
        {
            lock (mConnectSocketQueue)
            {
                mConnectSocketQueue.Enqueue(mSocket);
            }
        }

        private bool CreateClientPeer()
        {
            MainThreadCheck.Check();

            FakeSocket mSocket = null;
            lock (mConnectSocketQueue)
            {
                mConnectSocketQueue.TryDequeue(out mSocket);
            }
            if (mSocket != null)
            {
                ClientPeer clientPeer = mClientPeerPool.Pop();
                clientPeer.HandleConnectedSocket(mSocket);
                mClientList.Add(clientPeer);
                PrintAddClientMsg(clientPeer);
                return true;
            }
            return false;
        }

        private void PrintAddClientMsg(ClientPeer clientPeer)
        {
#if DEBUG
            var mRemoteEndPoint = clientPeer.GetIPEndPoint();
            if (mRemoteEndPoint != null)
            {
                NetLog.Log($"增加客户端: {mRemoteEndPoint}, 客户端总数: {mClientList.Count}");
            }
            else
            {
                NetLog.Log($"增加客户端, 客户端总数: {mClientList.Count}");
            }
#endif
        }

        private void PrintRemoveClientMsg(ClientPeer clientPeer)
        {
#if DEBUG
            var mRemoteEndPoint = clientPeer.GetIPEndPoint();
            if (mRemoteEndPoint != null)
            {
                NetLog.Log($"移除客户端: {mRemoteEndPoint}, 客户端总数: {mClientList.Count}");
            }
            else
            {
                NetLog.Log($"移除客户端, 客户端总数: {mClientList.Count}");
            }
#endif
        }

    }
}