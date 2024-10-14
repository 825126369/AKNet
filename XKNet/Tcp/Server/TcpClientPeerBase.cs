using System.Net;
using System.Net.Sockets;

namespace XKNet.Tcp.Server
{
    public interface TcpClientPeerBase
    {
#if DEBUG
        void ConnectClient(Socket mSocket);
        void Update(double elapsed);
        void Reset();
        IPEndPoint GetIPEndPoint();
        uint GetUUID();
#endif
    }
}
