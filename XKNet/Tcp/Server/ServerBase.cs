using System;
using XKNet.Common;

namespace XKNet.Tcp.Server
{
    internal interface ServerBase
	{
        void InitNet(string Ip, int nPort);
        int GetPort();
        SOCKET_SERVER_STATE GetServerState();
        void Update(double elapsed);

        void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func);
        void addNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func);
        void removeNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func);
    }
}
