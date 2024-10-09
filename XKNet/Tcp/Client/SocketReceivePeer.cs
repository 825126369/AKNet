using System;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Client
{
    //和线程打交道
    public class SocketReceivePeer : ClientPeerBase
	{
		private CircularBuffer<byte> mReceiveStreamList = null;
		protected readonly PackageManager mPackageManager = null;
		protected readonly NetPackage mNetPackage = null;

		private readonly object lock_mReceiveStreamList_object = new object();

		public SocketReceivePeer()
		{
            mNetPackage = new NetPackage();
			mPackageManager = new PackageManager();
			mReceiveStreamList = new CircularBuffer<byte>(Config.nBufferInitLength);
		}

		public void addNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
		{
			mPackageManager.addNetListenFun(nPackageId, fun);
		}

		public void removeNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
		{
			mPackageManager.removeNetListenFun(nPackageId, fun);
		}

		public override void Update(double elapsed)
		{
			base.Update(elapsed);

			switch (mSocketPeerState)
			{
				case CLIENT_SOCKET_PEER_STATE.CONNECTED:
					int nPackageCount = 0;

					while (NetPackageExecute())
					{
						nPackageCount++;
					}

					if (nPackageCount > 0)
					{
						ReceiveHeartBeat(); 
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

		protected void ReceiveSocketStream(ArraySegment<byte> readOnlySpan)
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

					NetLog.LogWarning("mReceiveStreamList Size: " + mReceiveStreamList.Capacity);
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
				mPackageManager.NetPackageExecute(this, mNetPackage);
			}

			return bSuccess;
		}

		public override void Reset()
		{
			base.Reset();
			lock (mReceiveStreamList)
			{
				mReceiveStreamList.reset();
			}
		}

		public override void Release()
		{
			base.Release();
		}
	}
}
