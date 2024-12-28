using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        Dictionary<int, Action<object>> mEventDic = new Dictionary<int, Action<object>>();

        public static void SendTcpStream(tcp_sock tp, ReadOnlySpan<byte> buffer)
        {

        }

        public static void SendSKBuff(tcp_sock tp, ReadOnlySpan<byte> buffer)
        {

        }

        public static void Update(double elapsed)
        {

        }

        public static void MultiThreading_ReceiveWaitCheckNetPackage(tcp_sock tp, SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            
        }

        public static void Reset(tcp_sock tp)
        {

        }
    }
}
