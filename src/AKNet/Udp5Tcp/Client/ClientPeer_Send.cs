/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:51
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp5Tcp.Client
{
    internal partial class ClientPeer
    {
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
                SendNetStream(mData);
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
                SendNetStream(mData);
            }
        }
    }
}