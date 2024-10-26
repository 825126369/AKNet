using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using XKNet.Common;

namespace XKNet.Tcp.Server
{
    internal class ClientPeerManager
	{
		private readonly List<ClientPeer> mClientList = new List<ClientPeer>(1024);
		private readonly ConcurrentStack<ClientPeer> mConnectClientPeerList = new ConcurrentStack<ClientPeer>();
		private TcpServer mNetServer;

		public ClientPeerManager(TcpServer mNetServer)
		{
			this.mNetServer = mNetServer;
		}

		public void Update(double elapsed)
		{
			ClientPeer clientPeer = null;
			while (mConnectClientPeerList.TryPop(out clientPeer))
			{
				mClientList.Add(clientPeer);
				AddClientMsg(clientPeer);
			}

			for (int i = mClientList.Count - 1; i >= 0; i--)
			{
				ClientPeer mClientPeer = mClientList[i];
				if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
				{
					clientPeer.Update(elapsed);
				}
				else
				{
					mClientList.RemoveAt(i);
					RemoveClientMsg(mClientPeer);
					clientPeer.Reset();
					mNetServer.mClientPeerPool.recycle(clientPeer);
				}
			}
		}

		public bool HandleConnectedSocket(Socket mSocket)
		{
			ClientPeer clientPeer = mNetServer.mClientPeerPool.Pop();
			if (clientPeer != null)
			{
				clientPeer.HandleConnectedSocket(mSocket);
				mConnectClientPeerList.Push(clientPeer);
				return true;
			}
			return false;
		}

		private void AddClientMsg(ClientPeer clientPeer)
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

        private void RemoveClientMsg(ClientPeer clientPeer)
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