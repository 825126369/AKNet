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
    internal class inet_sock : sock
    {
        public ulong inet_flags;
        public uint inet_saddr;// 表示本地发送地址（Source Address），即发送方的 IP 地址。
        public uint inet_daddr;// 表示目的地址（Destination Address），即接收方的 IP 地址

        public int uc_ttl;
        public ushort inet_sport;
        public int inet_id;

        public byte tos;//用于存储 IP 数据包的 TOS（Type of Service，服务类型）字段值。TOS 字段是一个 8 位字段，用于指示数据包的优先级和传输特性。
        public byte min_ttl;
        public byte mc_ttl;
        public byte pmtudisc;
        public ushort rcv_tos;
        public byte convert_csum;
        public int uc_index;
        public int mc_index;
        public int mc_addr;
        public uint local_port_range;   /* high << 16 | low */
    }

}
