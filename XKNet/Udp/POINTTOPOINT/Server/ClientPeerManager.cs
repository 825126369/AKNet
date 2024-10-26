using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerManager
	{
        public readonly ClientPeerPool mClientPeerPool = null;
        private readonly Dictionary<string, ClientPeer> mClientDic = new Dictionary<string, ClientPeer>();
        private readonly List<string> mRemovePeerList = new List<string>();
        private readonly ConcurrentStack<string> mSocketExceptionList = new ConcurrentStack<string>();
        private readonly ConcurrentStack<NetUdpFixedSizePackage> mPackageQueue = new ConcurrentStack<NetUdpFixedSizePackage>();
        private UdpServer mNetServer = null;

		public ClientPeerManager(UdpServer mNetServer)
		{
			this.mNetServer = mNetServer;
            mClientPeerPool = new ClientPeerPool(mNetServer, Config.numConnections);
        }

		public void Update(double elapsed)
		{
			string nPeerId = string.Empty;
			while (mSocketExceptionList.TryPop(out nPeerId))
			{
				ClientPeer mClientPeer = null;
				if (mClientDic.TryGetValue(nPeerId, out mClientPeer))
				{
					mClientDic.Remove(nPeerId);
					RemoveClientMsg(mClientPeer);
					mClientPeer.Reset();
					mClientPeerPool.recycle(mClientPeer);
				}
			}

			NetUdpFixedSizePackage mPackage = null;
			while (mPackageQueue.TryPop(out mPackage))
			{
				mPackage = new NetUdpFixedSizePackage();
				AddClient_And_ReceiveNetPackage(mPackage);
			}
			
			foreach (var v in mClientDic)
			{
				ClientPeer clientPeer = v.Value;
				clientPeer.Update(elapsed);
				if (clientPeer.GetSocketState() == SOCKET_PEER_STATE.DISCONNECTED)
				{
                    mRemovePeerList.Add(v.Key);
				}
			}

			foreach(var v in mRemovePeerList)
			{
				ClientPeer mClientPeer = mClientDic[v];
				mClientDic.Remove(v);
				RemoveClientMsg(mClientPeer);

                mClientPeer.Reset();
                mClientPeerPool.recycle(mClientPeer);
			}
		}

        public void MultiThreadingHandle_SendPackage_Exception(EndPoint endPoint)
        {
            string nPeerId = endPoint.ToString();
            mSocketExceptionList.Push(nPeerId);
        }

        public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mPackage)
		{
            mPackageQueue.Push(mPackage);
        }

		private void AddClient_And_ReceiveNetPackage(NetUdpFixedSizePackage mPackage)
		{
			EndPoint endPoint = mPackage.remoteEndPoint;

			ClientPeer clientPeer = null;
			string nPeerId = endPoint.ToString();
			if (mClientDic.TryGetValue(nPeerId, out clientPeer))
			{
				clientPeer = mClientPeerPool.Pop();
				if (clientPeer != null)
				{
					mClientDic.Add(nPeerId, clientPeer);
					AddClientMsg(clientPeer);
				}
			}

			if (clientPeer != null)
			{
				clientPeer.mMsgReceiveMgr.ReceiveNetPackage(mPackage);
			}
			else
			{
				ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
			}
		}

        private void AddClientMsg(ClientPeer clientPeer)
        {
#if DEBUG
            var mRemoteEndPoint = clientPeer.GetIPEndPoint();
            if (mRemoteEndPoint != null)
            {
                NetLog.Log($"增加客户端: {mRemoteEndPoint}, 客户端总数: {mClientDic.Count}");
            }
            else
            {
                NetLog.Log($"增加客户端, 客户端总数: {mClientDic.Count}");
            }
#endif
        }

        private void RemoveClientMsg(ClientPeer clientPeer)
        {
#if DEBUG
            var mRemoteEndPoint = clientPeer.GetIPEndPoint();
            if (mRemoteEndPoint != null)
            {
                NetLog.Log($"移除客户端: {mRemoteEndPoint}, 客户端总数: {mClientDic.Count}");
            }
            else
            {
                NetLog.Log($"移除客户端, 客户端总数: {mClientDic.Count}");
            }
#endif
        }
    }
}