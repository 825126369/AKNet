using System;
using XKNet.Common;

namespace XKNet.Tcp.Server
{
    internal interface ServerBase
	{
        void InitNet(string Ip, int nPort);
        void Update(double elapsed);
        void addNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func);
        void removeNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func);
    }
}
