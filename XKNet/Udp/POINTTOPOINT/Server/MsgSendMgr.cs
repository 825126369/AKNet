using Google.Protobuf;
using System;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class MsgSendMgr
	{
        private NetServer mNetServer = null;
        private ClientPeer mClientPeer = null;

		public MsgSendMgr(NetServer mNetServer, ClientPeer mClientPeer)
		{
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
		}

		public void SendNetData(UInt16 id, IMessage data)
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

}