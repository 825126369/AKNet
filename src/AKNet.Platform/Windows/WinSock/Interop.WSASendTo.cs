/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:21
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
            private static unsafe partial int WSASendTo(
                IntPtr socketHandle,
                WSABUF* buffers,
                int bufferCount,
                out int bytesTransferred,
                uint socketFlags,
                ReadOnlySpan<byte> socketAddress,
                int socketAddressSize,
                Overlapped* overlapped,
                IntPtr completionRoutine);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            private static unsafe extern int WSASendTo(
                IntPtr socketHandle,
                WSABUF* buffers,
                int bufferCount,
                out int bytesTransferred,
                uint socketFlags,
                ReadOnlySpan<byte> socketAddress,
                int socketAddressSize,
                Overlapped* overlapped,
                IntPtr completionRoutine);
#endif
        }
    }
}
