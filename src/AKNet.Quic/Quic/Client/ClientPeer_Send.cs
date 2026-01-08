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
namespace AKNet.Quic.Client
{
    internal partial class ClientPeer
    {
        public void SendNetData(byte nStreamIndex, ushort nPackageId)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                var mStreamObj = GetOrCreateSendStreamHandle(nStreamIndex);
                mStreamObj.SendNetData(nPackageId);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(byte nStreamIndex, ushort nPackageId, byte[] data)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                var mStreamObj = GetOrCreateSendStreamHandle(nStreamIndex);
                mStreamObj.SendNetData(nPackageId, data);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(byte nStreamIndex, NetPackage mNetPackage)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                var mStreamObj = GetOrCreateSendStreamHandle(nStreamIndex);
                mStreamObj.SendNetData(mNetPackage);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }

        public void SendNetData(byte nStreamIndex, ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            if (GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                var mStreamObj = GetOrCreateSendStreamHandle(nStreamIndex);
                mStreamObj.SendNetData(nPackageId, buffer);
            }
            else
            {
                NetLog.LogError("SendNetData Failed: " + GetSocketState());
            }
        }
    }
}
