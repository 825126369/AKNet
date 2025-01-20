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

    internal class flowi4
    {
        public uint saddr;
        public uint daddr;
        public flowi_uli uli;
    }


    internal class flowi
    {
        internal class uu
        {
            public flowi_common __fl_common;
            public flowi4 ip4;
            //public flowi6       ip6;
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
				      kuid_t uid)
        {

            fl4->flowi4_oif = oif;
	        fl4->flowi4_iif = LOOPBACK_IFINDEX;
	        fl4->flowi4_l3mdev = 0;
	        fl4->flowi4_mark = mark;
	        fl4->flowi4_tos = tos;
	        fl4->flowi4_scope = scope;
	        fl4->flowi4_proto = proto;
	        fl4->flowi4_flags = flags;
	        fl4->flowi4_secid = 0;
	        fl4->flowi4_tun_key.tun_id = 0;
	        fl4->flowi4_uid = uid;
	        fl4->daddr = daddr;
	        fl4->saddr = saddr;
	        fl4->fl4_dport = dport;
	        fl4->fl4_sport = sport;
	        fl4->flowi4_multipath_hash = 0;
        }
    }
}
