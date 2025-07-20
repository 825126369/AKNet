// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using static AKNet.Platform.Interop.Kernel32;

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
                OVERLAPPED* overlapped,
                IntPtr completionRoutine);

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
                OVERLAPPED* overlapped,
                IntPtr completionRoutine);
#endif
        }
    }
}
