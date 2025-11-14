/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp.Common
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
