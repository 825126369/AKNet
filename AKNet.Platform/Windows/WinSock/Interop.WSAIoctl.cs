using System.Runtime.InteropServices;
using static AKNet.Platform.Interop.Kernel32;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static unsafe partial int WSAIoctl(
                SafeHandle socketHandle,
                uint ioControlCode,
                void* lpvInBuffer,
                int lpvInBufferLen,
                void* lpvOutBuffer,
                int lpvOutBufferLen,
                out int bytesTransferred,
                void* shouldBeNull,
                void* shouldBeNull2);

            [LibraryImport(Interop.Libraries.Ws2_32, EntryPoint = "WSAIoctl", SetLastError = true)]
            public static partial int WSAIoctl_Blocking(
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
            public static unsafe extern int WSAIoctl(
                SafeHandle socketHandle,
                uint ioControlCode,
                void* lpvInBuffer,
                int lpvInBufferLen,
                void* lpvOutBuffer,
                int lpvOutBufferLen,
                out int bytesTransferred,
                void* shouldBeNull,
                void* shouldBeNull2);

            [DllImport(Interop.Libraries.Ws2_32, EntryPoint = "WSAIoctl", SetLastError = true)]
            public static extern int WSAIoctl_Blocking(
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
