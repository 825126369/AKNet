/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Client
{
    public interface UdpClientPeerBase
    {
        void ConnectServer(string Ip, ushort nPort);
        bool DisConnectServer();
        void ReConnectServer();
        void Update(double elapsed);
        void Release();
        void addNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
        void removeNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
        void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
        void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
        void SetName(string name);
    }
}
