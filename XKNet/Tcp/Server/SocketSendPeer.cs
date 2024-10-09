using System;
using Google.Protobuf;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
	public class SocketSendPeer : TCPSocket
	{
		public SocketSendPeer(ServerBase mNetServer) : base(mNetServer)
		{

		}

		public override void SendNetData(UInt16 nPackageId, IMessage data = null)
		{
			if (mSocketPeerState == SERVER_SOCKET_PEER_STATE.CONNECTED)
			{
				if (data == null)
				{
					ReadOnlySpan<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, null);
					SendNetStream(mBufferSegment);
				}
				else
				{
					EnSureSendBufferOk(data);
                    ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data, mNetServer.cacheSendProtobufBuffer);
					ReadOnlySpan<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, stream);
					SendNetStream(mBufferSegment);
				}
			}
		}

        private void EnSureSendBufferOk(IMessage data)
        {
            int Length = data.CalculateSize();
			var cacheSendProtobufBuffer = mNetServer.cacheSendProtobufBuffer;
            if (cacheSendProtobufBuffer.Length < Length)
            {
                int newSize = cacheSendProtobufBuffer.Length * 2;
                while (newSize < Length)
                {
                    newSize *= 2;
                }

                cacheSendProtobufBuffer = new byte[newSize];
				mNetServer.cacheSendProtobufBuffer = cacheSendProtobufBuffer;
            }
        }
    }
}


