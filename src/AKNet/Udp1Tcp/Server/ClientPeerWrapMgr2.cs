/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Generic;

namespace AKNet.Udp1Tcp.Server
{
    internal class ClientPeerWrapMgr2
	{
        private NetServerMain mNetServer = null;
        private readonly Queue<FakeSocket> mConnectSocketQueue = new Queue<FakeSocket>();
        private readonly List<ClientPeerWrap> mClientList = new List<ClientPeerWrap>();

        public ClientPeerWrapMgr2(NetServerMain mNetServer)
        {
            this.mNetServer = mNetServer;
        }

        public void Update(double elapsed)
        {
            while (CreateClientPeer())
            {

            }

            for (int i = mClientList.Count - 1; i >= 0; i--)
            {
                ClientPeerWrap mClientPeer = mClientList[i];
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    mClientPeer.Update(elapsed);
                }
                else
                {
                    mClientList.RemoveAt(i);
                    PrintRemoveClientMsg(mClientPeer);
                    mClientPeer.Reset();
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
                ClientPeerWrap clientPeer = new ClientPeerWrap(mNetServer);
                clientPeer.HandleConnectedSocket(mSocket);
                mClientList.Add(clientPeer);
                PrintAddClientMsg(clientPeer);
                return true;
            }
            return false;
        }

        private void PrintAddClientMsg(ClientPeerWrap clientPeer)
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

        private void PrintRemoveClientMsg(ClientPeerWrap clientPeer)
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