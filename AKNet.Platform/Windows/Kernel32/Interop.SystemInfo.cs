using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Kernel32
        {
            [LibraryImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool GlobalMemoryStatusEx(MEMORYSTATUSEX* lpBuffer);
            [LibraryImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool GetSystemTimeAdjustment(out int lpTimeAdjustment, 
                out int lpTimeIncrement, 
                [MarshalAs(UnmanagedType.Bool)] out bool lpTimeAdjustmentDisabled);
        }
#else
        public static unsafe partial class Kernel32
        {
            [DllImport("kernel32.dll")]
            public static extern bool GlobalMemoryStatusEx(MEMORYSTATUSEX* lpBuffer);
            [DllImport("kernel32.dll")]
            public static extern bool GetSystemTimeAdjustment(out int lpTimeAdjustment, out int lpTimeIncrement, out bool lpTimeAdjustmentDisabled);
        }
#endif
    }
}
