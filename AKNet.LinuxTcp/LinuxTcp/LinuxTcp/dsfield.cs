/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:50
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp.Common
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
