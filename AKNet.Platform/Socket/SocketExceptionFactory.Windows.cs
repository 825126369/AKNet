
#if TARGET_WINDOWS
using System.Net;
using System.Net.Sockets;

namespace AKNet.Socket
{
    internal static partial class SocketExceptionFactory
    {
        public static SocketException CreateSocketException(int socketError, EndPoint endPoint)
        {
            return new SocketException(socketError, CreateMessage(socketError, endPoint));
        }
    }
}
#endif
