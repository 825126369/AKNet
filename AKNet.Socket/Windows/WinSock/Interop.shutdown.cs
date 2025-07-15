using System.Runtime.InteropServices;

namespace AKNet.Socket
{
    internal static partial class Interop
    {
#if NET7_0_OR_GREATER
        internal static partial class Winsock
        {
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static partial SocketError shutdown(
                SafeSocketHandle socketHandle,
                int how);
        }
#else
        internal static partial class Winsock
        {
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static extern SocketError shutdown(SafeSocketHandle socketHandle, int how);
        }
#endif
    }
}
