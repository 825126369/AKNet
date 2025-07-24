/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;
namespace AKNet.Udp2MSQuic.Client
{
    public interface QuicClientPeerBase
    {
        void ConnectServer(string Ip, int nPort);
        bool DisConnectServer();
        void ReConnectServer();
        void Update(double elapsed);
        void Reset();
        void Release();

        void addNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
        void removeNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
        void addNetListenFunc(Action<ClientPeerBase, NetPackage> func);
        void removeNetListenFunc(Action<ClientPeerBase, NetPackage> func);

        void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc);
        void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc);
        void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
        void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
    }
}
