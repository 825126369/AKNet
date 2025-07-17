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
            [Flags]
            internal enum FileCompletionNotificationModes : byte
            {
                None = 0,
                SkipCompletionPortOnSuccess = 1,
                SkipSetEventOnHandle = 2
            }

            [LibraryImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static partial bool SetFileCompletionNotificationModes(SafeHandle handle, FileCompletionNotificationModes flags);
        }
#else
        internal static partial class Kernel32
        {
            [Flags]
            internal enum FileCompletionNotificationModes : byte
            {
                None = 0,
                SkipCompletionPortOnSuccess = 1,
                SkipSetEventOnHandle = 2
            }

            [DllImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetFileCompletionNotificationModes(SafeHandle handle, FileCompletionNotificationModes flags);
        }
#endif
    }
}
