using Google.Protobuf;
using System;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    public class SocketSendPeer : SocketUdp
	{
		internal NetUdpFixedSizePackage GetUdpSystemPackage(UInt16 id, IMessage data = null)
		{
			NetLog.Assert(UdpNetCommand.orNeedCheck(id) == false, "id: " + id);

			NetUdpFixedSizePackage mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
			mPackage.nOrderId = 0;
			mPackage.nGroupCount = 0;
			mPackage.nPackageId = id;
			mPackage.Length = Config.nUdpPackageFixedHeadSize;

			if (data != null)
			{
				byte[] cacheSendBuffer = ObjectPoolManager.Instance.nSendBufferPool.Pop(Config.nUdpCombinePackageFixedSize);
				Span<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendBuffer);
				mPackage.Length += stream.Length;
				for (int i = 0; i < stream.Length; i++)
				{
					mPackage.buffer[Config.nUdpPackageFixedHeadSize + i] = stream[i];
				}
				ObjectPoolManager.Instance.nSendBufferPool.recycle(cacheSendBuffer);
			}

			NetPackageEncryption.Encryption(mPackage);

			return mPackage;
		}

		public void SendNetData(UInt16 id, IMessage data)
		{
			if (mSocketPeerState == CLIENT_SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				byte[] cacheSendBuffer = ObjectPoolManager.Instance.nSendBufferPool.Pop(Config.nUdpCombinePackageFixedSize);
				Span<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendBuffer);
				mUdpCheckPool.SendLogicPackage(id, stream);
				ObjectPoolManager.Instance.nSendBufferPool.recycle(cacheSendBuffer);
			}
		}

		public void SendLuaNetData(UInt16 id, byte[] data)
		{
			if (mSocketPeerState == CLIENT_SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				Span<byte> stream = new Span<byte>(data);
				mUdpCheckPool.SendLogicPackage(id, stream);
			}
		}
	}
}