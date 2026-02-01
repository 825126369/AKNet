/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:04
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
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
