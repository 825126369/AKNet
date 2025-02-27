/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp.Common
{
    internal class flowi_tunnel
    {
        public long tun_id;
    }

    internal class flowi_common
    {
        public int flowic_oif;
        public int flowic_iif;
        public int flowic_l3mdev;
        public uint flowic_mark;
        public byte flowic_tos;
        public byte flowic_scope;
        public byte flowic_proto;
        public byte flowic_flags;
        public uint flowic_secid;
        public long flowic_uid;
        public uint flowic_multipath_hash;
        public flowi_tunnel flowic_tun_key;
    }

    internal class flowi_uli
    {
        internal class class_ports
        {
            public ushort dport;
            public ushort sport;
        }

        internal class class_icmpt
        {
            public byte type;
            public byte code;
        }

        internal class class_mht
        {
            public byte type;
        }

        public class_ports ports;
        public class_icmpt icmpt;
        public uint gre_key;
        public class_mht mht;
    }

    internal class flowi4 : flowi_common
    {
        public uint saddr;
        public uint daddr;
        public flowi_uli uli;

        public int flowi4_oif { get { return flowic_oif; } set { flowic_oif = value; } }
        public int flowi4_iif { get { return flowic_iif; } set { flowic_iif = value; } }
        public int flowi4_l3mdev { get { return flowic_l3mdev; } set { flowic_l3mdev = value; } }
        public uint flowi4_mark { get { return flowic_mark; } set { flowic_mark = value; } }
        public byte flowi4_tos { get { return flowic_tos; } set { flowic_tos = value; } }
        public byte flowi4_scope { get { return flowic_scope; } set { flowic_scope = value; } }
        public byte flowi4_proto { get { return flowic_proto; } set { flowic_proto = value; } }
        public byte flowi4_flags { get { return flowic_flags; } set { flowic_flags = value; } }
        public uint flowi4_secid { get { return flowic_secid; } set { flowic_secid = value; } }
        public flowi_tunnel flowi4_tun_key { get { return flowic_tun_key; } set { flowic_tun_key = value; } }
        public long flowi4_uid { get { return flowic_uid; } set { flowic_uid = value; } }
        public uint flowi4_multipath_hash { get { return flowic_multipath_hash; } set { flowic_multipath_hash = value; } }


        public ushort fl4_sport { get { return uli.ports.sport; } set { uli.ports.sport = value; } }
        public ushort fl4_dport { get { return uli.ports.dport; } set { uli.ports.dport = value; } }
        public byte fl4_icmp_type { get { return uli.icmpt.type; } set { uli.icmpt.type = value; } }
        public byte fl4_icmp_code { get { return uli.icmpt.code; } set { uli.icmpt.code = value; } }
        public byte fl4_mh_type { get { return uli.mht.type; } set { uli.mht.type = value; } }
        public uint fl4_gre_key { get { return uli.gre_key; } set { uli.gre_key = value; } }
    }


    internal class flowi : flowi_common
    {
        internal class uu
        {
            public flowi4 ip4;
        }
        public uu u;
    }

    internal static partial class LinuxTcpFunc
    {
        static void flowi4_init_output(flowi4 fl4, int oif,
                      uint mark, byte tos, byte scope,
                      byte proto, byte flags,
				      uint daddr, uint saddr,
				      ushort dport, ushort sport,
				      long uid)
        {
            fl4.flowi4_oif = oif;
	        fl4.flowi4_iif = LOOPBACK_IFINDEX;
	        fl4.flowi4_l3mdev = 0;
	        fl4.flowi4_mark = mark;
	        fl4.flowi4_tos = tos;
	        fl4.flowi4_scope = scope;
	        fl4.flowi4_proto = proto;
	        fl4.flowi4_flags = flags;
	        fl4.flowi4_secid = 0;
	        fl4.flowi4_tun_key.tun_id = 0;
	        fl4.flowi4_uid = uid;
	        fl4.daddr = daddr;
	        fl4.saddr = saddr;
	        fl4.fl4_dport = dport;
	        fl4.fl4_sport = sport;
	        fl4.flowi4_multipath_hash = 0;
        }
    }
}
