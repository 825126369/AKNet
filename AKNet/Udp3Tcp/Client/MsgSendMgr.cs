/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:27
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;
using System;

namespace AKNet.Udp3Tcp.Client
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
			var mPackage = mClientPeer.GetObjectPoolManager().UdpSendPackage_Pop();
			mPackage.SetInnerCommandId(nInnerCommandId);
			mClientPeer.SendNetPackage(mPackage);
            mClientPeer.GetObjectPoolManager().UdpSendPackage_Recycle(mPackage);
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
				ReadOnlySpan<byte> mData = mClientPeer.mCryptoMgr.Encode(nLogicPackageId, ReadOnlySpan<byte>.Empty);
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
                ReadOnlySpan<byte> mData = mClientPeer.mCryptoMgr.Encode(nLogicPackageId, data);
                mClientPeer.mUdpCheckPool.SendTcpStream(mData);
            }
        }
    }
}