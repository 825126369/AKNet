/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4LinuxTcp;
using AKNet.Udp4LinuxTcp.Common;
using System;

namespace AKNet.Udp4LinuxTcp.Server
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

        public void SendInnerNetData(byte nInnerCommandId)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(nInnerCommandId));
            sk_buff skb = new sk_buff();
            skb.nBufferLength = LinuxTcpFunc.sizeof_tcphdr;
            LinuxTcpFunc.tcp_hdr(skb).commandId = nInnerCommandId;
            LinuxTcpFunc.tcp_hdr(skb).tot_len = LinuxTcpFunc.sizeof_tcphdr;
            LinuxTcpFunc.tcp_hdr(skb).WriteTo(skb);
            mClientPeer.SendNetPackage(skb);
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
                ReadOnlySpan<byte> mData = LikeTcpNetPackageEncryption.Encode(id, data);
                mClientPeer.mUdpCheckPool.SendTcpStream(mData);
            }
        }

    }

}