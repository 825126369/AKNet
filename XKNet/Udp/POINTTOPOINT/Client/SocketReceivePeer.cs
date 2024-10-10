using System;
using System.Collections.Concurrent;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    public abstract class SocketReceivePeer
	{
		internal PackageManager mPackageManager = null;
        internal ConcurrentQueue<NetPackage> mNeedHandlePackageQueue = null;
        internal UdpCheck3Pool mUdpCheckPool = null;
        internal ClientPeer clientPeer = null;

		public SocketReceivePeer()
		{
			clientPeer = this as ClientPeer;
			mPackageManager = new PackageManager();
			mNeedHandlePackageQueue = new ConcurrentQueue<NetPackage>();
			mUdpCheckPool = new UdpCheck3Pool(clientPeer);
		}

		public void AddLogicHandleQueue(NetPackage mPackage)
		{
			mNeedHandlePackageQueue.Enqueue(mPackage);
		}

		public void NetPackageExecute(ClientPeer peer, NetPackage mPackage)
		{
			mPackageManager.NetPackageExecute(peer, mPackage);

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
			switch (clientPeer.GetSocketState())
			{
				case CLIENT_SOCKET_PEER_STATE.CONNECTED:
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

		internal void ReceiveNetPackage(NetUdpFixedSizePackage mPackage)
		{
			bool bSucccess = NetPackageEncryption.DeEncryption(mPackage);
			if (bSucccess)
			{
				mUdpCheckPool.ReceivePackage(mPackage);
			}
			else
			{
				NetLog.LogError("Client 解码失败 !!!");
			}
		}

		public void addNetListenFun(UInt16 id, Action<ClientPeer, NetPackage> func)
		{
			mPackageManager.addNetListenFun(id, func);
		}

		public void removeNetListenFun(UInt16 id, Action<ClientPeer, NetPackage> func)
		{
			mPackageManager.removeNetListenFun(id, func);
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