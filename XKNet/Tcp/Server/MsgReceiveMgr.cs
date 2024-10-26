using System;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    internal class MsgReceiveMgr
	{
		private CircularBuffer<byte> mReceiveStreamList = null;
		private readonly object lock_mReceiveStreamList_object = new object();
		private ClientPeer mClientPeer;
		private TcpServer mTcpServer;
        public MsgReceiveMgr(ClientPeer mClientPeer, TcpServer mTcpServer)
		{
			this.mTcpServer = mTcpServer;
			this.mClientPeer = mClientPeer;
            mReceiveStreamList = new CircularBuffer<byte>(Config.nSendReceiveCacheBufferInitLength);
		}

		public void Update(double elapsed)
		{
			switch (mClientPeer.GetSocketState())
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
			TcpNetPackage mNetPackage = mTcpServer.mNetPackage;
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
					mTcpServer.mPackageManager.NetPackageExecute(this.mClientPeer, mNetPackage);
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

	}
}
