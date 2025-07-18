// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    internal static unsafe partial class Interop
    {
        internal static unsafe partial class Kernel32
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
                public IntPtr lpCompletionKey;
                public OVERLAPPED* lpOverlapped;
                public IntPtr Internal;
                public int dwNumberOfBytesTransferred;
            }

            internal unsafe struct OVERLAPPED
            {
                public IntPtr Internal;
                public IntPtr InternalHigh;

                [StructLayout(LayoutKind.Explicit)]
                public struct DUMMYUNIONNAME_DATA1
                {
                    public struct DUMMYSTRUCTNAME_DATA2
                    {
                        public int Offset;
                        public int OffsetHigh;
                    }
                    [FieldOffset(0)] public DUMMYSTRUCTNAME_DATA2 DUMMYUNIONNAME;
                    [FieldOffset(0)] public void* Pointer;
                }
                public DUMMYUNIONNAME_DATA1 DUMMYUNIONNAME;
                public IntPtr hEvent;
            }
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

            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct OVERLAPPED_ENTRY
            {
                public IntPtr lpCompletionKey;
                public OVERLAPPED* lpOverlapped;
                public IntPtr Internal;
                public int dwNumberOfBytesTransferred;
            }

            internal unsafe struct OVERLAPPED
            {
                public IntPtr Internal;
                public IntPtr InternalHigh;

                [StructLayout(LayoutKind.Explicit)]
                public struct DUMMYUNIONNAME_DATA1
                {
                    public struct DUMMYSTRUCTNAME_DATA2
                    {
                        public int Offset;
                        public int OffsetHigh;
                    }
                    [FieldOffset(0)] public DUMMYSTRUCTNAME_DATA2 DUMMYUNIONNAME;
                    [FieldOffset(0)] public void* Pointer;
                }
                public DUMMYUNIONNAME_DATA1 DUMMYUNIONNAME;
                public IntPtr hEvent;
            }
#endif

        }
    }
}
