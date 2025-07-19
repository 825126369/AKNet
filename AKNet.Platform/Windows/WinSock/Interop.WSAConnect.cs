using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        private static partial int WSAConnect(
            IntPtr socketHandle,
            ReadOnlySpan<byte> socketAddress,
            int socketAddressSize,
            IntPtr inBuffer,
            IntPtr outBuffer,
            IntPtr sQOS,
            IntPtr gQOS);

        internal static int WSAConnect(
            IntPtr socketHandle,
            ReadOnlySpan<byte> socketAddress,
            IntPtr inBuffer,
            IntPtr outBuffer,
            IntPtr sQOS,
            IntPtr gQOS) =>
            WSAConnect(socketHandle, socketAddress, socketAddress.Length, inBuffer, outBuffer, sQOS, gQOS);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            private static extern int WSAConnect(IntPtr socketHandle, ReadOnlySpan<byte> socketAddress,
                int socketAddressSize,
                IntPtr inBuffer,
                IntPtr outBuffer,
                IntPtr sQOS,
                IntPtr gQOS);

            internal static int WSAConnect(
                IntPtr socketHandle,
                ReadOnlySpan<byte> socketAddress,
                IntPtr inBuffer,
                IntPtr outBuffer,
                IntPtr sQOS,
                IntPtr gQOS) =>
                WSAConnect(socketHandle, socketAddress, socketAddress.Length, inBuffer, outBuffer, sQOS, gQOS);
#endif
        }
    }
}
