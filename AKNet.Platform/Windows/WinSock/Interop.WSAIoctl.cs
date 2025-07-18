using System;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
            // Used with SIOGETEXTENSIONFUNCTIONPOINTER - we're assuming that will never block.
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static partial SocketError WSAIoctl(
                SafeSocketHandle socketHandle,
                int ioControlCode,
                ref Guid guid,
                int guidSize,
                out IntPtr funcPtr,
                int funcPtrSize,
                out int bytesTransferred,
                IntPtr shouldBeNull,
                IntPtr shouldBeNull2);

            [LibraryImport(Interop.Libraries.Ws2_32, EntryPoint = "WSAIoctl", SetLastError = true)]
            internal static partial SocketError WSAIoctl_Blocking(
                SafeSocketHandle socketHandle,
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
            internal static extern SocketError WSAIoctl(
                SafeSocketHandle socketHandle,
                int ioControlCode,
                ref Guid guid,
                int guidSize,
                out IntPtr funcPtr,
                int funcPtrSize,
                out int bytesTransferred,
                IntPtr shouldBeNull,
                IntPtr shouldBeNull2);

            [DllImport(Interop.Libraries.Ws2_32, EntryPoint = "WSAIoctl", SetLastError = true)]
            internal static extern SocketError WSAIoctl_Blocking(
                SafeSocketHandle socketHandle,
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
