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
    internal partial class LinuxTcpFunc
    {
        static byte ipv4_get_dsfield(tcphdr iph)
        {
	        return iph.tos;
        }

        static void ipv4_change_dsfield(tcphdr iph, byte mask, byte value)
        {
            byte dsfield = (byte)((iph.tos & mask) | value);
            iph.tos = dsfield;
        }
    }
}
