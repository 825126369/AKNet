using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    internal static partial class Interop
    {
#if NET7_0_OR_GREATER
        internal static unsafe partial class Kernel32
        {
            [LibraryImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial  bool CloseHandle(IntPtr hObject);
        }
#else
        internal static unsafe partial class Kernel32
        {
            [DllImport("kernel32.dll")]
            public static extern bool CloseHandle(IntPtr hObject);
        }
#endif
    }
}
