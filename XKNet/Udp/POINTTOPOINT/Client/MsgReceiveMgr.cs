using System;
using System.Collections.Concurrent;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    internal class MsgReceiveMgr
    {
		internal PackageManager mPackageManager = null;
        internal ConcurrentQueue<NetPackage> mNeedHandlePackageQueue = null;
        internal ClientPeer mClientPeer = null;

		public MsgReceiveMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
			mPackageManager = new PackageManager();
			mNeedHandlePackageQueue = new ConcurrentQueue<NetPackage>();
		}

		public void AddLogicHandleQueue(NetPackage mPackage)
		{
			mNeedHandlePackageQueue.Enqueue(mPackage);
		}

		public void NetPackageExecute(ClientPeerBase peer, NetPackage mPackage)
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
			else
			{
				NetLog.Assert(false);
			}
		}

		public void Update(double elapsed)
		{
			var mSocketState = mClientPeer.GetSocketState();
			switch (mSocketState)
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
				NetLog.LogError("Client 解码失败 !!!");
			}
		}

		public void addNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func)
		{
			mPackageManager.addNetListenFun(id, func);
		}

		public void removeNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func)
		{
			mPackageManager.removeNetListenFun(id, func);
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
                else
                {
                    NetLog.Assert(false);
                }
            }
		}

		public void Release()
		{
			Reset();
		}

	}
}