using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerManager
	{
		private ConcurrentDictionary<string, ClientPeer> mClientDic = null;
		private Queue<ClientPeer> mDisconnectClientPeerList = null;
		private NetServer mNetServer = null;

		public ClientPeerManager(NetServer mNetServer)
		{
			this.mNetServer = mNetServer;
			mClientDic = new ConcurrentDictionary<string, ClientPeer>();
			mDisconnectClientPeerList = new Queue<ClientPeer>();
		}

		public void Update(double elapsed)
		{
			if (mClientDic.Count > 0)
			{
				foreach (var v in mClientDic)
				{
					ClientPeer clientPeer = v.Value;
					clientPeer.Update(elapsed);
					if (clientPeer.GetSocketState() == SOCKET_PEER_STATE.DISCONNECTED)
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

		public ClientPeer FindClient(EndPoint endPoint)
		{
			ClientPeer clientPeer = null;
			string nPeerId = endPoint.ToString();

			if (mClientDic.TryGetValue(nPeerId, out clientPeer))
			{
				return clientPeer;
			}
			else
			{
				return null;
			}
		}

		public ClientPeer FindOrAddClient(EndPoint endPoint)
		{
			ClientPeer clientPeer = null;
			string nPeerId = endPoint.ToString();

			if (mClientDic.TryGetValue(nPeerId, out clientPeer))
			{
				return clientPeer;
			}
			else
			{
				clientPeer = ObjectPoolManager.Instance.mClientPeerPool.Pop();
				if (mClientDic.TryAdd(nPeerId, clientPeer))
				{
					clientPeer.Init(mNetServer);
					clientPeer.BindEndPoint(nPeerId, endPoint);
					int nClientCount = mClientDic.Count;
					NetLog.Log(string.Format("Server: 加入客户端: UUID: {0}   客户端总数: {1}", clientPeer.GetUUID(), nClientCount));
					return clientPeer;
				}

				if (mClientDic.TryGetValue(nPeerId, out clientPeer))
				{
					return clientPeer;
				}
			}

			NetLog.Assert(false);
			return null;
		}

		private void RemoveClient(ClientPeer clientPeer)
		{
			string nPeerId = clientPeer.GetUUID();
			if (mClientDic.TryRemove(nPeerId, out _))
			{
				int nClientCount = mClientDic.Count;
				NetLog.Log(string.Format("Server: 移除客户端: UUID: {0}   客户端总数: {1}", clientPeer.GetUUID(), nClientCount));

				clientPeer.Reset();
				ObjectPoolManager.Instance.mClientPeerPool.recycle(clientPeer);
			}
		}
	}
}