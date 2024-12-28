using System;
using System.Net.Sockets;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        public static void SendTcpStream(tcp_sock tp, ReadOnlySpan<byte> buffer)
        {

        }

        public static void MultiThreading_ReceiveWaitCheckNetPackage(tcp_sock tp, SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            
        }

    }
}
