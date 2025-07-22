using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static unsafe partial int getsockname(
                SafeHandle socketHandle,
                byte* socketAddress,
                out int socketAddressSize);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static unsafe extern int getsockname(SafeHandle socketHandle, byte* socketAddress, out int socketAddressSize);
#endif
        }
    }
}
