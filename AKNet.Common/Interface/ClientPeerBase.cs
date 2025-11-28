/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;

namespace AKNet.Common
{
    public interface ClientPeerBase
    {
        IPEndPoint GetIPEndPoint();
        SOCKET_PEER_STATE GetSocketState();
        void SendNetData(ushort nPackageId);
        void SendNetData(ushort nPackageId, byte[] data);
        void SendNetData(UInt16 nPackageId, ReadOnlySpan<byte> buffer);
        void SendNetData(NetPackage mNetPackage);
        void SetName(string name);
        string GetName();
        void SetID(uint id);
        uint GetID();
    }
}
