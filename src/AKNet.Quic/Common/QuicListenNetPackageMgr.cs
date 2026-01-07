/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:14
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    internal class QuicListenNetPackageMgr
	{
		private readonly Dictionary<ushort, Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage>> mNetEventDic = null;
		private event Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage> mCommonListenFunc = null;

		public QuicListenNetPackageMgr()
		{
			mNetEventDic = new Dictionary<ushort, Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage>>();
		}

		public void NetPackageExecute(QuicClientPeerBase peer, QuicStreamBase streamBase, QuicNetPackage mPackage)
		{
			if (mCommonListenFunc != null)
			{
				mCommonListenFunc(peer, streamBase, mPackage);
			}
			else
			{
				ushort nPackageId = mPackage.GetPackageId();
				if (mNetEventDic.ContainsKey(nPackageId) && mNetEventDic[nPackageId] != null)
				{
					mNetEventDic[nPackageId](peer, streamBase, mPackage);
				}
				else
				{
					NetLog.Log("不存在的包Id: " + nPackageId);
				}
			}
		}
		
        public void addNetListenFunc(Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage> func)
        {
			mCommonListenFunc += func;
        }

        public void removeNetListenFunc(Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage> func)
        {
            mCommonListenFunc -= func;
        }

        public void addNetListenFunc(UInt16 id, Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage> func)
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

		public void removeNetListenFunc(UInt16 id, Action<QuicClientPeerBase, QuicStreamBase, QuicNetPackage> func)
		{
			if (mNetEventDic.ContainsKey(id))
			{
				mNetEventDic[id] -= func;
			}
		}
	}
}
