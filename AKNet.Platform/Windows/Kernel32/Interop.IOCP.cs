// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    internal static partial class Interop
    {
        internal static partial class Kernel32
        {
#if NET7_0_OR_GREATER
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial IntPtr CreateIoCompletionPort(IntPtr FileHandle, IntPtr ExistingCompletionPort, IntPtr CompletionKey, int NumberOfConcurrentThreads);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool PostQueuedCompletionStatus(IntPtr CompletionPort, uint dwNumberOfBytesTransferred, IntPtr CompletionKey, IntPtr lpOverlapped);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetQueuedCompletionStatus(IntPtr CompletionPort, out uint lpNumberOfBytesTransferred, out IntPtr CompletionKey, out IntPtr lpOverlapped, int dwMilliseconds);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetQueuedCompletionStatusEx(
            IntPtr CompletionPort,
            OVERLAPPED_ENTRY* lpCompletionPortEntries,
            int ulCount,
            out int ulNumEntriesRemoved,
            int dwMilliseconds,
            [MarshalAs(UnmanagedType.Bool)] bool fAlertable);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct OVERLAPPED_ENTRY
        {
            public UIntPtr lpCompletionKey;
            public NativeOverlapped* lpOverlapped;
            public UIntPtr Internal;
            public uint dwNumberOfBytesTransferred;
        }
#else
            [DllImport(Libraries.Kernel32, SetLastError = true)]
            internal static extern IntPtr CreateIoCompletionPort(IntPtr FileHandle, IntPtr ExistingCompletionPort, IntPtr CompletionKey, int NumberOfConcurrentThreads);

            [DllImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PostQueuedCompletionStatus(IntPtr CompletionPort, uint dwNumberOfBytesTransferred, IntPtr CompletionKey, IntPtr lpOverlapped);

            [DllImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetQueuedCompletionStatus(IntPtr CompletionPort, out uint lpNumberOfBytesTransferred, out IntPtr CompletionKey, out IntPtr lpOverlapped, int dwMilliseconds);

            [DllImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe extern bool GetQueuedCompletionStatusEx(
                IntPtr CompletionPort,
                OVERLAPPED_ENTRY* lpCompletionPortEntries,
                int ulCount,
                out int ulNumEntriesRemoved,
                int dwMilliseconds,
                [MarshalAs(UnmanagedType.Bool)] bool fAlertable);

            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct OVERLAPPED_ENTRY
            {
                public UIntPtr lpCompletionKey;
                public NativeOverlapped* lpOverlapped;
                public UIntPtr Internal;
                public uint dwNumberOfBytesTransferred;
            }

//            internal struct OVERLAPPED
//            {
//                public ulong* Internal;
//                public ulong* InternalHigh;
//                union {
//                    struct {
//                                DWORD Offset;
//                                DWORD OffsetHigh;
//                     }
//                 DUMMYSTRUCTNAME;
//                 PVOID Pointer;
//                }
//                DUMMYUNIONNAME;

//                HANDLE hEvent;
//                    }
//        OVERLAPPED, *LPOVERLAPPED;
//#endif
//        }
    }
}
