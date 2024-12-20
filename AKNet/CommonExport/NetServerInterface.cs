/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
namespace AKNet.Common
{
    public interface NetServerInterface
    {
        void InitNet();
        void InitNet(int nPort);
        void InitNet(string Ip, int nPort);
        int GetPort();
        SOCKET_SERVER_STATE GetServerState();
        void Update(double elapsed);
        void Release();



        void addNetListenFunc(UInt16 id, Action<ClientPeerBase, NetPackage> mFunc);
        void removeNetListenFunc(UInt16 id, Action<ClientPeerBase, NetPackage> mFunc);
        void addNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc);
        void removeNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc);

        void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc);
        void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc);
        void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
        void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
    }
}
