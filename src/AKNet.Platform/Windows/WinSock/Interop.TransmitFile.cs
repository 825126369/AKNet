/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:05
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        //internal static partial class Mswsock
        //{
        //    [LibraryImport(Interop.Libraries.Mswsock, SetLastError = true)]
        //    [return: MarshalAs(UnmanagedType.Bool)]
        //    internal static unsafe partial bool TransmitFile(
        //        SafeHandle socket,
        //        IntPtr fileHandle,
        //        int numberOfBytesToWrite,
        //        int numberOfBytesPerSend,
        //        NativeOverlapped* overlapped,
        //        TransmitFileBuffers* buffers,
        //        TransmitFileOptions flags);

        //    [StructLayout(LayoutKind.Sequential)]
        //    internal struct TransmitFileBuffers
        //    {
        //        internal IntPtr Head;
        //        internal int HeadLength;
        //        internal IntPtr Tail;
        //        internal int TailLength;
        //    }
        //}
    }
}
