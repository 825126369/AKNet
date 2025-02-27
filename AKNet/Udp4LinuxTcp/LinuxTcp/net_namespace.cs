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
    internal class net
    {
        public readonly netns_ipv4 ipv4 = new netns_ipv4();
        public readonly netns_mib mib = new netns_mib();
    }

    internal static partial class LinuxTcpFunc
    {
        public static readonly net init_net = new net();
    }
}
