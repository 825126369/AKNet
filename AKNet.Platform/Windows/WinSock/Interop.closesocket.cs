using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static partial int closesocket(IntPtr socketHandle);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static extern int closesocket(IntPtr socketHandle);
#endif
        }
    }
}
