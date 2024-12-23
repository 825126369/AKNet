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
       
    }
}
