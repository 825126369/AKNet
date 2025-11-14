/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;
using System;

namespace AKNet.Udp4LinuxTcp.Server
{
    internal class MsgSendMgr
    {
        private UdpServer mNetServer = null;
        private ClientPeerPrivate mClientPeer = null;

        public MsgSendMgr(UdpServer mNetServer, ClientPeerPrivate mClientPeer)
        {
            this.mNetServer = mNetServer;
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

        public void SendNetData(UInt16 id)
        {
            SendNetData(id, ReadOnlySpan<byte>.Empty);
        }

        public void SendNetData(UInt16 id, byte[] data)
        {
            SendNetData(id, data.AsSpan());
        }

        public void SendNetData(UInt16 id, ReadOnlySpan<byte> data)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> mData = mNetServer.mCryptoMgr.Encode(id, data);
                mClientPeer.mUdpCheckPool.SendTcpStream(mData);
            }
        }

    }

}