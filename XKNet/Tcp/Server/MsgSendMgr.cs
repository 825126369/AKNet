using Google.Protobuf;
using System;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    internal class MsgSendMgr
	{
		private ClientPeer mClientPeer;
		public MsgSendMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
        }

		public void SendNetData(UInt16 nPackageId, IMessage data = null)
		{
			if (mClientPeer.GetSocketState() == SERVER_SOCKET_PEER_STATE.CONNECTED)
			{
				if (data == null)
				{
					ReadOnlySpan<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, null);
					mClientPeer.mSocketMgr.SendNetStream(mBufferSegment);
				}
				else
				{
					EnSureSendBufferOk(data);
					ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data, ServerGlobalVariable.Instance.cacheSendProtobufBuffer);
					ReadOnlySpan<byte> mBufferSegment = NetPackageEncryption.Encryption(nPackageId, stream);
					mClientPeer.mSocketMgr.SendNetStream(mBufferSegment);
				}
			}
		}

		private void EnSureSendBufferOk(IMessage data)
		{
			int Length = data.CalculateSize();
			var cacheSendProtobufBuffer = ServerGlobalVariable.Instance.cacheSendProtobufBuffer;
			if (cacheSendProtobufBuffer.Length < Length)
			{
				int newSize = cacheSendProtobufBuffer.Length * 2;
				while (newSize < Length)
				{
					newSize *= 2;
				}

				cacheSendProtobufBuffer = new byte[newSize];
				ServerGlobalVariable.Instance.cacheSendProtobufBuffer = cacheSendProtobufBuffer;
			}
		}
    }
}


