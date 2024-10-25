using System;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal interface ServerBase
	{
        void InitNet();
        void InitNet(int nPort);
        void InitNet(string Ip, int nPort);
        int GetPort();
        SOCKET_SERVER_STATE GetServerState();
        void Update(double elapsed);
        void addNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func);
        void removeNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func);
        void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func);
        void Release();

    }
}
