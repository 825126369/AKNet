/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Client
{
	//和线程打交道
	internal class MsgReceiveMgr
	{
		private CircularBuffer<byte> mReceiveStreamList = null;
		protected readonly PackageManager mPackageManager = null;
		protected readonly TcpNetPackage mNetPackage = null;

		private readonly object lock_mReceiveStreamList_object = new object();
		private ClientPeer mClientPeer;
		public MsgReceiveMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
			mNetPackage = new TcpNetPackage();
			mPackageManager = new PackageManager();
			mReceiveStreamList = new CircularBuffer<byte>(Config.nSendReceiveCacheBufferInitLength);
		}

        public void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> fun)
        {
            mPackageManager.SetNetCommonListenFun(fun);
        }

        public void addNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
		{
			mPackageManager.addNetListenFun(nPackageId, fun);
		}

		public void removeNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
		{
			mPackageManager.removeNetListenFun(nPackageId, fun);
		}

		public void Update(double elapsed)
		{
			var mSocketPeerState = mClientPeer.GetSocketState();
			switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTED:
					int nPackageCount = 0;

					while (NetPackageExecute())
					{
						nPackageCount++;
					}

					if (nPackageCount > 0)
					{
						mClientPeer.ReceiveHeartBeat();
					}

					if (nPackageCount > 50)
					{
						NetLog.LogWarning("Client 处理逻辑包的数量： " + nPackageCount);
					}

					break;
				default:
					break;
			}
		}

        private void EnSureCircularBufferCapacityOk(ReadOnlySpan<byte> readOnlySpan)
        {
            if (!mReceiveStreamList.isCanWriteFrom(readOnlySpan.Length))
            {
                CircularBuffer<byte> mOldBuffer = mReceiveStreamList;

                int newSize = mOldBuffer.Capacity * 2;
                while (newSize < mOldBuffer.Length + readOnlySpan.Length)
                {
                    newSize *= 2;
                }

                mReceiveStreamList = new CircularBuffer<byte>(newSize);
                mReceiveStreamList.WriteFrom(mOldBuffer, mOldBuffer.Capacity);

                NetLog.LogWarning("mReceiveStreamList Size: " + mReceiveStreamList.Capacity + " | " + mReceiveStreamList.Length + " | " + readOnlySpan.Length);
            }
        }

        public void ReceiveSocketStream(ReadOnlySpan<byte> readOnlySpan)
		{
			lock (lock_mReceiveStreamList_object)
			{
				EnSureCircularBufferCapacityOk(readOnlySpan);
                mReceiveStreamList.WriteFrom(readOnlySpan);
            }
        }

		private bool NetPackageExecute()
		{
			bool bSuccess = false;

			lock (lock_mReceiveStreamList_object)
			{
				if (mReceiveStreamList.Length > 0)
				{
					bSuccess = NetPackageEncryption.DeEncryption(mReceiveStreamList, mNetPackage);
				}
			}

			if (bSuccess)
			{
				if (TcpNetCommand.orInnerCommand(mNetPackage.nPackageId))
				{

				}
				else
				{
					mPackageManager.NetPackageExecute(this.mClientPeer, mNetPackage);
				}
			}

			return bSuccess;
		}

		public void Reset()
		{
			lock (mReceiveStreamList)
			{
				mReceiveStreamList.reset();
			}
		}

		public void Release()
		{

		}
	}
}
