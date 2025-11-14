/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:28
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Client
{
    internal interface UdpClientPeerBase
    {
        void ConnectServer(string Ip, int nPort);
        bool DisConnectServer();
        void ReConnectServer();
        void Update(double elapsed);
        void Release();

        void addNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> mFunc);
        void removeNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> mFunc);
        void addNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc);
        void removeNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc);

        void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc);
        void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc);
        void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
        void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
    }
}
