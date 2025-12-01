/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
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
            [LibraryImport(Libraries.NtDll)]
            public static partial uint RtlNtStatusToDosError(int Status);
        }
#else
        public static unsafe partial class Kernel32
        {
            [DllImport(Libraries.NtDll)]
            public static extern uint RtlNtStatusToDosError(int Status);
        }
#endif
    }
}
