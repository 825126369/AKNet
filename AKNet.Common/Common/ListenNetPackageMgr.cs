/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:43
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
[assembly: InternalsVisibleTo("AKNet.Other")]
namespace AKNet.Common
{
    internal class ListenNetPackageMgr
	{
		private readonly Dictionary<ushort, Action<ClientPeerBase, NetPackage>> mNetEventDic = null;
		private event Action<ClientPeerBase, NetPackage> mCommonListenFunc = null;

		public ListenNetPackageMgr()
		{
			mNetEventDic = new Dictionary<ushort, Action<ClientPeerBase, NetPackage>>();
		}

		public void NetPackageExecute(ClientPeerBase peer, NetPackage mPackage)
		{
			if (mCommonListenFunc != null)
			{
				mCommonListenFunc(peer, mPackage);
			}
			else
			{
				ushort nPackageId = mPackage.GetPackageId();
				if (mNetEventDic.ContainsKey(nPackageId) && mNetEventDic[nPackageId] != null)
				{
					mNetEventDic[nPackageId](peer, mPackage);
				}
				else
				{
					NetLog.Log("不存在的包Id: " + nPackageId);
				}
			}
		}
		
        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> func)
        {
			mCommonListenFunc += func;
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> func)
        {
            mCommonListenFunc -= func;
        }

        public void addNetListenFunc(UInt16 id, Action<ClientPeerBase, NetPackage> func)
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

		public void removeNetListenFunc(UInt16 id, Action<ClientPeerBase, NetPackage> func)
		{
			if (mNetEventDic.ContainsKey(id))
			{
				mNetEventDic[id] -= func;
			}
		}
	}
}
