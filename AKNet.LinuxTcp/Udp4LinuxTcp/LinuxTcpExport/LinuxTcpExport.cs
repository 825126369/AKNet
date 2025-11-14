/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:42
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        public static void SendTcpStream(tcp_sock tp, ReadOnlySpan<byte> mBuffer)
        {
            tcp_sendmsg(tp, mBuffer);
        }

        public static bool ReceiveTcpStream(tcp_sock tp, msghdr mBuffer)
        {
            return tcp_recvmsg(tp, mBuffer);
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

        public static void CheckReceivePackageLoss(tcp_sock tp, sk_buff skb)
        {
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
