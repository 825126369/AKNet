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
