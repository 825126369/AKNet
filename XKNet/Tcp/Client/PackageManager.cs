using System;
using System.Collections.Generic;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Client
{
    public class PackageManager
	{
		private Dictionary<UInt16, Action<ClientPeerBase, NetPackage>> mNetEventDic = null;

		public PackageManager()
		{
			mNetEventDic = new Dictionary<ushort, Action<ClientPeerBase, NetPackage>>();
			addNetListenFun(TcpNetCommand.COMMAND_HEARTBEAT, ReceiveHeartBeatPackage);
			addNetListenFun(TcpNetCommand.COMMAND_CONNECTFULL, ReceiveConnectFullPackage);
		}

		public virtual void NetPackageExecute(ClientPeerBase peer, NetPackage mPackage)
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

		private void ReceiveHeartBeatPackage(ClientPeerBase clientPeer, NetPackage mNetPackage)
		{
            //NetLog.Log("心跳包");
        }

		private void ReceiveConnectFullPackage(ClientPeerBase clientPeer, NetPackage mNetPackage)
		{
			NetLog.Log("服务器 连接已爆满");
		}
	}
}
