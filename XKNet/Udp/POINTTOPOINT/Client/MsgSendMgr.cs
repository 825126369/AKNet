using Google.Protobuf;
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

        public void SendNetData(UInt16 id, IMessage data)
		{
			if (mClientPeer.GetSocketState() == CLIENT_SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				byte[] cacheSendBuffer = ObjectPoolManager.Instance.nSendBufferPool.Pop(Config.nUdpCombinePackageFixedSize);
				Span<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendBuffer);
				mClientPeer.mUdpCheckPool.SendLogicPackage(id, stream);
				ObjectPoolManager.Instance.nSendBufferPool.recycle(cacheSendBuffer);
			}
		}

		public void SendLuaNetData(UInt16 id, byte[] data)
		{
			if (mClientPeer.GetSocketState() == CLIENT_SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				Span<byte> stream = new Span<byte>(data);
				mClientPeer.mUdpCheckPool.SendLogicPackage(id, stream);
			}
		}
	}
}