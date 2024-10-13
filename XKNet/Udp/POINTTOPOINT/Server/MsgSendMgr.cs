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
                int nOriLength = data.CalculateSize();
                byte[] cacheSendBuffer = ObjectPoolManager.Instance.nSendBufferPool.Pop(Config.nUdpCombinePackageFixedSize);
                ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendBuffer);
                if (stream.Length + Config.nUdpPackageFixedHeadSize > Config.nUdpPackageFixedSize)
                {
                    NetLog.LogError($"stream.Length 1111 : {stream.Length}  {data.GetType()}  {data.CalculateSize()} {nOriLength}");
                }

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
				byte[] cacheSendBuffer = ObjectPoolManager.Instance.nSendBufferPool.Pop(Config.nUdpCombinePackageFixedSize);
                ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendBuffer);
                mClientPeer.mUdpCheckPool.SendLogicPackage(id, stream);
				ObjectPoolManager.Instance.nSendBufferPool.recycle(cacheSendBuffer);
			}
			else
			{
                mClientPeer.mUdpCheckPool.SendLogicPackage(id, ReadOnlySpan<byte>.Empty);
			}
		}

        public void SendNetData(UInt16 id, byte[] data)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                NetLog.Assert(UdpNetCommand.orNeedCheck(id));
                if (data != null)
                {
                    ReadOnlySpan<byte> stream = new ReadOnlySpan<byte>(data);
                    mClientPeer.mUdpCheckPool.SendLogicPackage(id, stream);
                }
                else
                {
                    mClientPeer.mUdpCheckPool.SendLogicPackage(id, ReadOnlySpan<byte>.Empty);
                }
            }
        }
    }

}