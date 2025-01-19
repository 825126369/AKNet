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

        public static void Update(tcp_sock tp, double elapsed)
        {
            tp.icsk_retransmit_timer.Update(elapsed);
            tp.icsk_delack_timer.Update(elapsed);
            tp.sk_timer.Update(elapsed);
            tp.pacing_timer.Update(elapsed);
            tp.compressed_ack_timer.Update(elapsed);
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

        public static void Init(tcp_sock tp)
        {
            inet_create(tp);
            tcp_init_sock(tp);
        }

        public static void Reset(tcp_sock tp)
        {
            tp.icsk_retransmit_timer.Reset();
            tp.icsk_delack_timer.Reset();
            tp.sk_timer.Reset();
            tp.pacing_timer.Reset();
            tp.compressed_ack_timer.Reset();
        }
    }
}
