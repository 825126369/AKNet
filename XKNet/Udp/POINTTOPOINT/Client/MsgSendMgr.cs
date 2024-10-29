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

		public void SendInnerNetData(UInt16 id, ushort nOrderId = 0)
		{
			NetLog.Assert(UdpNetCommand.orInnerCommand(id));
			NetUdpFixedSizePackage mPackage = UdpNetCommand.GetUdpInnerCommandPackage(id, nOrderId);
			mClientPeer.SendNetPackage(mPackage);
		}

		public void SendNetData(NetPackage mNetPackage)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(mNetPackage.nPackageId));
				mClientPeer.mUdpCheckPool.SendLogicPackage(mNetPackage.nPackageId, mNetPackage.GetBuffBody());
			}
			else
			{
				NetLog.LogError("SendNetData Failed: " + mClientPeer.GetSocketState());
			}
		}

        public void SendNetData(UInt16 id)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				mClientPeer.mUdpCheckPool.SendLogicPackage(id, ReadOnlySpan<byte>.Empty);
			}
            else
            {
                NetLog.LogError("SendNetData Failed: " + mClientPeer.GetSocketState());
            }
        }

		public void SendNetData(UInt16 id, IMessage data)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				if (data != null)
				{
					byte[] cacheSendBuffer = ObjectPoolManager.Instance.EnSureSendBufferOk(data);
					ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendBuffer);
					mClientPeer.mUdpCheckPool.SendLogicPackage(id, stream);
				}
				else
				{
					mClientPeer.mUdpCheckPool.SendLogicPackage(id, ReadOnlySpan<byte>.Empty);
				}
			}
			else
			{
				NetLog.LogError("SendNetData Failed: " + mClientPeer.GetSocketState());
			}
		}

		public void SendNetData(UInt16 id, byte[] data)
		{
			SendNetData(id, data.AsSpan());
		}

        public void SendNetData(UInt16 id, ReadOnlySpan<byte> data)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                NetLog.Assert(UdpNetCommand.orNeedCheck(id));
                mClientPeer.mUdpCheckPool.SendLogicPackage(id, data);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + mClientPeer.GetSocketState());
            }
        }
    }
}