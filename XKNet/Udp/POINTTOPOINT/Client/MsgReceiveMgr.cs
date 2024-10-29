using System;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    internal class MsgReceiveMgr
    {
		internal readonly PackageManager mPackageManager = new PackageManager();
        internal ClientPeer mClientPeer = null;

		public MsgReceiveMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
		}

		public void AddLogicHandleQueue(NetPackage mPackage)
		{
            NetPackageExecute(mClientPeer, mPackage);
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
			
		}

		public void Release()
		{
			Reset();
		}

	}
}