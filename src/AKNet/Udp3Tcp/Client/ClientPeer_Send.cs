/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;
using System;

namespace AKNet.Udp3Tcp.Client
{
    internal partial class ClientPeer
    {
		public void SendInnerNetData(byte nInnerCommandId)
		{
			NetLog.Assert(UdpNetCommand.orInnerCommand(nInnerCommandId));
			var mPackage = GetObjectPoolManager().UdpSendPackage_Pop();
			mPackage.SetInnerCommandId(nInnerCommandId);
			SendNetPackage(mPackage);
            GetObjectPoolManager().UdpSendPackage_Recycle(mPackage);
        }

		public void SendNetData(NetPackage mNetPackage)
		{
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
                SendNetData(mNetPackage.GetPackageId(), mNetPackage.GetData());
            }
		}

		public void SendNetData(UInt16 nLogicPackageId)
		{
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				ReadOnlySpan<byte> mData = mCryptoMgr.Encode(nLogicPackageId, ReadOnlySpan<byte>.Empty);
				mUdpCheckPool.SendTcpStream(mData);
			}
		}

		public void SendNetData(UInt16 nLogicPackageId, byte[] data)
		{
			SendNetData(nLogicPackageId, data.AsSpan());
		}

        public void SendNetData(UInt16 nLogicPackageId, ReadOnlySpan<byte> data)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> mData = mCryptoMgr.Encode(nLogicPackageId, data);
                mUdpCheckPool.SendTcpStream(mData);
            }
        }
    }
}