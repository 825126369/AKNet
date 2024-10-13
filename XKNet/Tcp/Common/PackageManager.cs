using System;
using System.Collections.Generic;
using XKNet.Common;

namespace XKNet.Tcp.Common
{
    internal class PackageManager
	{
		private Dictionary<ushort, Action<ClientPeerBase, NetPackage>> mNetEventDic = null;

		public PackageManager()
		{
			mNetEventDic = new Dictionary<ushort, Action<ClientPeerBase, NetPackage>>();
			addNetListenFun(TcpNetCommand.COMMAND_HEARTBEAT, ReceiveHeartBeatPackage);
		}

		public virtual void NetPackageExecute(ClientPeerBase peer, NetPackage mPackage)
		{
			if (mNetEventDic.ContainsKey(mPackage.nPackageId) && mNetEventDic[mPackage.nPackageId] != null)
			{
				mNetEventDic[mPackage.nPackageId](peer, mPackage);
			}
			else
			{
				NetLog.Log("不存在的包Id: " + mPackage.nPackageId);
			}
		}

		public void addNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func)
		{
			NetLog.Assert(func != null);
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

		private void ReceiveHeartBeatPackage(ClientPeerBase clientPeer, NetPackage mNetPackage)
        {
            //NetLog.Log($"心跳包");
        }
	}
}
