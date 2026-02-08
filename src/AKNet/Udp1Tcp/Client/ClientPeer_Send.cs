/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp1Tcp.Common;
using System;

namespace AKNet.Udp1Tcp.Client
{
    internal partial class ClientPeer
    {
		public void SendInnerNetData(UInt16 id)
		{
			NetLog.Assert(UdpNetCommand.orInnerCommand(id));
			NetUdpFixedSizePackage mPackage = GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
			mPackage.nPackageId = id;
			mPackage.Length = Config.nUdpPackageFixedHeadSize;
			SendNetPackage(mPackage);
		}

		public void SendNetData(NetPackage mNetPackage)
		{
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
                SendNetData(mNetPackage.GetPackageId(), mNetPackage.GetData());
            }
			else
			{
				NetLog.LogError("SendNetData Failed: " + GetSocketState());
			}
		}

        public void SendNetData(UInt16 id)
		{
			if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(UdpNetCommand.orNeedCheck(id));
				mUdpCheckPool.SendLogicPackage(id, ReadOnlySpan<byte>.Empty);
			}
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
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
                mUdpCheckPool.SendLogicPackage(id, data);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }
    }
}