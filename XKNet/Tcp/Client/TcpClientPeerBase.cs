using System;
using XKNet.Common;
namespace XKNet.Tcp.Client
{
    public interface TcpClientPeerBase
    {
        void SetName(string Name);
        void ConnectServer(string Ip, int nPort);
        bool DisConnectServer();
        void ReConnectServer();
        void Update(double elapsed);
        void Reset();
        void Release();
        void addNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
        void removeNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
        void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func);
        void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
        void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
    }
}
