/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.LinuxTcp;

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
	/* This is tricky : We move IPCB at its correct location into TCP_SKB_CB()
	 * barrier() makes sure compiler wont play fool^Waliasing games.
	 */
	memmove(&TCP_SKB_CB(skb)->header.h4, IPCB(skb),
		sizeof(struct inet_skb_parm));
	barrier();

        TCP_SKB_CB(skb)->seq = ntohl(th->seq);
        TCP_SKB_CB(skb)->end_seq = (TCP_SKB_CB(skb)->seq + th->syn + th->fin +
				    skb->len - th->doff* 4);
	TCP_SKB_CB(skb)->ack_seq = ntohl(th->ack_seq);
        TCP_SKB_CB(skb)->tcp_flags = tcp_flag_byte(th);
        TCP_SKB_CB(skb)->ip_dsfield = ipv4_get_dsfield(iph);
        TCP_SKB_CB(skb)->sacked	 = 0;
	TCP_SKB_CB(skb)->has_rxtstamp =
			skb->tstamp || skb_hwtstamps(skb)->hwtstamp;
}

    /*
     *	From tcp_input.c
     */

    static int tcp_v4_rcv(tcp_sock tp, sk_buff skb)
{
	struct net * net = dev_net(skb->dev);
    enum skb_drop_reason drop_reason;
	int sdif = inet_sdif(skb);
    int dif = inet_iif(skb);
    const struct iphdr * iph;
    const struct tcphdr * th;
    struct sock * sk = NULL;
    bool refcounted;
    int ret;
    u32 isn;

    drop_reason = SKB_DROP_REASON_NOT_SPECIFIED;
	if (skb->pkt_type != PACKET_HOST)
		goto discard_it;

	/* Count it even if it's bad */
	__TCP_INC_STATS(net, TCP_MIB_INSEGS);

	if (!pskb_may_pull(skb, sizeof(struct tcphdr)))
		goto discard_it;

	th = (const struct tcphdr *)skb->data;

	if (unlikely(th->doff< sizeof(struct tcphdr) / 4)) {
		drop_reason = SKB_DROP_REASON_PKT_TOO_SMALL;
		goto bad_packet;
	}
if (!pskb_may_pull(skb, th->doff * 4))
    goto discard_it;

/* An explanation is required here, I think.
 * Packet length and doff are validated by header prediction,
 * provided case of th->doff==0 is eliminated.
 * So, we defer the checks. */

if (skb_checksum_init(skb, IPPROTO_TCP, inet_compute_pseudo))
    goto csum_error;

th = (const struct tcphdr *)skb->data;
iph = ip_hdr(skb);
lookup:
sk = __inet_lookup_skb(net->ipv4.tcp_death_row.hashinfo,
               skb, __tcp_hdrlen(th), th->source,
               th->dest, sdif, &refcounted);
if (!sk)
    goto no_tcp_socket;

if (sk->sk_state == TCP_TIME_WAIT)
    goto do_time_wait;

if (sk->sk_state == TCP_NEW_SYN_RECV)
{

        struct request_sock *req = inet_reqsk(sk);
bool req_stolen = false;
struct sock *nsk;

sk = req->rsk_listener;
if (!xfrm4_policy_check(sk, XFRM_POLICY_IN, skb))
    drop_reason = SKB_DROP_REASON_XFRM_POLICY;
else
    drop_reason = tcp_inbound_hash(sk, req, skb,
                       &iph->saddr, &iph->daddr,
                       AF_INET, dif, sdif);
if (unlikely(drop_reason))
{
    sk_drops_add(sk, skb);
    reqsk_put(req);
    goto discard_it;
}
if (tcp_checksum_complete(skb))
{
    reqsk_put(req);
    goto csum_error;
}
if (unlikely(sk->sk_state != TCP_LISTEN))
{
    nsk = reuseport_migrate_sock(sk, req_to_sk(req), skb);
    if (!nsk)
    {
        inet_csk_reqsk_queue_drop_and_put(sk, req);
        goto lookup;
    }
    sk = nsk;
    /* reuseport_migrate_sock() has already held one sk_refcnt
     * before returning.
     */
}
else
{
    /* We own a reference on the listener, increase it again
     * as we might lose it too soon.
     */
    sock_hold(sk);
}
refcounted = true;
nsk = NULL;
if (!tcp_filter(sk, skb))
{
    th = (const struct tcphdr *)skb->data;
iph = ip_hdr(skb);
tcp_v4_fill_cb(skb, iph, th);
nsk = tcp_check_req(sk, skb, req, false, &req_stolen);
		} else
{
    drop_reason = SKB_DROP_REASON_SOCKET_FILTER;
}
if (!nsk)
{
    reqsk_put(req);
    if (req_stolen)
    {
        /* Another cpu got exclusive access to req
         * and created a full blown socket.
         * Try to feed this packet to this socket
         * instead of discarding it.
         */
        tcp_v4_restore_cb(skb);
        sock_put(sk);
        goto lookup;
    }
    goto discard_and_relse;
}
nf_reset_ct(skb);
if (nsk == sk)
{
    reqsk_put(req);
    tcp_v4_restore_cb(skb);
}
else
{
    drop_reason = tcp_child_process(sk, nsk, skb);
    if (drop_reason)
    {

                enum sk_rst_reason rst_reason;

rst_reason = sk_rst_convert_drop_reason(drop_reason);
tcp_v4_send_reset(nsk, skb, rst_reason);
goto discard_and_relse;
			}
			sock_put(sk);
return 0;
		}
	}

process:
if (static_branch_unlikely(&ip4_min_ttl))
{
    /* min_ttl can be changed concurrently from do_ip_setsockopt() */
    if (unlikely(iph->ttl < READ_ONCE(inet_sk(sk)->min_ttl)))
    {
        __NET_INC_STATS(net, LINUX_MIB_TCPMINTTLDROP);
        drop_reason = SKB_DROP_REASON_TCP_MINTTL;
        goto discard_and_relse;
    }
}

if (!xfrm4_policy_check(sk, XFRM_POLICY_IN, skb))
{
    drop_reason = SKB_DROP_REASON_XFRM_POLICY;
    goto discard_and_relse;
}

drop_reason = tcp_inbound_hash(sk, NULL, skb, &iph->saddr, &iph->daddr,
                   AF_INET, dif, sdif);
if (drop_reason)
    goto discard_and_relse;

nf_reset_ct(skb);

if (tcp_filter(sk, skb))
{
    drop_reason = SKB_DROP_REASON_SOCKET_FILTER;
    goto discard_and_relse;
}
th = (const struct tcphdr *)skb->data;
iph = ip_hdr(skb);
tcp_v4_fill_cb(skb, iph, th);

skb->dev = NULL;

if (sk->sk_state == TCP_LISTEN)
{
    ret = tcp_v4_do_rcv(sk, skb);
    goto put_and_return;
}

sk_incoming_cpu_update(sk);

bh_lock_sock_nested(sk);
tcp_segs_in(tcp_sk(sk), skb);
ret = 0;
if (!sock_owned_by_user(sk))
{
    ret = tcp_v4_do_rcv(sk, skb);
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
