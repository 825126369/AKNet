namespace AKNet.LinuxTcp
{
    internal partial class LinuxTcpFunc
    {
        static tcp_sock tcp_check_req(tcp_sock tp, sk_buff skb, request_sock req, bool fastopen, ref bool req_stolen)
        {
             tcp_options_received tmp_opt;
	        struct sock *child;
	        const struct tcphdr *th = tcp_hdr(skb);
                __be32 flg = tcp_flag_word(th) & (TCP_FLAG_RST | TCP_FLAG_SYN | TCP_FLAG_ACK);
                bool paws_reject = false;
                bool own_req;

                tmp_opt.saw_tstamp = 0;
	        if (th->doff > (sizeof(struct tcphdr)>>2)) {
		        tcp_parse_options(sock_net(sk), skb, &tmp_opt, 0, NULL);

		        if (tmp_opt.saw_tstamp) {
			        tmp_opt.ts_recent = READ_ONCE(req->ts_recent);
			        if (tmp_opt.rcv_tsecr)
                        tmp_opt.rcv_tsecr -= tcp_rsk(req)->ts_off;
                /* We do not store true stamp, but it is not required,
                 * it can be estimated (approximately)
                 * from another data.
                 */
                tmp_opt.ts_recent_stamp = ktime_get_seconds() - reqsk_timeout(req, TCP_RTO_MAX) / HZ;
			        paws_reject = tcp_paws_reject(&tmp_opt, th->rst);
            }
        }

        /* Check for pure retransmitted SYN. */
        if (TCP_SKB_CB(skb)->seq == tcp_rsk(req)->rcv_isn &&
            flg == TCP_FLAG_SYN &&
            !paws_reject)
        {
            if (!tcp_oow_rate_limited(sock_net(sk), skb,
                          LINUX_MIB_TCPACKSKIPPEDSYNRECV,
                          &tcp_rsk(req)->last_oow_ack_time) &&

                !inet_rtx_syn_ack(sk, req))
            {
                unsigned long expires = jiffies;

                expires += reqsk_timeout(req, TCP_RTO_MAX);
                if (!fastopen)
                    mod_timer_pending(&req->rsk_timer, expires);
                else
                    req->rsk_timer.expires = expires;
            }
            return NULL;
        }

        if ((flg & TCP_FLAG_ACK) && !fastopen && (TCP_SKB_CB(skb)->ack_seq != tcp_rsk(req)->snt_isn + 1))
        {
            return sk;
        }

        if (paws_reject || !tcp_in_window(TCP_SKB_CB(skb)->seq,
                          TCP_SKB_CB(skb)->end_seq,
                          tcp_rsk(req)->rcv_nxt,
                          tcp_rsk(req)->rcv_nxt +
                          tcp_synack_window(req)))
        {
            /* Out of window: send ACK and drop. */
            if (!(flg & TCP_FLAG_RST) &&
                !tcp_oow_rate_limited(sock_net(sk), skb,
                          LINUX_MIB_TCPACKSKIPPEDSYNRECV,
                          &tcp_rsk(req)->last_oow_ack_time))
                req->rsk_ops->send_ack(sk, skb, req);
            if (paws_reject)
                NET_INC_STATS(sock_net(sk), LINUX_MIB_PAWSESTABREJECTED);
            return NULL;
        }
        
        if (tmp_opt.saw_tstamp && !after(TCP_SKB_CB(skb)->seq, tcp_rsk(req)->rcv_nxt))
            WRITE_ONCE(req->ts_recent, tmp_opt.rcv_tsval);

        if (TCP_SKB_CB(skb)->seq == tcp_rsk(req)->rcv_isn)
        {
            flg &= ~TCP_FLAG_SYN;
        }
        
        if (flg & (TCP_FLAG_RST | TCP_FLAG_SYN))
        {
            TCP_INC_STATS(sock_net(sk), TCP_MIB_ATTEMPTFAILS);
            goto embryonic_reset;
        }
        
        if (!(flg & TCP_FLAG_ACK))
            return NULL;
        
        if (fastopen)
            return sk;

        /* While TCP_DEFER_ACCEPT is active, drop bare ACK. */
        if (req->num_timeout < READ_ONCE(inet_csk(sk)->icsk_accept_queue.rskq_defer_accept) &&
            TCP_SKB_CB(skb)->end_seq == tcp_rsk(req)->rcv_isn + 1)
        {
            inet_rsk(req)->acked = 1;
            __NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPDEFERACCEPTDROP);
            return NULL;
        }
        
        child = inet_csk(sk)->icsk_af_ops->syn_recv_sock(sk, skb, req, NULL, req, &own_req);
        if (!child)
            goto listen_overflow;

        if (own_req && rsk_drop_req(req))
        {
            reqsk_queue_removed(&inet_csk(req->rsk_listener)->icsk_accept_queue, req);
            inet_csk_reqsk_queue_drop_and_put(req->rsk_listener, req);
            return child;
        }

        sock_rps_save_rxhash(child, skb);
        tcp_synack_rtt_meas(child, req);
        *req_stolen = !own_req;
        return inet_csk_complete_hashdance(sk, child, req, own_req);

        listen_overflow:
        if (sk != req->rsk_listener)
            __NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPMIGRATEREQFAILURE);

        if (!READ_ONCE(sock_net(sk)->ipv4.sysctl_tcp_abort_on_overflow))
        {
            inet_rsk(req)->acked = 1;
            return NULL;
        }

        embryonic_reset:
        if (!(flg & TCP_FLAG_RST))
        {
            req->rsk_ops->send_reset(sk, skb, SK_RST_REASON_INVALID_SYN);
        }
        else if (fastopen)
        {
            reqsk_fastopen_remove(sk, req, true);
            tcp_reset(sk, skb);
        }
        if (!fastopen)
        {
            bool unlinked = inet_csk_reqsk_queue_drop(sk, req);

            if (unlinked)
                __NET_INC_STATS(sock_net(sk), LINUX_MIB_EMBRYONICRSTS);
            *req_stolen = !unlinked;
        }
        return NULL;
        }
    }
}
