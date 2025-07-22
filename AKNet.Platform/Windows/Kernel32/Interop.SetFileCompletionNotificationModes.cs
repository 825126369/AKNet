using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Kernel32
        {
            [LibraryImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool SetFileCompletionNotificationModes(SafeHandle handle, byte flags);
        }
#else
        public static unsafe partial class Kernel32
        {
            [DllImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetFileCompletionNotificationModes(SafeHandle handle, byte flags);
        }
#endif
    }
}
