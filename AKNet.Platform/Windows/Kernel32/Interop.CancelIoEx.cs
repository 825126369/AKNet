using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Kernel32
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe partial bool CancelIoEx(SafeHandle handle, OVERLAPPED* lpOverlapped);

            [LibraryImport(Libraries.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe partial bool CancelIoEx(IntPtr handle, OVERLAPPED* lpOverlapped);
#else
            [DllImport(Libraries.Kernel32, SetLastError = true)]
            internal static unsafe extern bool CancelIoEx(SafeHandle handle, OVERLAPPED* lpOverlapped);

            [DllImport(Libraries.Kernel32, SetLastError = true)]
            internal static unsafe extern bool CancelIoEx(IntPtr handle, OVERLAPPED* lpOverlapped);
#endif
        }
    }
}
