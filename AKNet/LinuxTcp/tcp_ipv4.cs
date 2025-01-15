/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

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
            if (tp.sk_state == TCP_ESTABLISHED)
            {
                tcp_rcv_established(tp, skb);
                return 0;
            }

            if (tcp_checksum_complete(skb))
            {
                goto csum_err;
            }

            if (tp.sk_state == TCP_LISTEN)
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
        
        static void tcp_v4_fill_cb(sk_buff skb, iphdr iph, tcphdr th)
        {
            TCP_SKB_CB(skb).header.h4 = IPCB(skb);

            TCP_SKB_CB(skb).seq = th.seq;
            TCP_SKB_CB(skb).end_seq = (uint)(TCP_SKB_CB(skb).seq + th.syn + th.fin + skb.len - th.doff * 4);
            TCP_SKB_CB(skb).ack_seq = th.ack_seq;
            TCP_SKB_CB(skb).tcp_flags = tcp_flag_byte(th);
            TCP_SKB_CB(skb).ip_dsfield = ipv4_get_dsfield(iph);
            TCP_SKB_CB(skb).sacked = 0;
            TCP_SKB_CB(skb).has_rxtstamp = skb.tstamp > 0 || skb_hwtstamps(skb).hwtstamp > 0;
        }

        static bool tcp_add_backlog(tcp_sock tp, sk_buff skb)
        {
            uint tail_gso_size, tail_gso_segs;
            skb_shared_info shinfo;
            tcphdr th;
            tcphdr thtail;
            sk_buff tail;
            uint hdrlen;
            bool fragstolen;
            uint gso_segs;
            uint gso_size;
            ulong limit;
            int delta;

            skb_condense(skb);

            skb_dst_drop(skb);

            if (tcp_checksum_complete(skb))
            {
                reason = skb_drop_reason.SKB_DROP_REASON_TCP_CSUM;
                TCP_ADD_STATS(sock_net(tp), TCPMIB.TCP_MIB_CSUMERRORS, 1);
                TCP_ADD_STATS(sock_net(tp), TCPMIB.TCP_MIB_INERRS, 1);
                return true;
            }

            th = tcp_hdr(skb);
            hdrlen = th.doff * 4;

            tail = tp.sk_backlog.tail;
            if (tail == null)
            {
                goto no_coalesce;
            }

            thtail = tcp_hdr(tail).data;

            if (TCP_SKB_CB(tail).end_seq != TCP_SKB_CB(skb).seq ||
                TCP_SKB_CB(tail).ip_dsfield != TCP_SKB_CB(skb).ip_dsfield ||
                BoolOk((TCP_SKB_CB(tail).tcp_flags | TCP_SKB_CB(skb).tcp_flags) & (TCPHDR_SYN | TCPHDR_RST | TCPHDR_URG)) ||
                !BoolOk((TCP_SKB_CB(tail).tcp_flags & TCP_SKB_CB(skb).tcp_flags) & TCPHDR_ACK) ||
                BoolOk((TCP_SKB_CB(tail).tcp_flags ^ TCP_SKB_CB(skb).tcp_flags) & (TCPHDR_ECE | TCPHDR_CWR)) ||
                !tcp_skb_can_collapse_rx(tail, skb) ||
                thtail.doff != th.doff ||
                memcmp(thtail + 1, th + 1, hdrlen - sizeof_tcphdr))
            {
                goto no_coalesce;
            }

            shinfo = skb_shinfo(skb);
            gso_size = (uint)(shinfo.gso_size > 0 ? shinfo.gso_size : skb.len);
            gso_segs = (uint)(shinfo.gso_segs > 0 ? shinfo.gso_segs : 1);

            shinfo = skb_shinfo(tail);
            tail_gso_size = shinfo.gso_size > 0 ? shinfo.gso_size : (tail.len - hdrlen);
            tail_gso_segs = shinfo.gso_segs > 0 ? shinfo.gso_segs : 1;

            if (skb_try_coalesce(tail, skb))
            {
                TCP_SKB_CB(tail).end_seq = TCP_SKB_CB(skb).end_seq;

                if (!before(TCP_SKB_CB(skb).ack_seq, TCP_SKB_CB(tail).ack_seq))
                {
                    TCP_SKB_CB(tail).ack_seq = TCP_SKB_CB(skb).ack_seq;
                    thtail.window = th.window;
                }

                thtail.fin |= th.fin;
                TCP_SKB_CB(tail).tcp_flags |= TCP_SKB_CB(skb).tcp_flags;

                if (TCP_SKB_CB(skb).has_rxtstamp)
                {
                    TCP_SKB_CB(tail).has_rxtstamp = true;
                    tail.tstamp = skb.tstamp;
                    skb_hwtstamps(tail).hwtstamp = skb_hwtstamps(skb).hwtstamp;
                }

                shinfo.gso_size = (ushort)Math.Max(gso_size, tail_gso_size);
                shinfo.gso_segs = (ushort)Math.Min(gso_segs + tail_gso_segs, 0xFFFF);

                tp.sk_backlog.len += (int)delta;
                NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPBACKLOGCOALESCE, 1);
                return false;
            }

        no_coalesce:
            limit = (ulong)(tp.sk_rcvbuf << 1);
            limit += (ulong)(tp.sk_sndbuf >> 1);
            limit += 64 * 1024;

            limit = Math.Min(limit, uint.MaxValue);

            if (sk_add_backlog(tp, skb, limit))
            {
                reason = skb_drop_reason.SKB_DROP_REASON_SOCKET_BACKLOG;
                NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPBACKLOGDROP, 1);
                return true;
            }
            return false;
        }

        static void tcp_v4_rcv(tcp_sock tp, sk_buff skb)
        { 
            if (skb.pkt_type != PACKET_HOST)
            {
                return;
            }

            var th = tcp_hdr(skb);
            if (th.doff < sizeof_tcphdr / 4)
            {
                return;
            }

            if (skb_checksum_init(skb, IPPROTO_TCP, inet_compute_pseudo) > 0)
            {
                return;
            }

            var iph = ip_hdr(skb);
            tcp_v4_fill_cb(skb, iph, th);
            tcp_segs_in(tp, skb);
            if (true)
            {
                tcp_v4_do_rcv(tp, skb);
            }
            else
            {
                tcp_add_backlog(tp, skb);
            }
        }
    }

}
