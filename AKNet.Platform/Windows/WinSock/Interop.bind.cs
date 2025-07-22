using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static partial int bind(SafeHandle socketHandle, byte* socketAddress, int socketAddressSize);
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static partial int connect(SafeHandle s, byte* name, int namelen);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static extern int bind(SafeHandle socketHandle, byte* socketAddress, int socketAddressSize);
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static extern int connect(SafeHandle s, byte* name, int namelen);
#endif
        }
    }
}
