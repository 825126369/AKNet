using System;
using System.Collections.Concurrent;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    internal class PackageManager
	{
		private ConcurrentDictionary<UInt16, Action<ClientPeerBase, NetPackage>> mNetEventDic = null;
        private Action<ClientPeerBase, NetPackage> mCommonListenFunc = null;
        public PackageManager()
		{
			mNetEventDic = new ConcurrentDictionary<ushort, Action<ClientPeerBase, NetPackage>>();
		}

		public void NetPackageExecute(ClientPeerBase peer, NetPackage mPackage)
		{
			if (mCommonListenFunc != null)
			{
				mCommonListenFunc(peer, mPackage);
			}
			else
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
		}

        /// <summary>
        /// 通用方法一旦设置，就不能使用字典匹配了
        /// </summary>
        /// <param name="func"></param>
        public void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func)
        {
            mCommonListenFunc = func;
        }

        public void addNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func)
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

		public void removeNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func)
		{
			if (mNetEventDic.ContainsKey(id))
			{
				mNetEventDic[id] -= func;
			}
		}
	}

}