/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
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
			mClientPeer.mUdpCheckPool.SendInnerNetData(nInnerCommandId);
        }

		public void SendNetData(NetPackage mNetPackage)
		{
            SendNetData(mNetPackage.GetPackageId(), mNetPackage.GetData());
        }

		public void SendNetData(UInt16 nLogicPackageId)
		{
            SendNetData(nLogicPackageId, ReadOnlySpan<byte>.Empty);
        }

		public void SendNetData(UInt16 nLogicPackageId, byte[] data)
		{
			SendNetData(nLogicPackageId, data.AsSpan());
		}

        public void SendNetData(UInt16 nLogicPackageId, ReadOnlySpan<byte> data)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> mData = mClientPeer.mCryptoMgr.Encode(nLogicPackageId, data);
                mClientPeer.mUdpCheckPool.SendTcpStream(mData);
            }
        }

    }
}