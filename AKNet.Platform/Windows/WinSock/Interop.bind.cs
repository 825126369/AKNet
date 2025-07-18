// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        private static partial SocketError bind(
            SafeSocketHandle socketHandle,
            ReadOnlySpan<byte> socketAddress,
            int socketAddressSize);

        internal static SocketError bind(
            SafeSocketHandle socketHandle,
            ReadOnlySpan<byte> socketAddress) => bind(socketHandle, socketAddress, socketAddress.Length);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            private static extern SocketError bind(SafeSocketHandle socketHandle, ReadOnlySpan<byte> socketAddress, int socketAddressSize);
            internal static SocketError bind(SafeSocketHandle socketHandle, ReadOnlySpan<byte> socketAddress) =>
                bind(socketHandle, socketAddress, socketAddress.Length);
#endif
        }
    }
}
