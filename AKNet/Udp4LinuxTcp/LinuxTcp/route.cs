namespace AKNet.Udp4LinuxTcp
{
    internal class rtable
    {
        public dst_entry dst;
        public int rt_genid;
        public uint rt_flags;
        public ushort rt_type;
        public byte rt_is_input;
        public byte rt_uses_gateway;
        public int rt_iif;
        public byte rt_gw_family;
        public uint rt_gw4;
        public uint rt_mtu_locked;
        public uint rt_pmtu;
    }

    internal static partial class LinuxTcpFunc
    {
        //static rtable skb_rtable(sk_buff skb)
        //{
        //    return dst_rtable(skb_dst(skb));
        //}

        //static rt_scope_t ip_sock_rt_scope(tcp_sock tp)
        //{
        //    if (sock_flag(tp, sock_flags.SOCK_LOCALROUTE))
        //    {
        //        return rt_scope_t.RT_SCOPE_LINK;
        //    }
	       // return rt_scope_t.RT_SCOPE_UNIVERSE;
        //}

        //static rtable ip_route_output_ports(net net, flowi4 fl4, tcp_sock tp, 
        //    uint daddr, uint saddr, ushort dport, ushort sport, byte proto, byte tos, int oif)
        //{

        //    flowi4_init_output(fl4, oif, tp.sk_mark, tos,
        //               ip_sock_rt_scope(tp),
        //               proto, inet_sk_flowi_flags(tp),
        //               daddr, saddr, dport, sport, sock_net_uid(net, sk));

        //    security_sk_classify_flow(tp, flowi4_to_flowi_common(fl4));
        //    return ip_route_output_flow(net, fl4, sk);
        //}
    }
}
