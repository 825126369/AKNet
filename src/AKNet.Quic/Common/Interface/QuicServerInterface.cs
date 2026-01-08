/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:14
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    public interface QuicServerInterface
    {
        void InitNet();
        void InitNet(int nPort);
        void InitNet(string Ip, int nPort);
        int GetPort();
        SOCKET_SERVER_STATE GetServerState();
        void Update(double elapsed);
        void Release();
        
        void addNetListenFunc(ushort id, Action<QuicClientPeerBase, QuicNetPackage> mFunc);
        void removeNetListenFunc(ushort id, Action<QuicClientPeerBase, QuicNetPackage> mFunc);
        void addNetListenFunc(Action<QuicClientPeerBase, QuicNetPackage> mFunc);
        void removeNetListenFunc(Action<QuicClientPeerBase, QuicNetPackage> mFunc);

        void addListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc);
        void removeListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc);
        void addListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc);
        void removeListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc);
    }
}
