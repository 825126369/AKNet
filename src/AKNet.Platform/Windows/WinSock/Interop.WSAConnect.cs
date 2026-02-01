/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:05
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static partial int WSAConnect(
                SafeHandle socketHandle,
                ReadOnlySpan<byte> socketAddress,
                int socketAddressSize,
                IntPtr inBuffer,
                IntPtr outBuffer,
                IntPtr sQOS,
                IntPtr gQOS);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static extern int WSAConnect(
                SafeHandle socketHandle, 
                ReadOnlySpan<byte> socketAddress,
                int socketAddressSize,
                IntPtr inBuffer,
                IntPtr outBuffer,
                IntPtr sQOS,
                IntPtr gQOS);
#endif
        }
    }
}
