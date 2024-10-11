using Google.Protobuf;
using System;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    public interface ClientPeerBase
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
        CLIENT_SOCKET_PEER_STATE GetSocketState();
        void SendNetData(ushort nPackageId, IMessage data = null);
        void SendLuaNetData(ushort nPackageId, byte[] buffer = null);
    }
}
