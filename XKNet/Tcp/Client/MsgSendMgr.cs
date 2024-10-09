using System;
using Google.Protobuf;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Client
{
    internal class MsgSendMgr
	{
        private byte[] cacheSendProtobufBuffer = new byte[1024];
		private ClientPeer mClientPeer = null;
        public MsgSendMgr(ClientPeer mClientPeer)
        {
			this.mClientPeer = mClientPeer;
		}

		public void SendNetData(UInt16 nPackageId, IMessage data = null)
		{
			if (this.mClientPeer.GetSocketState() == CLIENT_SOCKET_PEER_STATE.CONNECTED)
			{
				if (data == null)
				{
					ArraySegment<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, null);
                    this.mClientPeer.mSocketMgr.SendNetStream(mBufferSegment);
				}
				else
				{
					EnSureSendBufferOk(data);
					Span<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendProtobufBuffer);
					ArraySegment<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, stream);
                    this.mClientPeer.mSocketMgr.SendNetStream(mBufferSegment);
				}
			}
		}

		public void SendLuaNetData(UInt16 nPackageId, byte[] buffer = null)
		{
			if (mClientPeer.GetSocketState() == CLIENT_SOCKET_PEER_STATE.CONNECTED)
			{
				ArraySegment<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, buffer);
                this.mClientPeer.mSocketMgr.SendNetStream(mBufferSegment);
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
    }
}
