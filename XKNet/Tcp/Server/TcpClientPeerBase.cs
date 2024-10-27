using System.Net;
using System.Net.Sockets;

namespace XKNet.Tcp.Server
{
    public interface TcpClientPeerBase
    {
        void SetName(string Name);
        void HandleConnectedSocket(Socket mSocket);
        void Update(double elapsed);
        void Reset();
        IPEndPoint GetIPEndPoint();
    }
}
