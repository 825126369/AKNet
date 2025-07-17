// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    internal static partial class Interop
    {
#if NET7_0_OR_GREATER
        internal static partial class Kernel32
    {
        [LibraryImport("kernel32.dll")]
        public static extern int GetLastError();

        [LibraryImport("kernel32.dll")]
        public static extern IntPtr CreateThread(
               IntPtr lpThreadAttributes,
               IntPtr dwStackSize,
               LPTHREAD_START_ROUTINE lpStartAddress,
               IntPtr lpParameter,
               uint dwCreationFlags,
               out int lpThreadId);

        public delegate uint LPTHREAD_START_ROUTINE(IntPtr lpParameter);

        [LibraryImport("kernel32.dll")]
        public static unsafe extern bool GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType,
            SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* Buffer, out int ReturnedLength);
    }
#else
        internal static partial class Kernel32
        {
            [DllImport("kernel32.dll")]
            public static extern int GetLastError();

            [DllImport("kernel32.dll"]
            public static extern IntPtr CreateThread(
                   IntPtr lpThreadAttributes,
                   IntPtr dwStackSize,
                   LPTHREAD_START_ROUTINE lpStartAddress,
                   IntPtr lpParameter,
                   uint dwCreationFlags,
                   out int lpThreadId);

            [DllImport("kernel32.dll")]
            public static unsafe extern bool GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType,
                SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* Buffer, out int ReturnedLength);
        }

#endif
    }
}
