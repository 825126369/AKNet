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
namespace AKNet.Quic.Server
{
    internal partial class ClientPeer
    {
        const int nDefaultStreamId = 0;
        public void SendNetData(ushort nPackageId)
        {
            SendNetData(nDefaultStreamId, nPackageId);
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            SendNetData(nDefaultStreamId, nPackageId, data);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            SendNetData(nDefaultStreamId, mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            SendNetData(nDefaultStreamId, nPackageId, buffer);
        }

        public void SendNetData(int nStreamIndex, ushort nPackageId)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                var mBufferSegment = mServerMgr.mCryptoMgr.Encode(nPackageId, ReadOnlySpan<byte>.Empty);
                SendNetStream(nStreamIndex, mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(int nStreamIndex, ushort nPackageId, byte[] data)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                var mBufferSegment = mServerMgr.mCryptoMgr.Encode(nPackageId, data);
                SendNetStream(nStreamIndex, mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(int nStreamIndex, NetPackage mNetPackage)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                var mBufferSegment = mServerMgr.mCryptoMgr.Encode(mNetPackage.GetPackageId(), mNetPackage.GetData());
                SendNetStream(nStreamIndex, mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(int nStreamIndex, ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ResetSendHeartBeatTime();
                var mBufferSegment = mServerMgr.mCryptoMgr.Encode(nPackageId, buffer);
                SendNetStream(nStreamIndex, mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }
    }
}
