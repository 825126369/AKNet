using System;
using System.IO;
using Google.Protobuf;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Client
{
    internal class MsgSendMgr
	{
        private byte[] cacheSendProtobufBuffer = new byte[Config.nMsgPackageBufferMaxLength];
		private ClientPeer mClientPeer = null;
		public MsgSendMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
		}

		public void SendNetData(NetPackage mNetPackage)
		{
			if (this.mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
                ReadOnlySpan<byte> mBufferSegment = NetPackageEncryption.Encryption(mNetPackage.nPackageId, mNetPackage.GetBuffBody());
                this.mClientPeer.mSocketMgr.SendNetStream(mBufferSegment);
            }
		}

        public void SendNetData(UInt16 nPackageId)
        {
            if (this.mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, null);
                this.mClientPeer.mSocketMgr.SendNetStream(mBufferSegment);
            }
        }

        public void SendNetData(UInt16 nPackageId, IMessage data)
		{
			if (this.mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				if (data == null)
				{
					SendNetData(nPackageId);
				}
				else
				{
					EnSureSendBufferOk(data);
					ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendProtobufBuffer);
                    ReadOnlySpan<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, stream);
                    this.mClientPeer.mSocketMgr.SendNetStream(mBufferSegment);
				}
			}
		}

		public void SendNetData(UInt16 nPackageId, byte[] buffer)
		{
            SendNetData(nPackageId, buffer.AsSpan());
		}

        public void SendNetData(UInt16 nPackageId, ReadOnlySpan<byte> buffer)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                if (buffer == ReadOnlySpan<byte>.Empty)
                {
                    SendNetData(nPackageId);
                }
                else
                {
                    ReadOnlySpan<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, buffer);
                    this.mClientPeer.mSocketMgr.SendNetStream(mBufferSegment);
                }
            }
        }

        private void EnSureSendBufferOk(IMessage data)
        {
            int Length = data.CalculateSize();
            if (cacheSendProtobufBuffer.Length < Length)
            {
                int newSize = cacheSendProtobufBuffer.Length * 2;
                while (newSize < Length)
                {
                    newSize *= 2;
                }

                cacheSendProtobufBuffer = new byte[newSize];
            }
        }

		public void Reset()
		{

		}

		public void Release()
		{

		}
    }
}
