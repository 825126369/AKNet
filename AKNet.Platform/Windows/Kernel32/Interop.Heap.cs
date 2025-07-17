// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    internal static partial class Interop
    {
#if NET7_0_OR_GREATER
        internal static partial class Kernel32
        {
            [LibraryImport("kernel32.dll")]
            public static extern void* HeapAlloc(IntPtr hHeap, uint dwFlags, int dwBytes);
        }
#else
        internal static partial class Kernel32
        {
            [DllImport("kernel32.dll")]
            public static extern void* HeapAlloc(IntPtr hHeap, uint dwFlags, int dwBytes);
        }

#endif
    }
}
