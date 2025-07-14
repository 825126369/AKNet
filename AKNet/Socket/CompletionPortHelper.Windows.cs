using AKNet.Socket.Windows;
using System;
using System.Runtime.InteropServices;
namespace AKNet.Socket
{
    internal static class CompletionPortHelper
    {
        internal static bool SkipCompletionPortOnSuccess(SafeHandle handle)
        {
            return Kernel32.SetFileCompletionNotificationModes(handle,
                Kernel32.FileCompletionNotificationModes.SkipCompletionPortOnSuccess |
                Kernel32.FileCompletionNotificationModes.SkipSetEventOnHandle);
        }

        // There's a bug with using SetFileCompletionNotificationModes with UDP on Windows 7 and before.
        // This check tells us if the problem exists on the platform we're running on.
        internal static readonly bool PlatformHasUdpIssue = CheckIfPlatformHasUdpIssue();

        private static bool CheckIfPlatformHasUdpIssue()
        {
            Version osVersion = Environment.OSVersion.Version;

            // 6.1 == Windows 7
            return (osVersion.Major < 6 ||
                    (osVersion.Major == 6 && osVersion.Minor <= 1));
        }
    }
}
