/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:39
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;

namespace AKNet.Common
{
    public interface ClientPeerBase
    {
        string GetName();
        string GetIPAddress();
        SOCKET_PEER_STATE GetSocketState();
        void SendNetData(ushort nPackageId);
        void SendNetData(ushort nPackageId, IMessage data);
        void SendNetData(ushort nPackageId, byte[] data);
        void SendNetData(NetPackage mNetPackage);
        void SendNetData(UInt16 nPackageId, ReadOnlySpan<byte> buffer);
    }
}
