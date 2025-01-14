/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    internal partial class LinuxTcpFunc
    {
        public static void tcp_v4_send_check(tcp_sock tp, sk_buff skb)
        {
            __tcp_v4_send_check(skb, tp.inet_saddr, tp.inet_daddr);
        }

        public static void __tcp_v4_send_check(sk_buff skb, int saddr, int daddr)
        {
            //tcphdr th = tcp_hdr(skb);
            //th.check = ~tcp_v4_check(skb.len, saddr, daddr, 0);
            //skb.csum_start = skb_transport_header(skb) - skb.head;
            //skb.csum_offset = offsetof(tcphdr, check);
        }

        static void tcp_v4_send_reset(tcp_sock tp, sk_buff skb, skb_drop_reason reason)
        {

        }

        public static int tcp_v4_do_rcv(tcp_sock tp, sk_buff skb)
        {
            skb_drop_reason reason = skb_drop_reason.SKB_DROP_REASON_NOT_SPECIFIED;
            if (tp.sk_state == (byte)TCP_STATE.TCP_ESTABLISHED)
            {
                tcp_rcv_established(tp, skb);
                return 0;
            }

            if (tcp_checksum_complete(skb))
            {
                goto csum_err;
            }

            if (tp.sk_state == (byte)TCP_STATE.TCP_LISTEN)
            {

            }

            reason = tcp_rcv_state_process(tp, skb);
            if (reason > 0)
            {
                goto reset;
            }
            return 0;

        reset:
            tcp_v4_send_reset(tp, skb, reason);
        discard:
            sk_skb_reason_drop(tp, skb, reason);
            return 0;

        csum_err:
            reason = skb_drop_reason.SKB_DROP_REASON_TCP_CSUM;
            TCP_ADD_STATS(sock_net(tp), TCPMIB.TCP_MIB_CSUMERRORS, 1);
            TCP_ADD_STATS(sock_net(tp), TCPMIB.TCP_MIB_INERRS, 1);
            goto discard;
        }


    }

}
