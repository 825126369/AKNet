using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        public static void SendTcpStream(tcp_sock tp, ReadOnlySpan<byte> mBuffer)
        {
            tcp_sendmsg(tp, mBuffer);
            //NetLogHelper.PrintByteArray("SendTcpStream: ", mBuffer);
        }

        public static bool ReceiveTcpStream(tcp_sock tp, msghdr mBuffer)
        {
            bool bHaveMoreData = tcp_recvmsg(tp, mBuffer);
            if (mBuffer.nLength > 0)
            {
                //NetLogHelper.PrintByteArray("ReceiveTcpStream: ", mBuffer.mBuffer.AsSpan().Slice(0, mBuffer.nLength));
            }
            return bHaveMoreData;
        }

        static int nSumSendCount = 0;
        public static void IPLayerSendStream(tcp_sock tp, sk_buff skb)
        {
            nSumSendCount++;
            tp.mClientPeer.SendNetPackage(skb);
            //NetLogHelper.PrintByteArray("IPLayerSendStream: ", skb.mBuffer.AsSpan().Slice(skb.nBufferOffset, skb.nBufferLength));
            //NetLog.Log("nSumSendCount: " + nSumSendCount);
        }

        public static void Update(tcp_sock tp, double elapsed)
        {
            tp.icsk_retransmit_timer.Update(elapsed);
            tp.icsk_delack_timer.Update(elapsed);
            tp.sk_timer.Update(elapsed);
            tp.pacing_timer.Update(elapsed);
            tp.compressed_ack_timer.Update(elapsed);
        }

        public static void CheckReceivePackageLoss(tcp_sock tp, sk_buff skb)
        {
            //NetLogHelper.PrintByteArray("CheckReceivePackageLoss: ", skb.mBuffer.AsSpan().Slice(skb.nBufferOffset, skb.nBufferLength));
            tcp_v4_rcv(tp, skb);
        }

        public static void Init(tcp_sock tp)
        {
            inet_init(tp);
            tcp_v4_init_sock(tp);
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
