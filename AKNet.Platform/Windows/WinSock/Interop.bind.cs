using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    internal static partial class Interop
    {
        internal static unsafe partial class Winsock
        {
#if NET7_0_OR_GREATER
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        private static partial int bind(
            SafeHandle socketHandle,
            byte* socketAddress,
            int socketAddressSize);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static extern int bind(SafeHandle socketHandle, byte* socketAddress, int socketAddressSize);
#endif
        }
    }
}
