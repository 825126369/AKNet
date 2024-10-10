using System.Collections.Generic;
using System.Net;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerManager
	{
		private Dictionary<string, ClientPeer> mClientDic = null;
		private Queue<ClientPeer> mDisconnectClientPeerList = null;
		private NetServer mNetServer = null;

		public ClientPeerManager(NetServer mNetServer)
		{
			this.mNetServer = mNetServer;
			mClientDic = new Dictionary<string, ClientPeer>();
			mDisconnectClientPeerList = new Queue<ClientPeer>();
		}

		public void Update(double elapsed)
		{
			lock (mClientDic)
			{
				if (mClientDic.Count > 0)
				{
					foreach (var v in mClientDic)
					{
						ClientPeer clientPeer = v.Value;
						clientPeer.Update(elapsed);
						if (clientPeer.GetSocketState() == SERVER_SOCKET_PEER_STATE.DISCONNECTED)
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
		}

		public ClientPeer FindOrAddClient(EndPoint endPoint)
		{
			ClientPeer clientPeer = null;
			string nPeerId = endPoint.ToString();

			lock (mClientDic)
			{
				if (mClientDic.TryGetValue(nPeerId, out clientPeer))
				{
					return clientPeer;
				}
				else
				{
					clientPeer = ObjectPoolManager.Instance.mClientPeerPool.Pop();
                    clientPeer.Init(mNetServer);
                    
					clientPeer.BindEndPoint(nPeerId, endPoint);
					mClientDic.Add(nPeerId, clientPeer);

					int nClientCount = mClientDic.Count;
					NetLog.Log(string.Format("Server: 加入客户端: UUID: {0}   客户端总数: {1}", clientPeer.GetUUID(), nClientCount));

					return clientPeer;
				}
			}
		}

		private void RemoveClient(ClientPeer clientPeer)
		{
			string nPeerId = clientPeer.GetUUID();
			mClientDic.Remove(nPeerId);

			int nClientCount = mClientDic.Count;
			NetLog.Log(string.Format("Server: 移除客户端: UUID: {0}   客户端总数: {1}", clientPeer.GetUUID(), nClientCount));

			clientPeer.Reset();
			ObjectPoolManager.Instance.mClientPeerPool.recycle(clientPeer);
		}
	}
}