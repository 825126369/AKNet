using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Kernel32
        {
            [LibraryImport(Libraries.Kernel32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool CloseHandle(IntPtr hObject);
        }
#else
        public static unsafe partial class Kernel32
        {
            [DllImport(Libraries.Kernel32)]
            public static extern bool CloseHandle(IntPtr hObject);
        }
#endif
    }
}
