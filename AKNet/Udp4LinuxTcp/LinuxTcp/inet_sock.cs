/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp
{
    internal class inet_cork
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

    internal class inet_cork_full : inet_cork
    {
        public flowi fl;
    }

    internal class inet_sock : sock
    {
        public ulong inet_flags;
        public uint inet_saddr;// 表示本地发送地址（Source Address），即发送方的 IP 地址。
        public uint inet_daddr;// 表示目的地址（Destination Address），即接收方的 IP 地址

        public int uc_ttl;
        public ushort inet_sport;
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
        public readonly inet_cork_full cork = new inet_cork_full();
    }

}
