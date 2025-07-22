using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static partial int WSAConnect(
                SafeHandle socketHandle,
                ReadOnlySpan<byte> socketAddress,
                int socketAddressSize,
                IntPtr inBuffer,
                IntPtr outBuffer,
                IntPtr sQOS,
                IntPtr gQOS);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static extern int WSAConnect(
                SafeHandle socketHandle, 
                ReadOnlySpan<byte> socketAddress,
                int socketAddressSize,
                IntPtr inBuffer,
                IntPtr outBuffer,
                IntPtr sQOS,
                IntPtr gQOS);
#endif
        }
    }
}
