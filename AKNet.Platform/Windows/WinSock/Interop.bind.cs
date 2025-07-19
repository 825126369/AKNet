using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        private static partial SocketError bind(
            IntPtr socketHandle,
            ReadOnlySpan<byte> socketAddress,
            int socketAddressSize);

        internal static SocketError bind(
            IntPtr socketHandle,
            ReadOnlySpan<byte> socketAddress) => bind(socketHandle, socketAddress, socketAddress.Length);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            private static extern int bind(IntPtr socketHandle, ReadOnlySpan<byte> socketAddress, int socketAddressSize);
            internal static int bind(IntPtr socketHandle, ReadOnlySpan<byte> socketAddress) =>
                bind(socketHandle, socketAddress, socketAddress.Length);

#endif
        }
    }
}
