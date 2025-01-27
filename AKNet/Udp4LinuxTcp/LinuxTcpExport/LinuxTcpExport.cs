using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        public static void SendTcpStream(tcp_sock tp, ReadOnlySpan<byte> buffer)
        {
            tcp_sendmsg(tp, buffer);
        }

        public static void IPLayerSendStream(tcp_sock tp, sk_buff skb)
        {
            tp.mClientPeer.SendNetPackage(skb);
        }

        public static void Update(tcp_sock tp, double elapsed)
        {
            tp.icsk_retransmit_timer.Update(elapsed);
            tp.icsk_delack_timer.Update(elapsed);
            tp.sk_timer.Update(elapsed);
            tp.pacing_timer.Update(elapsed);
            tp.compressed_ack_timer.Update(elapsed);
        }

        public static void CheckReceivePackageLoss(tcp_sock tp, sk_buff mSkBuff)
        {
            tcp_v4_rcv(tp, mSkBuff);
        }

        public static void Init(tcp_sock tp)
        {
            inet_create(tp);
            tcp_init_sock(tp);
            tcp_connect_init(tp);
            tp.sk_state = TCP_ESTABLISHED;
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
