/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Security.Cryptography;

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

        public static int tcp_v4_do_rcv(tcp_sock tp, sk_buff skb)
        {
	        skb_drop_reason reason;

            if (tp.sk_state == (byte)TCP_STATE.TCP_ESTABLISHED)
            {
                dst_entry dst = tp.sk_rx_dst;

                sock_rps_save_rxhash(sk, skb);
                sk_mark_napi_id(sk, skb);
                if (dst != null)
                {
                    if (sk->sk_rx_dst_ifindex != skb->skb_iif ||
                        !INDIRECT_CALL_1(dst->ops->check, ipv4_dst_check,
                                 dst, 0))
                    {
                        RCU_INIT_POINTER(sk->sk_rx_dst, NULL);
                        dst_release(dst);
                    }
                }
                tcp_rcv_established(sk, skb);
                return 0;
            }

	        if (tcp_checksum_complete(skb))
            goto csum_err;

        if (sk->sk_state == TCP_LISTEN)
        {

                struct sock *nsk = tcp_v4_cookie_check(sk, skb);

        if (!nsk)
            return 0;
        if (nsk != sk)
        {
            reason = tcp_child_process(sk, nsk, skb);
            if (reason)
            {
                rsk = nsk;
                goto reset;
            }
            return 0;
        }
	        } else
            sock_rps_save_rxhash(sk, skb);

        reason = tcp_rcv_state_process(sk, skb);
        if (reason)
        {
            rsk = sk;
            goto reset;
        }
        return 0;

        reset:
        tcp_v4_send_reset(rsk, skb, sk_rst_convert_drop_reason(reason));
        discard:
        sk_skb_reason_drop(sk, skb, reason);
        /* Be careful here. If this function gets more complicated and
         * gcc suffers from register pressure on the x86, sk (in %ebx)
         * might be destroyed here. This current version compiles correctly,
         * but you have been warned.
         */
        return 0;
        
        csum_err:
            reason = SKB_DROP_REASON_TCP_CSUM;
            trace_tcp_bad_csum(skb);
            TCP_INC_STATS(sock_net(sk), TCP_MIB_CSUMERRORS);
            TCP_INC_STATS(sock_net(sk), TCP_MIB_INERRS);
            goto discard;
        }

    }
}
