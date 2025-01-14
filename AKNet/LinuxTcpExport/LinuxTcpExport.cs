using System;
using System.Net.Sockets;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        //Dictionary<int, Action<object>> mEventDic = new Dictionary<int, Action<object>>();

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
            sk_buff mSkBuff = new sk_buff();
            Buffer.BlockCopy(e.Buffer, e.Offset, mSkBuff.data, 0, e.BytesTransferred);
            mSkBuff.nDataBeginIndex = 0;
            mSkBuff.len = mBuff.Length;
            tcp_v4_do_rcv(tp, mSkBuff);
        }

        public static void Reset(tcp_sock tp)
        {

        }
    }
}
