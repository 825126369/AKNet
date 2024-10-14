using System;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    public interface UdpClientPeerBase
    {
#if DEBUG
        void ConnectServer(string Ip, ushort nPort);
        bool DisConnectServer();
        void ReConnectServer();
        void Update(double elapsed);
        void Release();
        void addNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
        void removeNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
#endif
    }
}
