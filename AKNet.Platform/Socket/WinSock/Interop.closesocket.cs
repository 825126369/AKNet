using System;
using System.Runtime.InteropServices;

namespace AKNet.Socket
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static partial SocketError closesocket(IntPtr socketHandle);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static extern SocketError closesocket(IntPtr socketHandle);
#endif
        }
    }
}
