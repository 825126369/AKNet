using Google.Protobuf;
using System;
using XKNet.Common;
using XKNet.Udp.Common;

namespace XKNet.Udp.Server
{
    internal class SocketSendPeer : SocketUdp
	{
		public NetUdpFixedSizePackage GetUdpSystemPackage(UInt16 id, IMessage data = null)
		{
			NetLog.Assert(UdpNetCommand.orNeedCheck(id) == false);

			var mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
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
			NetLog.Assert(UdpNetCommand.orNeedCheck(id));
			if (data != null)
			{
				byte[] cacheSendBuffer = ObjectPoolManager.Instance.nSendBufferPool.Pop(Config.nUdpCombinePackageFixedSize);
				Span<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendBuffer);
				mUdpCheckPool.SendLogicPackage(id, stream);
				ObjectPoolManager.Instance.nSendBufferPool.recycle(cacheSendBuffer);
			}
			else
			{
				mUdpCheckPool.SendLogicPackage(id, null);
			}
		}

		public void SendLuaNetData(UInt16 id, byte[] data)
		{
			NetLog.Assert(UdpNetCommand.orNeedCheck(id));
			mUdpCheckPool.SendLogicPackage(id, data);
		}

	}

}