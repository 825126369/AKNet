using Google.Protobuf;
using Google.Protobuf.Collections;
using System;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    internal class MsgSendMgr
	{
        private ClientPeer mClientPeer;
        public MsgSendMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
        }

		private NetUdpFixedSizePackage GetUdpSystemPackage(UInt16 id, IMessage data = null)
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
				mPackage.CopyFromMsgStream(stream);
				ObjectPoolManager.Instance.nSendBufferPool.recycle(cacheSendBuffer);
			}

			NetPackageEncryption.Encryption(mPackage);
			return mPackage;
		}

		public void SendInnerNetData(UInt16 id, IMessage data = null)
		{
			NetLog.Assert(UdpNetCommand.orInnerCommand(id));
			NetUdpFixedSizePackage mPackage = GetUdpSystemPackage(id, data);
			mClientPeer.SendNetPackage(mPackage);
			ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
		}

		public void SendNetData(UInt16 id, IMessage data = null)
		{
			if (mClientPeer.GetSocketState() == CLIENT_SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				if (data != null)
				{
					byte[] cacheSendBuffer = ObjectPoolManager.Instance.nSendBufferPool.Pop(Config.nUdpCombinePackageFixedSize);
					Span<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendBuffer);
					mClientPeer.mUdpCheckPool.SendLogicPackage(id, stream);
					ObjectPoolManager.Instance.nSendBufferPool.recycle(cacheSendBuffer);
				}
				else
				{
					mClientPeer.mUdpCheckPool.SendLogicPackage(id, null);
				}
			}
		}

		public void SendLuaNetData(UInt16 id, byte[] data = null)
		{
			if (mClientPeer.GetSocketState() == CLIENT_SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				if (data != null)
				{
					Span<byte> stream = new Span<byte>(data);
					mClientPeer.mUdpCheckPool.SendLogicPackage(id, stream);
				}
				else
				{
					mClientPeer.mUdpCheckPool.SendLogicPackage(id, null);
				}
			}
		}
	}
}