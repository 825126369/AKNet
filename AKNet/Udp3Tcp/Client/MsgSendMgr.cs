/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using AKNet.Common;
using AKNet.Udp3Tcp.Common;

namespace AKNet.Udp3Tcp.Client
{
    internal class MsgSendMgr
	{
        private ClientPeer mClientPeer;
        public MsgSendMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
        }

		public void SendInnerNetData(byte id)
		{
			NetLog.Assert(UdpNetCommand.orInnerCommand(id));
			var mPackage = mClientPeer.GetObjectPoolManager().UdpSendPackage_Pop();
			mPackage.nPackageId = id;
			mPackage.nOrderId = 0;
			mPackage.nRequestOrderId = Config.nUdpPackageFixedHeadSize;
			mClientPeer.SendNetPackage(mPackage);
		}

		public void SendNetData(NetPackage mNetPackage)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
                SendNetData(mNetPackage.GetPackageId(), mNetPackage.GetData());
            }
		}

		public void SendNetData(UInt16 id)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				ReadOnlySpan<byte> mData = LikeTcpNetPackageEncryption.Encode(id, ReadOnlySpan<byte>.Empty);
				mClientPeer.mUdpCheckPool.SendTcpStream(mData);
			}
		}

		public void SendNetData(UInt16 id, IMessage data)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				if (data != null)
				{
					ReadOnlySpan<byte> mData = Protocol3Utility.SerializePackage(data);
                    mData = LikeTcpNetPackageEncryption.Encode(id, mData);
					mClientPeer.mUdpCheckPool.SendTcpStream(mData);
				}
				else
				{
                    ReadOnlySpan<byte> mData = LikeTcpNetPackageEncryption.Encode(id, ReadOnlySpan<byte>.Empty);
                    mClientPeer.mUdpCheckPool.SendTcpStream(mData);
                }
			}
		}

		public void SendNetData(UInt16 id, byte[] data)
		{
			SendNetData(id, data.AsSpan());
		}

        public void SendNetData(UInt16 id, ReadOnlySpan<byte> data)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                NetLog.Assert(UdpNetCommand.orNeedCheck(id));
                ReadOnlySpan<byte> mData = LikeTcpNetPackageEncryption.Encode(id, data);
                mClientPeer.mUdpCheckPool.SendTcpStream(mData);
            }
        }
    }
}