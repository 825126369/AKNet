using Google.Protobuf;
using System;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class MsgSendMgr
	{
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;

		public MsgSendMgr(UdpServer mNetServer, ClientPeer mClientPeer)
		{
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
		}

        private NetUdpFixedSizePackage GetUdpInnerCommandPackage(UInt16 id, ushort nOrderId = 0)
        {
            NetUdpFixedSizePackage mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
            mPackage.nOrderId = nOrderId;
            mPackage.nGroupCount = 0;
            mPackage.nPackageId = id;
            mPackage.Length = Config.nUdpPackageFixedHeadSize;
            NetPackageEncryption.Encryption(mPackage);
            return mPackage;
        }

        public void SendInnerNetData(UInt16 id, ushort nOrderId = 0)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(id));
            NetUdpFixedSizePackage mPackage = GetUdpInnerCommandPackage(id, nOrderId);
            mClientPeer.SendNetPackage(mPackage);
            ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                NetLog.Assert(UdpNetCommand.orNeedCheck(mNetPackage.nPackageId));
                mClientPeer.mUdpCheckPool.SendLogicPackage(mNetPackage.nPackageId, mNetPackage.GetBuffBody());
            }
        }

        public void SendNetData(UInt16 id)
        {
            NetLog.Assert(UdpNetCommand.orNeedCheck(id));
            mClientPeer.mUdpCheckPool.SendLogicPackage(id, ReadOnlySpan<byte>.Empty);
        }

        public void SendNetData(UInt16 id, IMessage data)
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

        public void SendNetData(UInt16 id, byte[] data)
        {
            SendNetData(id, data.AsSpan());
        }

        public void SendNetData(UInt16 id, ReadOnlySpan<byte> data)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                NetLog.Assert(UdpNetCommand.orNeedCheck(id));
                if (data != null)
                {
                    mClientPeer.mUdpCheckPool.SendLogicPackage(id, data);
                }
                else
                {
                    mClientPeer.mUdpCheckPool.SendLogicPackage(id, ReadOnlySpan<byte>.Empty);
                }
            }
        }
    }

}