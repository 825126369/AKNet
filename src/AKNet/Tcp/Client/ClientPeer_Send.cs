/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Tcp.Client
{
	internal partial class NetClientMain
    {
        public void SendNetData(ushort nPackageId)
        {
            if (this.mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(nPackageId, ReadOnlySpan<byte>.Empty);
                SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            if (this.mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(nPackageId, data);
                SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            if (this.mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(mNetPackage.GetPackageId(), mNetPackage.GetData());
                SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            if (this.mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                ReadOnlySpan<byte> mBufferSegment = mCryptoMgr.Encode(nPackageId, buffer);
                SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }
    }
}
