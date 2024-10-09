using System;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    public abstract class SocketReceivePeer : ClientPeerBase
	{
		private CircularBuffer<byte> mReceiveStreamList = null;
		private object lock_mReceiveStreamList_object = new object();
		
		public SocketReceivePeer(ServerBase mNetServer) : base(mNetServer)
		{
			mReceiveStreamList = new CircularBuffer<byte>(ServerConfig.nBufferMaxLength);
		}

		public override void Update(double elapsed)
		{
			base.Update(elapsed);
			switch (mSocketPeerState)
			{
				case SERVER_SOCKET_PEER_STATE.CONNECTED:
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
		
        protected void ReceiveSocketStream(ReadOnlySpan<byte> readOnlySpan)
		{
			lock (lock_mReceiveStreamList_object)
			{
				EnSureCircularBufferCapacityOk(readOnlySpan);
                mReceiveStreamList.WriteFrom(readOnlySpan);
			}
		}

		private bool NetPackageExecute()
		{
			NetPackage mNetPackage = mNetServer.mNetPackage;
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
				mNetServer.mPackageManager.NetPackageExecute(this, mNetPackage);
			}

			return bSuccess;
		}

		internal override void Reset()
		{
			base.Reset();
			lock (mReceiveStreamList)
			{
				mReceiveStreamList.reset();
			}
		}

	}
}
