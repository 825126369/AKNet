using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Winsock
        {
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static partial int shutdown(SafeHandle socketHandle, int how);
        }
#else
        public static unsafe partial class Winsock
        {
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static extern int shutdown(SafeHandle socketHandle, int how);
        }
#endif
    }
}
