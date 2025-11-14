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
        //tcp_metrics，默认情况下，不启用哈，所以代码全部注释掉了
        static void tcp_init_metrics(tcp_sock tp)
        {
            if (tp.srtt_us == 0)
            {
                tp.rttvar_us = TCP_TIMEOUT_FALLBACK;
                tp.mdev_us = tp.mdev_max_us = tp.rttvar_us;
                tp.icsk_rto = TCP_TIMEOUT_FALLBACK;
            }
        }

    }

}
