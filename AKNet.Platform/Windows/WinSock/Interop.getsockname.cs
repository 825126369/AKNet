using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe partial SocketError getsockname(
                SafeSocketHandle socketHandle,
                byte* socketAddress,
                out int socketAddressSize);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe extern SocketError getsockname(SafeSocketHandle socketHandle, byte* socketAddress, out int socketAddressSize);
#endif
        }
    }
}
