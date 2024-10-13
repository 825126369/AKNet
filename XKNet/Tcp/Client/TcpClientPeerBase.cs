using System;
using XKNet.Common;
namespace XKNet.Tcp.Client
{
    public interface TcpClientPeerBase
    {
#if DEBUG
        void ConnectServer(string Ip, ushort nPort);
        bool DisConnectServer();
        void ReConnectServer();
        void Update(double elapsed);
        void Reset();
        void Release();
        void addNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
        void removeNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
#endif
    }
}
