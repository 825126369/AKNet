using Google.Protobuf;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;

namespace XKNet.Tcp.Server
{
    public interface ClientPeerBase
    {
#if DEBUG
        void ConnectClient(Socket mSocket);
        void Update(double elapsed);
        void Reset();
        IPEndPoint GetIPEndPoint();
        uint GetUUID();
#endif

        SERVER_SOCKET_PEER_STATE GetSocketState();
        void SendNetData(ushort nPackageId, IMessage data = null);
    }
}
