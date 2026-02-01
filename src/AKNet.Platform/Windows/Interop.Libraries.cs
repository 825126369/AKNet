/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:04
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static class Libraries
        {
            internal const string Kernel32 = "kernel32.dll";
            internal const string Ucrtbase = "ucrtbase.dll";
            internal const string Ws2_32 = "ws2_32.dll";
            internal const string BCrypt = "BCrypt.dll";
            internal const string NtDll = "ntdll.dll";
        }
    }
}
