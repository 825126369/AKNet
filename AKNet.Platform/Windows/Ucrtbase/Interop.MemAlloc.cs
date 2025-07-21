// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Ucrtbase
        {
            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            internal static partial void* _aligned_malloc(nuint size, nuint alignment);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            internal static partial void _aligned_free(void* ptr);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            internal static partial void* _aligned_realloc(void* ptr, nuint size, nuint alignment);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            internal static partial void* calloc(nuint num, nuint size);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            internal static partial void free(void* ptr);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            internal static partial void* malloc(int size);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            internal static partial void* realloc(void* ptr, nuint new_size);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            internal static partial void* memset(void* ptr, int c, int n);
        }
#else
        public static unsafe partial class Ucrtbase
        {
            [DllImport(Libraries.Ucrtbase,CallingConvention = CallingConvention.Cdecl)]
            internal static extern void* _aligned_malloc(nuint size, nuint alignment);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _aligned_free(void* ptr);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void* _aligned_realloc(void* ptr, nuint size, nuint alignment);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void* calloc(nuint num, nuint size);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void free(void* ptr);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void* malloc(int size);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void* realloc(void* ptr, nuint new_size);
            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void* memset(void* ptr, int c, int n);
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
        }
#endif
    }
}
