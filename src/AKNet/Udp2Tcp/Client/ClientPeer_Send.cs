/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2Tcp.Common;
using System;

namespace AKNet.Udp2Tcp.Client
{
    internal partial class ClientPeer
    {
		public void SendInnerNetData(UInt16 id)
		{
			NetLog.Assert(UdpNetCommand.orInnerCommand(id));
			NetUdpFixedSizePackage mPackage = GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
			mPackage.SetPackageId(id);
			mPackage.Length = Config.nUdpPackageFixedHeadSize;
			SendNetPackage(mPackage);
		}

		public void SendNetData(NetPackage mNetPackage)
		{
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
                SendNetData(mNetPackage.GetPackageId(), mNetPackage.GetData());
            }
		}

		public void SendNetData(UInt16 id)
		{
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				ReadOnlySpan<byte> mData = mCryptoMgr.Encode(id, ReadOnlySpan<byte>.Empty);
				mUdpCheckPool.SendTcpStream(mData);
			}
		}

		public void SendNetData(UInt16 id, byte[] data)
		{
			SendNetData(id, data.AsSpan());
		}

        public void SendNetData(UInt16 id, ReadOnlySpan<byte> data)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                NetLog.Assert(UdpNetCommand.orNeedCheck(id));
                ReadOnlySpan<byte> mData = mCryptoMgr.Encode(id, data);
                mUdpCheckPool.SendTcpStream(mData);
            }
        }
    }
}