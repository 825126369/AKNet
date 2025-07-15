using System.Net.Sockets;
using System.Runtime.InteropServices;
using static Interop.Winsock;

namespace AKNet.Socket
{
    internal static partial class Interop
    {
#if NET7_0_OR_GREATER
        internal static partial class Winsock
        {
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe partial SocketError getsockopt(
                SafeSocketHandle socketHandle,
                SocketOptionLevel optionLevel,
                SocketOptionName optionName,
                byte* optionValue,
                ref int optionLength);

            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static partial SocketError getsockopt(
                SafeSocketHandle socketHandle,
                SocketOptionLevel optionLevel,
                SocketOptionName optionName,
                out Linger optionValue,
                ref int optionLength);

            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static partial SocketError getsockopt(
                SafeSocketHandle socketHandle,
                SocketOptionLevel optionLevel,
                SocketOptionName optionName,
                out IPMulticastRequest optionValue,
                ref int optionLength);

            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static partial SocketError getsockopt(
                SafeSocketHandle socketHandle,
                SocketOptionLevel optionLevel,
                SocketOptionName optionName,
                out IPv6MulticastRequest optionValue,
                ref int optionLength);
        }
#else
        internal static partial class Winsock
        {
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe extern SocketError getsockopt(
                SafeSocketHandle socketHandle,
                SocketOptionLevel optionLevel,
                SocketOptionName optionName,
                byte* optionValue,
                ref int optionLength);

            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static extern SocketError getsockopt(
                SafeSocketHandle socketHandle,
                SocketOptionLevel optionLevel,
                SocketOptionName optionName,
                out Linger optionValue,
                ref int optionLength);

            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static extern SocketError getsockopt(
                SafeSocketHandle socketHandle,
                SocketOptionLevel optionLevel,
                SocketOptionName optionName,
                out IPMulticastRequest optionValue,
                ref int optionLength);

            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static extern SocketError getsockopt(
                SafeSocketHandle socketHandle,
                SocketOptionLevel optionLevel,
                SocketOptionName optionName,
                out IPv6MulticastRequest optionValue,
                ref int optionLength);
        }
#endif
    }
}
