using System;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Client
{
	//和线程打交道
	internal class MsgReceiveMgr
	{
		private CircularBuffer<byte> mReceiveStreamList = null;
		protected readonly PackageManager mPackageManager = null;
		protected readonly NetPackage mNetPackage = null;

		private readonly object lock_mReceiveStreamList_object = new object();
		private ClientPeer mClientPeer;
		public MsgReceiveMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
			mNetPackage = new TcpNetPackage();
			mPackageManager = new PackageManager();
			mReceiveStreamList = new CircularBuffer<byte>(Config.nSendReceiveCacheBufferInitLength);
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

		public void ReceiveSocketStream(ArraySegment<byte> readOnlySpan)
		{
			lock (lock_mReceiveStreamList_object)
			{
				if (!mReceiveStreamList.isCanWriteFrom(readOnlySpan.Count))
				{
					CircularBuffer<byte> mOldBuffer = mReceiveStreamList;

					int newSize = mOldBuffer.Capacity * 2;
					while (newSize < mOldBuffer.Length + readOnlySpan.Count)
					{
						newSize *= 2;
					}

					mReceiveStreamList = new CircularBuffer<byte>(newSize);
					mReceiveStreamList.WriteFrom(mOldBuffer, mOldBuffer.Length);
                }

                mReceiveStreamList.WriteFrom(readOnlySpan.Array, readOnlySpan.Offset, readOnlySpan.Count);
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
				mPackageManager.NetPackageExecute(this.mClientPeer, mNetPackage);
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
