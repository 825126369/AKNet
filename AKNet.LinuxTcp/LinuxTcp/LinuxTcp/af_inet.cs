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
