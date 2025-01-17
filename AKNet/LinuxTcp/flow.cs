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
