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
