using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    internal static partial class Interop
    {
#if NET7_0_OR_GREATER
        internal static unsafe partial class Kernel32
        {
            [LibraryImport("kernel32.dll")]
            public static extern bool GlobalMemoryStatusEx(MEMORYSTATUSEX* lpBuffer);
            [LibraryImport("kernel32.dll")]
            public static extern bool GetSystemTimeAdjustment(out int lpTimeAdjustment, out int lpTimeIncrement, out bool lpTimeAdjustmentDisabled);
        }
#else
        internal static unsafe partial class Kernel32
        {
            [DllImport("kernel32.dll")]
            public static extern bool GlobalMemoryStatusEx(MEMORYSTATUSEX* lpBuffer);
            [DllImport("kernel32.dll")]
            public static extern bool GetSystemTimeAdjustment(out int lpTimeAdjustment, out int lpTimeIncrement, out bool lpTimeAdjustmentDisabled);
        }
#endif
    }
}
