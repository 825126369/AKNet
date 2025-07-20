
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

            internal static unsafe int WSASendTo(
                IntPtr socketHandle,
                ref WSABUF buffer,
                int bufferCount,
                out int bytesTransferred,
                uint socketFlags,
                ReadOnlySpan<byte> socketAddress,
                Overlapped* overlapped,
                IntPtr completionRoutine)
            {
                WSABUF localBuffer = buffer;
                return WSASendTo(socketHandle, &localBuffer, bufferCount, out bytesTransferred, socketFlags, socketAddress, socketAddress.Length, overlapped, completionRoutine);
            }

            internal static unsafe int WSASendTo(
                IntPtr socketHandle,
                WSABUF[] buffers,
                int bufferCount,
                [Out] out int bytesTransferred,
                uint socketFlags,
                ReadOnlySpan<byte> socketAddress,
                Overlapped* overlapped,
                IntPtr completionRoutine)
            {
                NetLog.Assert(buffers != null && buffers.Length > 0);
                fixed (WSABUF* buffersPtr = &buffers[0])
                {
                    return WSASendTo(socketHandle, buffersPtr, bufferCount, out bytesTransferred, socketFlags, socketAddress, socketAddress.Length, overlapped, completionRoutine);
                }
            }
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

            internal static unsafe int WSASendTo(
                IntPtr socketHandle,
                ref WSABUF buffer,
                int bufferCount,
                out int bytesTransferred,
                uint socketFlags,
                ReadOnlySpan<byte> socketAddress,
                Overlapped* overlapped,
                IntPtr completionRoutine)
            {
                WSABUF localBuffer = buffer;
                return WSASendTo(socketHandle, &localBuffer, bufferCount, out bytesTransferred, socketFlags, socketAddress, socketAddress.Length, overlapped, completionRoutine);
            }

            internal static unsafe int WSASendTo(
                IntPtr socketHandle,
                WSABUF[] buffers,
                int bufferCount,
                [Out] out int bytesTransferred,
                uint socketFlags,
                ReadOnlySpan<byte> socketAddress,
                Overlapped* overlapped,
                IntPtr completionRoutine)
            {
                NetLog.Assert(buffers != null && buffers.Length > 0);
                fixed (WSABUF* buffersPtr = &buffers[0])
                {
                    return WSASendTo(socketHandle, buffersPtr, bufferCount, out bytesTransferred, socketFlags, socketAddress, socketAddress.Length, overlapped, completionRoutine);
                }
            }
#endif
        }
    }
}
