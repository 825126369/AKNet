using System;
using Google.Protobuf;
using XKNetCommon;
using XKNetTcpCommon;

namespace XKNetTcpServer
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
					ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data);
					ReadOnlySpan<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, stream);
					SendNetStream(mBufferSegment);
				}
			}
		}
	}
}


