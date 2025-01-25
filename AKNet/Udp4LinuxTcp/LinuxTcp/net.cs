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
    internal class net
    {
        public readonly netns_ipv4  ipv4 = new netns_ipv4();
        public readonly netns_mib    mib = new netns_mib();
    }

    internal static class init_net
    {
        public readonly static netns_ipv4 ipv4 = new netns_ipv4();
    }

}
