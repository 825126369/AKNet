using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
#if NET7_0_OR_GREATER
            // Used with SIOGETEXTENSIONFUNCTIONPOINTER - we're assuming that will never block.
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe partial int WSAIoctl(
                SafeHandle socketHandle,
                int ioControlCode,
                void* guid,
                int guidSize,
                out IntPtr funcPtr,
                int funcPtrSize,
                out int bytesTransferred,
                IntPtr shouldBeNull,
                IntPtr shouldBeNull2);

            [LibraryImport(Interop.Libraries.Ws2_32, EntryPoint = "WSAIoctl", SetLastError = true)]
            internal static partial int WSAIoctl_Blocking(
                SafeHandle socketHandle,
                int ioControlCode,
                byte[]? inBuffer,
                int inBufferSize,
                byte[]? outBuffer,
                int outBufferSize,
                out int bytesTransferred,
                IntPtr overlapped,
                IntPtr completionRoutine);

#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe extern int WSAIoctl(
                SafeHandle socketHandle,
                int ioControlCode,
                void* guid,
                int guidSize,
                out IntPtr funcPtr,
                int funcPtrSize,
                out int bytesTransferred,
                IntPtr shouldBeNull,
                IntPtr shouldBeNull2);

            [DllImport(Interop.Libraries.Ws2_32, EntryPoint = "WSAIoctl", SetLastError = true)]
            internal static extern int WSAIoctl_Blocking(
                SafeHandle socketHandle,
                int ioControlCode,
                byte[]? inBuffer,
                int inBufferSize,
                byte[]? outBuffer,
                int outBufferSize,
                out int bytesTransferred,
                IntPtr overlapped,
                IntPtr completionRoutine);
#endif
        }
    }
}
