/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.LinuxTcp;
using AKNet.Udp4LinuxTcp.Common;
using System;

namespace AKNet.Udp4LinuxTcp.Client
{
    internal class MsgSendMgr
	{
        private ClientPeer mClientPeer;
        public MsgSendMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
        }

		public void SendInnerNetData(byte nInnerCommandId)
		{
			NetLog.Assert(UdpNetCommand.orInnerCommand(nInnerCommandId));
			var skb = new sk_buff();
			skb.mBuffer[0] = nInnerCommandId;
            mClientPeer.SendNetPackage(skb);
        }

		public void SendNetData(NetPackage mNetPackage)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
                SendNetData(mNetPackage.GetPackageId(), mNetPackage.GetData());
            }
		}

		public void SendNetData(UInt16 nLogicPackageId)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				ReadOnlySpan<byte> mData = LikeTcpNetPackageEncryption.Encode(nLogicPackageId, ReadOnlySpan<byte>.Empty);
				mClientPeer.mUdpCheckPool.SendTcpStream(mData);
			}
		}

		public void SendNetData(UInt16 nLogicPackageId, byte[] data)
		{
			SendNetData(nLogicPackageId, data.AsSpan());
		}

        public void SendNetData(UInt16 nLogicPackageId, ReadOnlySpan<byte> data)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> mData = LikeTcpNetPackageEncryption.Encode(nLogicPackageId, data);
                mClientPeer.mUdpCheckPool.SendTcpStream(mData);
            }
        }

    }
}