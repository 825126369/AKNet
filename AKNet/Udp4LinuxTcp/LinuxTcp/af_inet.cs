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
    internal static partial class LinuxTcpFunc
    {
        static bool b_inet_init = false;
        static void inet_init(tcp_sock tp)
        {
            if (!b_inet_init)
            {
                b_inet_init = true;
                tcp_init();
            }
            inet_create(tp);
        }

        static void inet_create(tcp_sock tp)
        {
            sock_init_data(tp);
        }

    }
}
