using System;
using Google.Protobuf;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Client
{
    public class SocketSendPeer : TcpSocket
	{
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
					Span<byte> stream = Protocol3Utility.SerializePackage(data);
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
	}
}
