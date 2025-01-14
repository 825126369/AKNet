/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.LinuxTcp;
using System.Collections.Generic;
using System.Net.Sockets;

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
            TCP_SKB_CB(skb).end_seq = TCP_SKB_CB(skb).seq + th.syn + th.fin + skb.len - th.doff * 4;
            TCP_SKB_CB(skb).ack_seq = th.ack_seq;
            TCP_SKB_CB(skb).tcp_flags = tcp_flag_byte(th);
            TCP_SKB_CB(skb).ip_dsfield = ipv4_get_dsfield(iph);
            TCP_SKB_CB(skb).sacked = 0;
            TCP_SKB_CB(skb).has_rxtstamp = BoolOk(skb.tstamp || skb_hwtstamps(skb).hwtstamp);
        }

        static bool tcp_add_backlog(tcp_sock tp, sk_buff skb, skb_drop_reason reason)
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

	        if (unlikely(tcp_checksum_complete(skb))) {
		        bh_unlock_sock(sk);
                trace_tcp_bad_csum(skb);
		        *reason = SKB_DROP_REASON_TCP_CSUM;
		        __TCP_INC_STATS(sock_net(sk), TCP_MIB_CSUMERRORS);
                __TCP_INC_STATS(sock_net(sk), TCP_MIB_INERRS);
		        return true;
	        }
            th = (const struct tcphdr *)skb->data;
	        hdrlen = th.doff * 4;

	        tail = tp.sk_backlog.tail;
	        if (!tail)
		        goto no_coalesce;
	        thtail = (struct tcphdr *)tail->data;

	        if (TCP_SKB_CB(tail)->end_seq != TCP_SKB_CB(skb)->seq ||
	            TCP_SKB_CB(tail)->ip_dsfield != TCP_SKB_CB(skb)->ip_dsfield ||
	            ((TCP_SKB_CB(tail)->tcp_flags |
	              TCP_SKB_CB(skb)->tcp_flags) & (TCPHDR_SYN | TCPHDR_RST | TCPHDR_URG)) ||
	            !((TCP_SKB_CB(tail)->tcp_flags &
	              TCP_SKB_CB(skb)->tcp_flags) & TCPHDR_ACK) ||
	            ((TCP_SKB_CB(tail)->tcp_flags ^
	              TCP_SKB_CB(skb)->tcp_flags) & (TCPHDR_ECE | TCPHDR_CWR)) ||
	            !tcp_skb_can_collapse_rx(tail, skb) ||
	            thtail->doff != th->doff ||
	            memcmp(thtail + 1, th + 1, hdrlen - sizeof(* th)))
		        goto no_coalesce;

	        __skb_pull(skb, hdrlen);

            shinfo = skb_shinfo(skb);
            gso_size = shinfo->gso_size?: skb->len;
	        gso_segs = shinfo->gso_segs?: 1;

	        shinfo = skb_shinfo(tail);
            tail_gso_size = shinfo->gso_size?: (tail->len - hdrlen);
	        tail_gso_segs = shinfo->gso_segs?: 1;

	        if (skb_try_coalesce(tail, skb, &fragstolen, &delta)) {
		        TCP_SKB_CB(tail)->end_seq = TCP_SKB_CB(skb)->end_seq;

		        if (likely(!before(TCP_SKB_CB(skb)->ack_seq, TCP_SKB_CB(tail)->ack_seq))) {
			        TCP_SKB_CB(tail)->ack_seq = TCP_SKB_CB(skb)->ack_seq;
			        thtail->window = th->window;
		        }

        /* We have to update both TCP_SKB_CB(tail)->tcp_flags and
         * thtail->fin, so that the fast path in tcp_rcv_established()
         * is not entered if we append a packet with a FIN.
         * SYN, RST, URG are not present.
         * ACK is set on both packets.
         * PSH : we do not really care in TCP stack,
         *       at least for 'GRO' packets.
         */
        thtail->fin |= th->fin;
        TCP_SKB_CB(tail)->tcp_flags |= TCP_SKB_CB(skb)->tcp_flags;

        if (TCP_SKB_CB(skb)->has_rxtstamp)
        {
            TCP_SKB_CB(tail)->has_rxtstamp = true;
            tail->tstamp = skb->tstamp;
            skb_hwtstamps(tail)->hwtstamp = skb_hwtstamps(skb)->hwtstamp;
        }

        /* Not as strict as GRO. We only need to carry mss max value */
        shinfo->gso_size = max(gso_size, tail_gso_size);
        shinfo->gso_segs = min_t(u32, gso_segs + tail_gso_segs, 0xFFFF);

        sk->sk_backlog.len += delta;
        __NET_INC_STATS(sock_net(sk),
                LINUX_MIB_TCPBACKLOGCOALESCE);
        kfree_skb_partial(skb, fragstolen);
        return false;
	        }
	        __skb_push(skb, hdrlen);

        no_coalesce:
        /* sk->sk_backlog.len is reset only at the end of __release_sock().
         * Both sk->sk_backlog.len and sk->sk_rmem_alloc could reach
         * sk_rcvbuf in normal conditions.
         */
        limit = ((u64)READ_ONCE(sk->sk_rcvbuf)) << 1;

        limit += ((u32)READ_ONCE(sk->sk_sndbuf)) >> 1;

        /* Only socket owner can try to collapse/prune rx queues
         * to reduce memory overhead, so add a little headroom here.
         * Few sockets backlog are possibly concurrently non empty.
         */
        limit += 64 * 1024;

        limit = min_t(u64, limit, UINT_MAX);

        if (unlikely(sk_add_backlog(sk, skb, limit)))
        {
            bh_unlock_sock(sk);
            *reason = SKB_DROP_REASON_SOCKET_BACKLOG;
            __NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPBACKLOGDROP);
            return true;
        }
        return false;
        }

        static int tcp_v4_rcv(tcp_sock tp, sk_buff skb)
        {
            net net = dev_net(skb.dev);
            skb_drop_reason drop_reason;
            int sdif = inet_sdif(skb);
            int dif = inet_iif(skb);

            iphdr iph;
            tcphdr th;
            bool refcounted;
            int ret;
            uint isn;

            drop_reason = skb_drop_reason.SKB_DROP_REASON_NOT_SPECIFIED;
            if (skb.pkt_type != PACKET_HOST)
            {
                goto discard_it;
            }

            TCP_ADD_STATS(net, TCPMIB.TCP_MIB_INSEGS, 1);

            th = tcp_hdr(skb);
            if (th.doff < sizeof_tcphdr / 4)
            {
                drop_reason = skb_drop_reason.SKB_DROP_REASON_PKT_TOO_SMALL;
                goto bad_packet;
            }

            if (skb_checksum_init(skb, IPPROTO_TCP, inet_compute_pseudo) > 0)
            {
                goto csum_error;
            }

            iph = ip_hdr(skb);
        lookup:
            if (tp.sk_state == TCP_TIME_WAIT)
            {
                goto do_time_wait;
            }
            if (tp.sk_state == TCP_NEW_SYN_RECV)
            {

                request_sock req = inet_reqsk(tp);
                bool req_stolen = false;
                sock nsk;

                sk = req.rsk_listener;
                if (!xfrm4_policy_check(sk, XFRM_POLICY_IN, skb))
                    drop_reason = SKB_DROP_REASON_XFRM_POLICY;
                else
                    drop_reason = tcp_inbound_hash(sk, req, skb,
                                       &iph->saddr, &iph->daddr,
                                       AF_INET, dif, sdif);


                if (drop_reason > 0)
                {
                    sk_drops_add(sk, skb);
                    goto discard_it;
                }
                if (tcp_checksum_complete(skb))
                {
                    goto csum_error;
                }

                if (tp.sk_state != TCP_LISTEN)
                {
                    nsk = reuseport_migrate_sock(sk, req_to_sk(req), skb);
                    if (!nsk)
                    {
                        inet_csk_reqsk_queue_drop_and_put(sk, req);
                        goto lookup;
                    }
                    sk = nsk;
                }

                refcounted = true;
                nsk = null;
                if (true)
                {
                    th = tcp_hdr(skb);
                    iph = ip_hdr(skb);
                    tcp_v4_fill_cb(skb, iph, th);
                } 
                else
                {
                    drop_reason = SKB_DROP_REASON_SOCKET_FILTER;
                }


            process:
                if (ip4_min_ttl)
                {
                    if (iph.ttl < tp.min_ttl)
                    {
                        NET_ADD_STATS(net, LINUXMIB.LINUX_MIB_TCPMINTTLDROP, 1);
                        drop_reason = skb_drop_reason.SKB_DROP_REASON_TCP_MINTTL;
                        goto discard_and_relse;
                    }
                }


                th = tcp_hdr(skb);
                iph = ip_hdr(skb);
                tcp_v4_fill_cb(skb, iph, th);

                skb.dev = null;

                if (tp.sk_state == TCP_LISTEN)
                {
                    ret = tcp_v4_do_rcv(sk, skb);
                    goto put_and_return;
                }


                tcp_segs_in(tp, skb);
                ret = 0;
                if (true)
                {
                    ret = tcp_v4_do_rcv(tp, skb);
                }
                else
                {
                    if (tcp_add_backlog(sk, skb, &drop_reason))
                        goto discard_and_relse;
                }
bh_unlock_sock(sk);

put_and_return:
if (refcounted)
    sock_put(sk);

return ret;

no_tcp_socket:
drop_reason = SKB_DROP_REASON_NO_SOCKET;
if (!xfrm4_policy_check(NULL, XFRM_POLICY_IN, skb))
    goto discard_it;

tcp_v4_fill_cb(skb, iph, th);

if (tcp_checksum_complete(skb))
{
csum_error:
    drop_reason = SKB_DROP_REASON_TCP_CSUM;
    trace_tcp_bad_csum(skb);
    __TCP_INC_STATS(net, TCP_MIB_CSUMERRORS);
bad_packet:
    __TCP_INC_STATS(net, TCP_MIB_INERRS);
}
else
{
    tcp_v4_send_reset(NULL, skb, sk_rst_convert_drop_reason(drop_reason));
}

discard_it:
SKB_DR_OR(drop_reason, NOT_SPECIFIED);
/* Discard frame. */
sk_skb_reason_drop(sk, skb, drop_reason);
return 0;

discard_and_relse:
sk_drops_add(sk, skb);
if (refcounted)
    sock_put(sk);
goto discard_it;

do_time_wait:
if (!xfrm4_policy_check(NULL, XFRM_POLICY_IN, skb))
{
    drop_reason = SKB_DROP_REASON_XFRM_POLICY;
    inet_twsk_put(inet_twsk(sk));
    goto discard_it;
}

tcp_v4_fill_cb(skb, iph, th);

if (tcp_checksum_complete(skb))
{
    inet_twsk_put(inet_twsk(sk));
    goto csum_error;
}
switch (tcp_timewait_state_process(inet_twsk(sk), skb, th, &isn))
{
    case TCP_TW_SYN:
        {

        struct sock *sk2 = inet_lookup_listener(net,
                            net->ipv4.tcp_death_row.hashinfo,
                            skb, __tcp_hdrlen(th),
                            iph->saddr, th->source,
                            iph->daddr, th->dest,
                            inet_iif(skb),
                            sdif);
if (sk2)
{
    inet_twsk_deschedule_put(inet_twsk(sk));
    sk = sk2;
    tcp_v4_restore_cb(skb);
    refcounted = false;
    __this_cpu_write(tcp_tw_isn, isn);
    goto process;
}
	}
		/* to ACK */
		fallthrough;

    case TCP_TW_ACK:
    tcp_v4_timewait_ack(sk, skb);
    break;
case TCP_TW_RST:
    tcp_v4_send_reset(sk, skb, SK_RST_REASON_TCP_TIMEWAIT_SOCKET);
    inet_twsk_deschedule_put(inet_twsk(sk));
    goto discard_it;
case TCP_TW_SUCCESS:;
}
goto discard_it;
}


    }

}
