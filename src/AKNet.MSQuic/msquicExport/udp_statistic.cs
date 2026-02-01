/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:01
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TestNetClient")]
namespace AKNet.Common
{
    internal static class udp_statistic
    {
        public static void PrintInfo()
        {
            NetLog.Log("-----------");
            NetLog.Log("Quic UDP 所有分区总共统计信息: ");
            MSQuicFunc.QuicPerfCounterSnapShot(0);
            MSQuicFunc.udp_statistic_printInfo();
        }
    }
}
