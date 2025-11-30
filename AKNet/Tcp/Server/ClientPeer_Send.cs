/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:38
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Server
{
    internal partial class ClientPeer
    {
        public void SendNetData(ushort nPackageId)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(nPackageId, ReadOnlySpan<byte>.Empty);
                SendNetStream(mBufferSegment);
            }
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(nPackageId, data);
                SendNetStream(mBufferSegment);
            }
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(mNetPackage.GetPackageId(), mNetPackage.GetData());
                SendNetStream(mBufferSegment);
            }
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                ReadOnlySpan<byte> mBufferSegment = mNetServer.mCryptoMgr.Encode(nPackageId, buffer);
                SendNetStream(mBufferSegment);
            }
        }
    }
}
