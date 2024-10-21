using Google.Protobuf;
using System.Net;

namespace XKNet.Common
{
    public interface ClientPeerBase
    {
        string GetIPAddress();
        SOCKET_PEER_STATE GetSocketState();
        void SendNetData(ushort nPackageId);
        void SendNetData(ushort nPackageId, IMessage data);
        void SendNetData(ushort nPackageId, byte[] data);
        void SendNetData(NetPackage mNetPackage);
    }
}
