/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2Tcp.Common;
using System;

namespace AKNet.Udp2Tcp.Server
{
    internal class MsgSendMgr
	{
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;

		public MsgSendMgr(UdpServer mNetServer, ClientPeer mClientPeer)
		{
			this.mNetServer = mNetServer;
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
                mClientPeer.mUdpCheckPool.SendTcpStream(ReadOnlySpan<byte>.Empty);
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