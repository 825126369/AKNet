
using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            private static unsafe partial int WSASendTo(
                IntPtr socketHandle,
                WSABUF* buffers,
                int bufferCount,
                out int bytesTransferred,
                uint socketFlags,
                ReadOnlySpan<byte> socketAddress,
                int socketAddressSize,
                Overlapped* overlapped,
                IntPtr completionRoutine);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            private static unsafe extern int WSASendTo(
                IntPtr socketHandle,
                WSABUF* buffers,
                int bufferCount,
                out int bytesTransferred,
                uint socketFlags,
                ReadOnlySpan<byte> socketAddress,
                int socketAddressSize,
                Overlapped* overlapped,
                IntPtr completionRoutine);
#endif
        }
    }
}
