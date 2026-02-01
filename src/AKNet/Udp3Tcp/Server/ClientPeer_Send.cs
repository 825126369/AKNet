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
using AKNet.Udp3Tcp.Common;
using System;

namespace AKNet.Udp3Tcp.Server
{
    internal partial class ClientPeer
    {
        public void SendInnerNetData(byte id)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(id));
            NetUdpSendFixedSizePackage mPackage = GetObjectPoolManager().UdpSendPackage_Pop();
            mPackage.SetInnerCommandId(id);
            SendNetPackage(mPackage);
            GetObjectPoolManager().UdpSendPackage_Recycle(mPackage);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                SendNetData(mNetPackage.GetPackageId(), mNetPackage.GetData());
            }
        }

        public void SendNetData(UInt16 id)
        {
            if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                mUdpCheckPool.SendTcpStream(ReadOnlySpan<byte>.Empty);
            }
        }

        public void SendNetData(UInt16 id, byte[] data)
        {
            SendNetData(id, data.AsSpan());
        }

        public void SendNetData(UInt16 id, ReadOnlySpan<byte> data)
        {
            if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> mData = mServerMgr.GetCryptoMgr().Encode(id, data);
                mUdpCheckPool.SendTcpStream(mData);
            }
        }

    }

}