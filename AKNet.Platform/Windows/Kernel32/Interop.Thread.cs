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

using System;
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Kernel32
        {
            //[LibraryImport(Interop.Libraries.Kernel32)]
            //public static partial int GetLastError();

            [LibraryImport(Interop.Libraries.Kernel32)]
            public static partial IntPtr CreateThread(
                   IntPtr lpThreadAttributes,
                   IntPtr dwStackSize,
                   LPTHREAD_START_ROUTINE lpStartAddress,
                   IntPtr lpParameter,
                   uint dwCreationFlags,
                   out int lpThreadId);

            [LibraryImport(Interop.Libraries.Kernel32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static unsafe partial bool GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType,
                SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* Buffer, out int ReturnedLength);

            [LibraryImport(Interop.Libraries.Kernel32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static unsafe partial bool SetThreadGroupAffinity(IntPtr hThread, GROUP_AFFINITY* GroupAffinity, GROUP_AFFINITY* PreviousGroupAffinity);
            [LibraryImport(Interop.Libraries.Kernel32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static unsafe partial bool SetThreadIdealProcessorEx(IntPtr hThread, PROCESSOR_NUMBER* lpIdealProcessor, PROCESSOR_NUMBER* lpPreviousIdealProcessor);
            [LibraryImport(Interop.Libraries.Kernel32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static unsafe partial bool SetThreadPriority(IntPtr hThread, int nPriority);
            [LibraryImport(Interop.Libraries.Kernel32)]
            public static unsafe partial int SetThreadDescription(IntPtr hThread, [MarshalAs(UnmanagedType.LPWStr)] string lpThreadDescription);
            [LibraryImport(Interop.Libraries.Kernel32)]
            public static unsafe partial int WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
            [LibraryImport(Interop.Libraries.Kernel32)]
            public static unsafe partial void Sleep(long dwMilliseconds);
            [LibraryImport(Interop.Libraries.Kernel32)]
            public static unsafe partial IntPtr GetCurrentThread();
            [LibraryImport(Interop.Libraries.Kernel32)]
            public static partial int GetCurrentThreadId();
        }
#else
        public static unsafe partial class Kernel32
        {
            //[DllImport(Interop.Libraries.Kernel32)]
            //public static extern int GetLastError();

            [DllImport(Interop.Libraries.Kernel32)]
            public static extern IntPtr CreateThread(
                   IntPtr lpThreadAttributes,
                   IntPtr dwStackSize,
                   LPTHREAD_START_ROUTINE lpStartAddress,
                   IntPtr lpParameter,
                   uint dwCreationFlags,
                   out int lpThreadId);

            [DllImport(Interop.Libraries.Kernel32)]
            public static unsafe extern bool GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType,
                SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* Buffer, out int ReturnedLength);

            [DllImport(Interop.Libraries.Kernel32)]
            public static unsafe extern bool SetThreadGroupAffinity(IntPtr hThread, GROUP_AFFINITY* GroupAffinity, GROUP_AFFINITY* PreviousGroupAffinity);
            [DllImport(Interop.Libraries.Kernel32)]
            public static unsafe extern bool SetThreadIdealProcessorEx(IntPtr hThread, PROCESSOR_NUMBER* lpIdealProcessor, PROCESSOR_NUMBER* lpPreviousIdealProcessor);
            [DllImport(Interop.Libraries.Kernel32)]
            public static unsafe extern bool SetThreadPriority(IntPtr hThread, int nPriority);
            [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Unicode)]
            public static unsafe extern int SetThreadDescription(IntPtr hThread, string lpThreadDescription);
            [DllImport(Interop.Libraries.Kernel32)]
            public static unsafe extern int WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
            [DllImport(Interop.Libraries.Kernel32)]
            public static unsafe extern void Sleep(long dwMilliseconds);
            [DllImport(Interop.Libraries.Kernel32)]
            public static unsafe extern IntPtr GetCurrentThread();
            [DllImport(Interop.Libraries.Kernel32)]
            public static extern int GetCurrentThreadId();
        }

#endif
    }
}
