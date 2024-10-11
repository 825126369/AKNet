using System;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal interface ServerBase
	{
        void InitNet(string Ip, int nPort);
        void Update(double elapsed);
        void addNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func);
        void removeNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func);
    }
}
