using System.Security.Cryptography;

namespace AKNet.LinuxTcp
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
        static rtable skb_rtable(sk_buff skb)
        {
            return dst_rtable(skb_dst(skb));
        }

        static rtable ip_route_output_ports(net net, flowi4 fl4, tcp_sock tp, uint daddr, uint saddr, ushort dport, ushort sport, byte proto, byte tos, int oif)
        {
            flowi4_init_output(fl4, oif, sk? READ_ONCE(sk->sk_mark) : 0, tos,
			           sk? ip_sock_rt_scope(sk) : RT_SCOPE_UNIVERSE,
			           proto, sk? inet_sk_flowi_flags(sk) : 0,
			           daddr, saddr, dport, sport, sock_net_uid(net, sk));
	        if (sk)
                security_sk_classify_flow(sk, flowi4_to_flowi_common(fl4));
	        return ip_route_output_flow(net, fl4, sk);
            }
    }


}
