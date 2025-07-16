using System.Net;
using System.Runtime.InteropServices;
namespace AKNet.Socket
{
    internal static partial class SocketExceptionFactory
    {
        private static string CreateMessage(int nativeSocketError, EndPoint endPoint)
        {
            return Marshal.GetLastWin32Error() + " " + endPoint.ToString();
        }
    }
}
