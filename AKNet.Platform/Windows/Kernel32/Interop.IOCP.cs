/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Kernel32
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Libraries.Kernel32, SetLastError = true)]
            internal static partial IntPtr CreateIoCompletionPort(IntPtr FileHandle, IntPtr ExistingCompletionPort, IntPtr CompletionKey, int NumberOfConcurrentThreads);

            [LibraryImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static partial bool PostQueuedCompletionStatus(IntPtr CompletionPort, uint dwNumberOfBytesTransferred, IntPtr CompletionKey, OVERLAPPED* lpOverlapped);

            [LibraryImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static partial bool GetQueuedCompletionStatus(IntPtr CompletionPort, out uint lpNumberOfBytesTransferred, out IntPtr CompletionKey, OVERLAPPED* lpOverlapped, int dwMilliseconds);

            [LibraryImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe partial bool GetQueuedCompletionStatusEx(
                IntPtr CompletionPort,
                OVERLAPPED_ENTRY* lpCompletionPortEntries,
                int ulCount,
                out int ulNumEntriesRemoved,
                int dwMilliseconds,
                [MarshalAs(UnmanagedType.Bool)] bool fAlertable);
#else
            [DllImport(Libraries.Kernel32, SetLastError = true)]
            internal static extern IntPtr CreateIoCompletionPort(IntPtr FileHandle, IntPtr ExistingCompletionPort, IntPtr CompletionKey, int NumberOfConcurrentThreads);

            [DllImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PostQueuedCompletionStatus(IntPtr CompletionPort, uint dwNumberOfBytesTransferred, IntPtr CompletionKey, OVERLAPPED* lpOverlapped);

            [DllImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetQueuedCompletionStatus(IntPtr CompletionPort, out uint lpNumberOfBytesTransferred, out IntPtr CompletionKey, OVERLAPPED* lpOverlapped, int dwMilliseconds);

            [DllImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe extern bool GetQueuedCompletionStatusEx(
                IntPtr CompletionPort,
                OVERLAPPED_ENTRY* lpCompletionPortEntries,
                int ulCount,
                out int ulNumEntriesRemoved,
                int dwMilliseconds,
                [MarshalAs(UnmanagedType.Bool)] bool fAlertable);
#endif

        }
    }
}
