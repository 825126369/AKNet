/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Generic;

namespace AKNet.Udp3Tcp.Server
{
    internal class ClientPeerMgr
	{
        private UdpServer mNetServer = null;
        private readonly Queue<FakeSocket> mConnectSocketQueue = new Queue<FakeSocket>();
        private readonly List<ClientPeerPrivate> mClientList = new List<ClientPeerPrivate>();
        
        public ClientPeerMgr(UdpServer mNetServer)
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
                ClientPeerPrivate mClientPeer = mClientList[i];
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    mClientPeer.Update(elapsed);
                }
                else
                {
                    mClientList.RemoveAt(i);
                    mClientPeer.CloseSocket();
                    PrintRemoveClientMsg(mClientPeer);
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
                ClientPeerPrivate clientPeer = new ClientPeerPrivate(mNetServer);
                clientPeer.HandleConnectedSocket(mSocket);
                mClientList.Add(clientPeer);
                PrintAddClientMsg(clientPeer);
                return true;
            }
            return false;
        }

        private void PrintAddClientMsg(ClientPeerPrivate clientPeer)
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

        private void PrintRemoveClientMsg(ClientPeerPrivate clientPeer)
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