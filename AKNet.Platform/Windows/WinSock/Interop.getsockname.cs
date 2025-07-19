using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe partial int getsockname(
                IntPtr socketHandle,
                byte* socketAddress,
                out int socketAddressSize);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe extern int getsockname(IntPtr socketHandle, byte* socketAddress, out int socketAddressSize);
#endif
        }
    }
}
