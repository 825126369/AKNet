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
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				if (data == null)
				{
                    SendNetData(nPackageId);
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

		public void SendNetData(UInt16 nPackageId, byte[] buffer)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				if (buffer == null)
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

        public void Reset()
        {

        }

    }
}


