/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:39
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2Tcp.Common;
using System;

namespace AKNet.Udp2Tcp.Client
{
    internal class MsgSendMgr
	{
        private ClientPeer mClientPeer;
        public MsgSendMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
        }

		public void SendInnerNetData(UInt16 id)
		{
			NetLog.Assert(UdpNetCommand.orInnerCommand(id));
			NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
			mPackage.SetPackageId(id);
			mPackage.Length = Config.nUdpPackageFixedHeadSize;
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
				ReadOnlySpan<byte> mData = mClientPeer.mCryptoMgr.Encode(id, ReadOnlySpan<byte>.Empty);
				mClientPeer.mUdpCheckPool.SendTcpStream(mData);
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
                ReadOnlySpan<byte> mData = mClientPeer.mCryptoMgr.Encode(id, data);
                mClientPeer.mUdpCheckPool.SendTcpStream(mData);
            }
        }
    }
}