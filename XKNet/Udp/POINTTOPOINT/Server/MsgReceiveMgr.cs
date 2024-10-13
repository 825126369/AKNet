using System.Collections.Concurrent;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class MsgReceiveMgr
	{
        private ConcurrentQueue<NetPackage> mNeedHandlePackageQueue = null;

        private NetServer mNetServer = null;
        private ClientPeer mClientPeer = null;
		
		public MsgReceiveMgr(NetServer mNetServer, ClientPeer mClientPeer)
        {
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
			mNeedHandlePackageQueue = new ConcurrentQueue<NetPackage>();
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
			switch (mClientPeer.GetSocketState())
			{
				case SOCKET_PEER_STATE.CONNECTED:
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

					break;
				default:
					this.Reset();
					break;
			}
		}

		public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mPackage)
		{
			bool bSucccess = NetPackageEncryption.DeEncryption(mPackage);
			if (bSucccess)
			{
				mClientPeer.mUdpCheckPool.MultiThreadingReceiveNetPackage(mPackage);
			}
			else
			{
				ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
				NetLog.LogError("解码失败 !!!");
			}
		}

		public void Reset()
		{
			NetPackage mNetPackage = null;
			while (mNeedHandlePackageQueue.TryDequeue(out mNetPackage))
			{
				if (mNetPackage is NetCombinePackage)
				{
					ObjectPoolManager.Instance.mCombinePackagePool.recycle(mNetPackage as NetCombinePackage);
				}
				else if (mNetPackage is NetUdpFixedSizePackage)
				{
					ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mNetPackage as NetUdpFixedSizePackage);
				}
			}
		}

		public void Release()
		{
			Reset();
		}
	}
}