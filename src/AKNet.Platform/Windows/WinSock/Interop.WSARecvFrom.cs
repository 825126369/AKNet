/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using static AKNet.Platform.Interop.Kernel32;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            private static unsafe partial int WSARecvFrom(
                SafeHandle socketHandle,
                WSABUF* buffers,
                int bufferCount,
                out int bytesTransferred,
                ref uint socketFlags,
                IntPtr socketAddressPointer,
                IntPtr socketAddressSizePointer,
                OVERLAPPED* overlapped,
                IntPtr completionRoutine);

#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            private static unsafe extern int WSARecvFrom(
                SafeHandle socketHandle,
                WSABUF* buffers,
                int bufferCount,
                out int bytesTransferred,
                ref uint socketFlags,
                IntPtr socketAddressPointer,
                IntPtr socketAddressSizePointer,
                OVERLAPPED* overlapped,
                IntPtr completionRoutine);
#endif
        }
    }
}
