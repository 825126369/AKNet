// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    internal static partial class Interop
    {
#if NET7_0_OR_GREATER
        internal static unsafe partial class Kernel32
        {
            [LibraryImport("kernel32.dll")]
            public static partial IntPtr HeapCreate(uint flOptions, int dwInitialSize, int dwMaximumSize);
            [LibraryImport("kernel32.dll")]
            public static partial bool HeapDestroy(IntPtr hHeap);
            [LibraryImport("kernel32.dll")]
            public static partial void* HeapAlloc(IntPtr hHeap, uint dwFlags, int dwBytes);
            [LibraryImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool HeapFree(IntPtr hHeap, uint dwFlags, void* lpMem);
        }
#else
        internal static unsafe partial class Kernel32
        {
            [DllImport("kernel32.dll")]
            public static extern IntPtr HeapCreate(uint flOptions, int dwInitialSize, int dwMaximumSize);
            [DllImport("kernel32.dll")]
            public static extern bool HeapDestroy(IntPtr hHeap);
            [DllImport("kernel32.dll")]
            public static extern void* HeapAlloc(IntPtr hHeap, uint dwFlags, int dwBytes);
            [DllImport("kernel32.dll")]
            public static extern bool HeapFree(IntPtr hHeap, uint dwFlags, void* lpMem);
        }
#endif
    }
}
