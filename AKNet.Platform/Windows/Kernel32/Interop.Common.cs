using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Kernel32
        {
            [LibraryImport(Libraries.Kernel32)]
            [return: MarshalAs(UnmanagedType.U8)]
            public static partial ulong RtlNtStatusToDosError(long Status);
        }
#else
        public static unsafe partial class Kernel32
        {
            [DllImport(Libraries.Kernel32)]
            public static extern ulong RtlNtStatusToDosError(long Status);
        }
#endif
    }
}
