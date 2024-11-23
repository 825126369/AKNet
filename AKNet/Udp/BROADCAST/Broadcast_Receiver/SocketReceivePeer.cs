/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AKNet.Common;
using AKNet.Udp.BROADCAST.COMMON;

namespace AKNet.Udp.BROADCAST.Receiver
{
    public class SocketReceivePeer
	{
		internal SafeObjectPool<NetUdpFixedSizePackage> mUdpFixedSizePackagePool = null;
		protected ConcurrentQueue<NetUdpFixedSizePackage> mNeedHandlePackageQueue = null;
		protected Dictionary<UInt16, Action<ClientPeer, NetUdpFixedSizePackage>> mLogicFuncDic = null;
		protected ClientPeer clientPeer;

		public SocketReceivePeer()
        {
			clientPeer = this as ClientPeer;
			mUdpFixedSizePackagePool = new SafeObjectPool<NetUdpFixedSizePackage> (5);
			mNeedHandlePackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
			mLogicFuncDic = new Dictionary<ushort, Action<ClientPeer, NetUdpFixedSizePackage>> ();
        }

		public void addNetListenFun(UInt16 command, Action<ClientPeer, NetUdpFixedSizePackage> func)
		{
			if (!mLogicFuncDic.ContainsKey (command)) {
				mLogicFuncDic [command] = func;
			} else {
				mLogicFuncDic [command] += func;
			}
		}

		public void removeNetListenFun(UInt16 command, Action<ClientPeer,NetUdpFixedSizePackage> func)
		{
			if (mLogicFuncDic.ContainsKey (command)) {
				mLogicFuncDic [command] -= func;
			}
		}

		public virtual void Update(double elapsed)
		{
			int nPackageCount = 0;

			NetUdpFixedSizePackage mNetPackage = null;
			while (mNeedHandlePackageQueue.TryDequeue(out mNetPackage))
			{
				if (mLogicFuncDic.ContainsKey(mNetPackage.nPackageId))
				{
					mLogicFuncDic[mNetPackage.nPackageId](clientPeer, mNetPackage);
				}

				mUdpFixedSizePackagePool.recycle(mNetPackage);
				nPackageCount++;

				if (nPackageCount > 50)
				{
					break;
				}
			}

			if (nPackageCount > 50)
			{
				NetLog.LogWarning("广播接收器 处理逻辑的数量： " + nPackageCount);
			}
		}

        public void ReceiveUdpSocketFixedPackage(NetUdpFixedSizePackage mPackage)
		{
			bool bSucccess = NetPackageEncryption.DeEncryption (mPackage);
			if (bSucccess) {
				mNeedHandlePackageQueue.Enqueue (mPackage);
			} else {
				NetLog.LogError ("解码失败 !!!");
			}
		}


        public virtual void Release()
        {
			
        }

    }
}