/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.IO;
using Google.Protobuf;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Client
{
    internal class MsgSendMgr
	{
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
					ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data);
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

		public void Reset()
		{

		}

		public void Release()
		{

		}
    }
}
