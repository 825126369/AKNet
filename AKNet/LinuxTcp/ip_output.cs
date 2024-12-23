using AKNet.LinuxTcp;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        static int ip_queue_xmit(tcp_sock tp, sk_buff skb, flowi fl)
        {
	        return __ip_queue_xmit(tp, skb, fl, tp.tos);
        }

        static int __ip_queue_xmit(struct sock *sk, struct sk_buff *skb, struct flowi *fl, __u8 tos)
		{
				net net = sock_net(sk);
				ip_options inet_opt;
			flowi4 fl4;
			rtable rt;
			iphdr iph;
			int res;


				rcu_read_lock();
				inet_opt = rcu_dereference(inet->inet_opt);
				fl4 = &fl->u.ip4;
			rt = skb_rtable(skb);
			if (rt)
				goto packet_routed;

			/* Make sure we can route this packet. */
			rt = dst_rtable(__sk_dst_check(sk, 0));
			if (!rt) {
				__be32 daddr;

				/* Use correct destination address if we have options. */
				daddr = inet->inet_daddr;
				if (inet_opt && inet_opt->opt.srr)
					daddr = inet_opt->opt.faddr;

				/* If this fails, retransmit mechanism of transport layer will
				 * keep trying until route appears or the connection times
				 * itself out.
				 */
				rt = ip_route_output_ports(net, fl4, sk,
							   daddr, inet->inet_saddr,
							   inet->inet_dport,
							   inet->inet_sport,
							   sk->sk_protocol,
							   tos & INET_DSCP_MASK,
							   sk->sk_bound_dev_if);
				if (IS_ERR(rt))
					goto no_route;
				sk_setup_caps(sk, &rt->dst);
			}
			skb_dst_set_noref(skb, &rt->dst);

			packet_routed:
			if (inet_opt && inet_opt->opt.is_strictroute && rt->rt_uses_gateway)
				goto no_route;

			/* OK, we know where to send it, allocate and build IP header. */
			skb_push(skb, sizeof(struct iphdr) + (inet_opt? inet_opt->opt.optlen : 0));
			skb_reset_network_header(skb);
			iph = ip_hdr(skb);

			* ((__be16*) iph) = htons((4 << 12) | (5 << 8) | (tos & 0xff));
			if (ip_dont_fragment(sk, &rt->dst) && !skb->ignore_df)
				iph->frag_off = htons(IP_DF);
			else
				iph->frag_off = 0;
			iph->ttl      = ip_select_ttl(inet, &rt->dst);
			iph->protocol = sk->sk_protocol;
			ip_copy_addrs(iph, fl4);

			/* Transport layer set skb->h.foo itself. */

			if (inet_opt && inet_opt->opt.optlen) {
				iph->ihl += inet_opt->opt.optlen >> 2;
				ip_options_build(skb, &inet_opt->opt, inet->inet_daddr, rt);
		}

		ip_select_ident_segs(net, skb, sk,
					 skb_shinfo(skb)->gso_segs ?: 1);

		/* TODO : should we use skb->sk here instead of sk ? */
		skb->priority = READ_ONCE(sk->sk_priority);
		skb->mark = READ_ONCE(sk->sk_mark);

		res = ip_local_out(net, sk, skb);
		rcu_read_unlock();
		return res;

		no_route:
		rcu_read_unlock();
		IP_INC_STATS(net, IPSTATS_MIB_OUTNOROUTES);
		kfree_skb_reason(skb, SKB_DROP_REASON_IP_OUTNOROUTES);
		return -EHOSTUNREACH;
		}

    }
}
