/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
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
            public static partial void* _aligned_malloc(nuint size, nuint alignment);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void _aligned_free(void* ptr);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void* _aligned_realloc(void* ptr, nuint size, nuint alignment);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void* calloc(nuint num, nuint size);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void free(void* ptr);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void* malloc(int size);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void* realloc(void* ptr, nuint new_size);

            [LibraryImport(Libraries.Ucrtbase)]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void* memset(void* ptr, int c, int n);

            [LibraryImport(Libraries.Ucrtbase)]
            public static partial int memcmp(void* s1, void* s2, int n);
        }
#else
        public static unsafe partial class Ucrtbase
        {
            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void* _aligned_malloc(nuint size, nuint alignment);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            public static extern void _aligned_free(void* ptr);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            public static extern void* _aligned_realloc(void* ptr, nuint size, nuint alignment);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            public static extern void* calloc(nuint num, nuint size);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            public static extern void free(void* ptr);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            public static extern void* malloc(int size);

            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            public static extern void* realloc(void* ptr, nuint new_size);
            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            public static extern void* memset(void* ptr, int c, int n);
            [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl)]
            public static extern int memcmp(void* s1, void* s2, int n);
        }
#endif
    }
}
