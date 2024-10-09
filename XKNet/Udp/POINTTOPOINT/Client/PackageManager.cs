using System;
using System.Collections.Concurrent;
using XKNet.Common;
using XKNet.Udp.Common;

namespace XKNet.Udp.Client
{
    internal class PackageManager
	{
		private ConcurrentDictionary<UInt16, Action<ClientPeer, NetPackage>> mNetEventDic = null;

		public PackageManager()
		{
			mNetEventDic = new ConcurrentDictionary<ushort, Action<ClientPeer, NetPackage>>();
		}

		public virtual void NetPackageExecute(ClientPeer peer, NetPackage mPackage)
		{
			if (mNetEventDic.ContainsKey(mPackage.nPackageId) && mNetEventDic[mPackage.nPackageId] != null)
			{
				mNetEventDic[mPackage.nPackageId](peer, mPackage);
			}
			else
			{
				NetLog.Log("Client 不存在的包Id: " + mPackage.nPackageId);
			}
		}

		public void addNetListenFun(UInt16 id, Action<ClientPeer, NetPackage> func)
		{
			NetLog.Assert(func != null, "Client addNetListenFun is Null :" + id);
			if (!mNetEventDic.ContainsKey(id))
			{
				mNetEventDic[id] = func;
			}
			else
			{
				mNetEventDic[id] += func;
			}
		}

		public void removeNetListenFun(UInt16 id, Action<ClientPeer, NetPackage> func)
		{
			if (mNetEventDic.ContainsKey(id))
			{
				mNetEventDic[id] -= func;
			}
		}
	}

}