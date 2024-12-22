namespace AKNet.LinuxTcp
{
    public class inet_cork
    {
        public uint flags;
        public int addr;
        public ip_options opt;
        public uint fragsize;
        public int length;
        public dst_entry dst;
        public byte tx_flags;
        public byte ttl;
        public short tos;
        public char priority;
        public ushort gso_size;
        public uint ts_opt_id;
        public long transmit_time;
        public int mark;
    }

    public class inet_cork_full : inet_cork
    {
	    public flowi        fl;
    }

    internal class inet_sock : sock
    {
        public ulong inet_flags;
        public int inet_saddr;
        public int uc_ttl;
        public short inet_sport;
        public ip_options inet_opt;
        public int inet_id;

        public byte tos;
        public byte min_ttl;
        public byte mc_ttl;
        public byte pmtudisc;
        public ushort rcv_tos;
        public byte convert_csum;
        public int uc_index;
        public int mc_index;
        public int mc_addr;
        public uint local_port_range;   /* high << 16 | low */

        public ip_mc_socklist mc_list;
        public inet_cork_full cork;
    }
}
