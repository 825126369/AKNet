using System.Collections.Concurrent;
using XKNet.Common;
using XKNet.Udp.Common;

namespace XKNet.Udp.Server
{
    internal abstract class SocketReceivePeer
	{
		protected NetServer mNetServer = null;
		protected ConcurrentQueue<NetPackage> mNeedHandlePackageQueue = null;
		protected UdpCheck3Pool mUdpCheckPool = null;
		protected ClientPeer clientPeer = null;

		public SocketReceivePeer()
		{
			clientPeer = this as ClientPeer;
			mNeedHandlePackageQueue = new ConcurrentQueue<NetPackage>();
			mUdpCheckPool = new UdpCheck3Pool(clientPeer);
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

		public virtual void Update(double elapsed)
		{
			switch ((this as ClientPeer).GetSocketState())
			{
				case SERVER_SOCKET_PEER_STATE.CONNECTED:
					mUdpCheckPool.Update(elapsed);

					int nPackageCount = 0;
					NetPackage mNetPackage = null;
					while (mNeedHandlePackageQueue.TryDequeue(out mNetPackage))
					{
						NetPackageExecute(clientPeer, mNetPackage);
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

		public void ReceiveUdpSocketFixedPackage(NetUdpFixedSizePackage mPackage)
		{
			bool bSucccess = NetPackageEncryption.DeEncryption(mPackage);
			if (bSucccess)
			{
				mUdpCheckPool.ReceivePackage(mPackage);
			}
			else
			{
				NetLog.LogError("解码失败 !!!");
			}
		}

		public virtual void Reset()
		{
			mUdpCheckPool.Reset();
			lock (mNeedHandlePackageQueue)
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
		}

		public virtual void Release()
		{
			mUdpCheckPool.Release();
			lock (mNeedHandlePackageQueue)
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
		}
	}
}