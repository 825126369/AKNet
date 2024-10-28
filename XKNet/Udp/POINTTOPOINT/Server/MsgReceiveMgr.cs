using System.Collections.Concurrent;
using System.Collections.Generic;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class MsgReceiveMgr
	{
        private readonly Queue<NetPackage> mNeedHandlePackageQueue = new Queue<NetPackage>();
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;
		
		public MsgReceiveMgr(UdpServer mNetServer, ClientPeer mClientPeer)
        {
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
		}

		public void AddLogicHandleQueue(NetPackage mPackage)
		{
            mNeedHandlePackageQueue.Enqueue(mPackage);
		}

		private void NetPackageExecute(ClientPeer clientPeer, NetPackage mPackage)
		{
			mNetServer.GetPackageManager().NetPackageExecute(clientPeer, mPackage);
			if (mPackage is NetCombinePackage)
			{
				ObjectPoolManager.Instance.mCombinePackagePool.recycle(mPackage as NetCombinePackage);
			}
			else if (mPackage is NetUdpFixedSizePackage)
			{
				ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage as NetUdpFixedSizePackage);
			}
		}

		public void Update(double elapsed)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				int nPackageCount = 0;
				NetPackage mNetPackage = null;
				while (mNeedHandlePackageQueue.TryDequeue(out mNetPackage))
				{
					NetPackageExecute(mClientPeer, mNetPackage);
					nPackageCount++;
				}

				if (nPackageCount > 50)
				{
					NetLog.LogWarning("Client 处理逻辑包的数量： " + nPackageCount);
				}
			}
		}

		public void ReceiveNetPackage(NetUdpFixedSizePackage mPackage)
		{
            mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
        }

		public void Reset()
		{
			while (mNeedHandlePackageQueue.Count > 0)
			{
				NetPackage mNetPackage = mNeedHandlePackageQueue.Dequeue();
				if (mNetPackage is NetCombinePackage)
				{
					ObjectPoolManager.Instance.mCombinePackagePool.recycle(mNetPackage as NetCombinePackage);
				}
				else if (mNetPackage is NetUdpFixedSizePackage)
				{
					ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mNetPackage as NetUdpFixedSizePackage);
				}
				else
				{
					NetLog.Assert(false);
				}
			}
		}
	}
}