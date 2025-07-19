// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            private static unsafe partial int WSARecvFrom(
                SafeHandle socketHandle,
                WSABUF* buffers,
                int bufferCount,
                out int bytesTransferred,
                ref uint socketFlags,
                IntPtr socketAddressPointer,
                IntPtr socketAddressSizePointer,
                NativeOverlapped* overlapped,
                IntPtr completionRoutine);

            internal static unsafe int WSARecvFrom(
                SafeHandle socketHandle,
                ref WSABUF buffer,
                int bufferCount,
                out int bytesTransferred,
                ref uint socketFlags,
                IntPtr socketAddressPointer,
                IntPtr socketAddressSizePointer,
                NativeOverlapped* overlapped,
                IntPtr completionRoutine)
            {
                // We intentionally do NOT copy this back after the function completes:
                // We don't want to cause a race in async scenarios.
                // The WSABuffer struct should be unchanged anyway.
                WSABUF localBuffer = buffer;
                return WSARecvFrom(socketHandle, &localBuffer, bufferCount, out bytesTransferred, ref socketFlags, socketAddressPointer, socketAddressSizePointer, overlapped, completionRoutine);
            }

            internal static unsafe int WSARecvFrom(
                SafeHandle socketHandle,
                WSABUF[] buffers,
                int bufferCount,
                out int bytesTransferred,
                ref uint socketFlags,
                IntPtr socketAddressPointer,
                IntPtr socketAddressSizePointer,
                NativeOverlapped* overlapped,
                IntPtr completionRoutine)
            {
                Debug.Assert(buffers != null && buffers.Length > 0);
                fixed (WSABUF* buffersPtr = &buffers[0])
                {
                    return WSARecvFrom(socketHandle, buffersPtr, bufferCount, out bytesTransferred, ref socketFlags, socketAddressPointer, socketAddressSizePointer, overlapped, completionRoutine);
                }
            }

#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            private static unsafe extern int WSARecvFrom(
                SafeHandle socketHandle,
                WSABUF* buffers,
                int bufferCount,
                out int bytesTransferred,
                ref uint socketFlags,
                IntPtr socketAddressPointer,
                IntPtr socketAddressSizePointer,
                NativeOverlapped* overlapped,
                IntPtr completionRoutine);

            internal static unsafe int WSARecvFrom(
                SafeHandle socketHandle,
                ref WSABUF buffer,
                int bufferCount,
                out int bytesTransferred,
                ref uint socketFlags,
                IntPtr socketAddressPointer,
                IntPtr socketAddressSizePointer,
                NativeOverlapped* overlapped,
                IntPtr completionRoutine)
            {
                WSABUF localBuffer = buffer;
                return WSARecvFrom(socketHandle, &localBuffer, bufferCount, out bytesTransferred, ref socketFlags, socketAddressPointer, socketAddressSizePointer, overlapped, completionRoutine);
            }

            internal static unsafe int WSARecvFrom(
                SafeHandle socketHandle,
                WSABUF[] buffers,
                int bufferCount,
                out int bytesTransferred,
                ref uint socketFlags,
                IntPtr socketAddressPointer,
                IntPtr socketAddressSizePointer,
                NativeOverlapped* overlapped,
                IntPtr completionRoutine)
            {
                Debug.Assert(buffers != null && buffers.Length > 0);
                fixed (WSABUF* buffersPtr = &buffers[0])
                {
                    return WSARecvFrom(socketHandle, buffersPtr, bufferCount, out bytesTransferred, ref socketFlags, socketAddressPointer, socketAddressSizePointer, overlapped, completionRoutine);
                }
            }
#endif
        }
    }
}
