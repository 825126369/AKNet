using System;
using Google.Protobuf;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Client
{
    public class SocketSendPeer : TcpSocket
	{
        private byte[] cacheSendProtobufBuffer = new byte[1024];
        
		public SocketSendPeer()
        {
			
		}

		public override void SendNetData(UInt16 nPackageId, IMessage data = null)
		{
			if (mSocketPeerState == CLIENT_SOCKET_PEER_STATE.CONNECTED)
			{
				if (data == null)
				{
					ArraySegment<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, null);
					SendNetStream(mBufferSegment);
				}
				else
				{
					EnSureSendBufferOk(data);
					Span<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendProtobufBuffer);
					ArraySegment<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, stream);
					SendNetStream(mBufferSegment);
				}
			}
		}

		public override void SendLuaNetData(UInt16 nPackageId, byte[] buffer = null)
		{
			if (mSocketPeerState == CLIENT_SOCKET_PEER_STATE.CONNECTED)
			{
				ArraySegment<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, buffer);
				SendNetStream(mBufferSegment);
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
