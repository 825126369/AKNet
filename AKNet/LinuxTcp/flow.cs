namespace AKNet.LinuxTcp
{
    public class flowi_tunnel
    {
        public long tun_id;
    }

    public class flowi_common
    {
        public int flowic_oif;
        public int flowic_iif;
        public int flowic_l3mdev;
        public uint flowic_mark;
        public byte flowic_tos;
        public byte flowic_scope;
        public byte flowic_proto;
        public byte flowic_flags;
        public const byte FLOWI_FLAG_ANYSRC = 0x01;
        public const byte FLOWI_FLAG_KNOWN_NH = 0x02;
        public uint flowic_secid;
        public long flowic_uid;
        public uint flowic_multipath_hash;
        public flowi_tunnel flowic_tun_key;
    }

    internal class flowi
    {
        public class u
        {
            public flowi_common __fl_common;
            public object ip4;
            public object ip6;
        }

        public u u;
        public int flowi_oif
        {
            get { return u.__fl_common.flowic_oif; }
            set { u.__fl_common.flowic_oif = value; }
        }

        public int flowi_iif
        {
            get { return u.__fl_common.flowic_iif; }
            set { u.__fl_common.flowic_iif = value; }
        }

        public int flowi_l3mdev
        {
            get { return u.__fl_common.flowic_l3mdev; }
            set { u.__fl_common.flowic_l3mdev = value; }
        }

        public int flowi_mark
        {
            get { return u.__fl_common.flowic_mark; }
            set { u.__fl_common.flowic_mark = value; }
        }

        public int flowi_tos
        {
            get { return u.__fl_common.flowic_tos; }
            set { u.__fl_common.flowic_tos = value; }
        }

        public int flowi_scope
        {
            get { return u.__fl_common.flowic_scope; }
            set { u.__fl_common.flowic_scope = value; }
        }
        public int flowi_proto
        {
            get { return u.__fl_common.flowic_proto; }
            set { u.__fl_common.flowic_proto = value; }
        }

        public int flowi_flags
        {
            get { return u.__fl_common.flowic_flags; }
            set { u.__fl_common.flowic_flags = value; }
        }
        public int flowi_secid
        {
            get { return u.__fl_common.flowic_secid; }
            set { u.__fl_common.flowic_secid = value; }
        }

        public int flowi_tun_key
        {
            get { return u.__fl_common.flowic_tun_key; }
            set { u.__fl_common.flowic_tun_key = value; }
        }

        public int flowi_uid
        {
            get { return u.__fl_common.flowic_uid; }
            set { u.__fl_common.flowic_uid = value; }
        }
    }
}
