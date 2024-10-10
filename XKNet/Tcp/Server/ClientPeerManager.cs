using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;

namespace XKNet.Tcp.Server
{
    internal class ClientPeerManager
	{
		private Dictionary<uint, ClientPeer> mClientDic = null;

		private Queue<ClientPeer> mConnectClientPeerList = null;
		private Queue<ClientPeer> mDisconnectClientPeerList = null;

		public ClientPeerManager()
		{
			mClientDic = new Dictionary<uint, ClientPeer>();

			mConnectClientPeerList = new Queue<ClientPeer>();
			mDisconnectClientPeerList = new Queue<ClientPeer>();
		}

		public void Update(double elapsed)
		{
			lock (mConnectClientPeerList)
			{
				while (mConnectClientPeerList.Count > 0)
				{
					ClientPeer clientPeer = mConnectClientPeerList.Dequeue();
					mClientDic.Add(clientPeer.GetUUID(), clientPeer);
#if DEBUG
					NetLog.Assert(clientPeer.GetUUID() > 0);
#endif
				}
			}

			if (mClientDic.Count > 0)
			{
				foreach (var iter in mClientDic)
				{
					ClientPeer clientPeer = iter.Value;
					SERVER_SOCKET_PEER_STATE mSocketPeerState = clientPeer.GetSocketState();
					if (mSocketPeerState == SERVER_SOCKET_PEER_STATE.CONNECTED)
					{
						clientPeer.Update(elapsed);
					}
					else
					{
						mDisconnectClientPeerList.Enqueue(clientPeer);
					}
				}

				while (mDisconnectClientPeerList.Count > 0)
				{
					ClientPeer clientPeer = mDisconnectClientPeerList.Dequeue();
					RemoveClient(clientPeer);
				}
			}
		}

		public bool AddClient(Socket mSocket)
		{
			ClientPeer clientPeer = ServerGlobalVariable.Instance.mClientPeerPool.Pop();
			if (clientPeer != null)
			{
				clientPeer.ConnectClient(mSocket);

				lock (mConnectClientPeerList)
				{
					mConnectClientPeerList.Enqueue(clientPeer);
				}
#if DEBUG
				IPEndPoint mRemoteEndPoint = clientPeer.GetIPEndPoint();
				int nClientCount = mConnectClientPeerList.Count + mClientDic.Count;
				NetLog.Log(string.Format("加入客户端: {0}:{1},  UUID: {2}   客户端总数: {3}", mRemoteEndPoint.Address.ToString(), mRemoteEndPoint.Port, clientPeer.GetUUID(), nClientCount));
#endif
				return true;
			}

			return false;
		}

		private void RemoveClient(ClientPeer clientPeer)
		{
			uint nId = clientPeer.GetUUID();
			mClientDic.Remove(nId);
#if DEBUG
			NetLog.Assert(nId > 0);

			IPEndPoint mRemoteEndPoint = clientPeer.GetIPEndPoint();
			int nClientCount = mConnectClientPeerList.Count + mClientDic.Count;
			NetLog.Log(string.Format("移除客户端: {0}:{1},  UUID: {2} 客户端总数: {3}", mRemoteEndPoint.Address.ToString(), mRemoteEndPoint.Port, clientPeer.GetUUID(), nClientCount));
#endif
			clientPeer.Reset();
			ServerGlobalVariable.Instance.mClientPeerPool.recycle(clientPeer);
		}
	}

}