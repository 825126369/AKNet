/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:14
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Net;
namespace AKNet.Common
{
    public interface QuicClientInterface
    {
        void ConnectServer(string Ip, int nPort);
        bool DisConnectServer();
        void ReConnectServer();
        void Update(double elapsed);
        void Release();
        
        void addNetListenFunc(ushort nPackageId, Action<QuicClientPeerBase, QuicNetPackage> mFunc);
        void removeNetListenFunc(ushort nPackageId, Action<QuicClientPeerBase, QuicNetPackage> mFunc);
        void addNetListenFunc(Action<QuicClientPeerBase, QuicNetPackage> mFunc);
        void removeNetListenFunc(Action<QuicClientPeerBase, QuicNetPackage> mFunc);

        void addListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc);
        void removeListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc);
        void addListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc);
        void removeListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc);
        IPEndPoint GetIPEndPoint();
        SOCKET_PEER_STATE GetSocketState();
        
        void SendNetData(byte nStreamIndex, ushort nPackageId);
        void SendNetData(byte nStreamIndex, ushort nPackageId, byte[] data);
        void SendNetData(byte nStreamIndex, ushort nPackageId, ReadOnlySpan<byte> buffer);
        void SendNetData(byte nStreamIndex, NetPackage mNetPackage);

        void SetName(string name);
        string GetName();
        void SetID(uint id);
        uint GetID();
    }
}
