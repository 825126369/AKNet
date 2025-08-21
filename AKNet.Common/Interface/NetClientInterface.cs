/************************************Copyright*****************************************
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
    public interface NetClientInterface
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
        IPEndPoint GetIPEndPoint();
        SOCKET_PEER_STATE GetSocketState();
        void SendNetData(ushort nPackageId);
        void SendNetData(ushort nPackageId, byte[] data);
        void SendNetData(NetPackage mNetPackage);
        void SendNetData(UInt16 nPackageId, ReadOnlySpan<byte> buffer);
    }
}
