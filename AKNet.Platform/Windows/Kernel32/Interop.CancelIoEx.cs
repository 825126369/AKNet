// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace AKNet.Socket
{
    internal static partial class Interop
    {
        internal static partial class Kernel32
        {
#if NET7_0_OR_GREATER
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CancelIoEx(SafeHandle handle, NativeOverlapped* lpOverlapped);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CancelIoEx(IntPtr handle, NativeOverlapped* lpOverlapped);
#else
            [DllImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe extern bool CancelIoEx(SafeHandle handle, NativeOverlapped* lpOverlapped);

            [DllImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe extern bool CancelIoEx(IntPtr handle, NativeOverlapped* lpOverlapped);
#endif
        }
    }
}
