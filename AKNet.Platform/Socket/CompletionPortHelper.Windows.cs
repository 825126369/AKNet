
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    internal static class CompletionPortHelper
    {
        internal static bool SkipCompletionPortOnSuccess(SafeHandle handle)
        {
            return Interop.Kernel32.SetFileCompletionNotificationModes(handle,
                Interop.Kernel32.FileCompletionNotificationModes.SkipCompletionPortOnSuccess |
                Interop.Kernel32.FileCompletionNotificationModes.SkipSetEventOnHandle);
        }
        
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
