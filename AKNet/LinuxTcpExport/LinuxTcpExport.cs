using AKNet.Udp4LinuxTcp.Common;
using System;
using System.Net.Sockets;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        //Dictionary<int, Action<object>> mEventDic = new Dictionary<int, Action<object>>();

        public static void SendTcpStream(tcp_sock tp, ReadOnlySpan<byte> buffer)
        {
            tcp_sendmsg(tp, buffer);
        }

        public static void SendSKBuff(tcp_sock tp, ReadOnlySpan<byte> buffer)
        {

        }

        public static void ReceiveSKBuff(tcp_sock tp, sk_buff mBuffer)
        {

        }

        public static void Update(double elapsed)
        {

        }

        public static void MultiThreading_ReceiveWaitCheckNetPackage(tcp_sock tp, SocketAsyncEventArgs e)
        {
            ReadOnlySpan<byte> mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            sk_buff mSkBuff = new sk_buff();
            Buffer.BlockCopy(e.Buffer, e.Offset, mSkBuff.mBuffer, 0, e.BytesTransferred);
            mSkBuff.nBeginDataIndex = 0;
            mSkBuff.len = mBuff.Length;
            tcp_v4_rcv(tp, mSkBuff);
        }

        public static void CheckReceivePackageLoss(tcp_sock tp, NetUdpReceiveFixedSizePackage mPackage)
        {
            sk_buff mSkBuff = new sk_buff();
            mPackage.GetTcpBufferSpan().CopyTo(mSkBuff.mBuffer);
            mSkBuff.nBeginDataIndex = 0;
            mSkBuff.len = mPackage.nBodyLength;
            tcp_v4_rcv(tp, mSkBuff);
        }

        public static void Reset(tcp_sock tp)
        {
            tcp_init_sock(tp);
        }
    }
}
