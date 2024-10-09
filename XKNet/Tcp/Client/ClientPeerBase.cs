using Google.Protobuf;
using System;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Client
{
    public interface ClientPeerBase
    {
        void ConnectServer(string Ip, ushort nPort);
        bool DisConnectServer();
        void ReConnectServer();
        CLIENT_SOCKET_PEER_STATE GetSocketState();
        void Update(double elapsed);
        void SendNetData(ushort nPackageId, IMessage data = null);
        void SendLuaNetData(ushort nPackageId, byte[] buffer = null);
        void Reset();
        void Release();
        void addNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
        void removeNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun);
    }
}
