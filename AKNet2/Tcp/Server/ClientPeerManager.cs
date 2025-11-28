/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:45
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Tcp.Common;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    internal class ClientPeerManager
	{
		private readonly List<ClientPeer> mClientList = new List<ClientPeer>(0);
		private readonly Queue<Socket> mConnectSocketQueue = new Queue<Socket>();
		private TcpServer mNetServer;

		public ClientPeerManager(TcpServer mNetServer)
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
				var mClientPeer = mClientList[i];
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

		public bool MultiThreadingHandleConnectedSocket(Socket mSocket)
		{
			int nNowConnectCount = mClientList.Count + mConnectSocketQueue.Count;
			if (nNowConnectCount >= Config.MaxPlayerCount)
			{
#if DEBUG
				NetLog.Log($"服务器爆满, 客户端总数: {nNowConnectCount}");
#endif
				return false;
			}
			else
			{
				lock (mConnectSocketQueue)
				{
					mConnectSocketQueue.Enqueue(mSocket);
				}
				return true;
			}
		}

		private bool CreateClientPeer()
		{
			Socket mSocket = null;
			lock (mConnectSocketQueue)
			{
				mConnectSocketQueue.TryDequeue(out mSocket);
			}
			if (mSocket != null)
			{
				var clientPeer = new ClientPeer(mNetServer);
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