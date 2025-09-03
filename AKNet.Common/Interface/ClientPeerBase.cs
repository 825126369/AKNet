﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
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
