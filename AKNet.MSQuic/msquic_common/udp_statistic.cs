/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:53
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
            //NetLog.Log("Quic UDP 统计信息: ");
            //foreach(QUIC_WORKER v in MsQuicApi.Api.Registration.WorkerPool.Workers)
            //{
            //    NetLog.Log($"分区信息: 处理器Id:{v.Partition.Processor} Index: {v.Partition.Index}");
            //    MSQuicFunc.QuicPartitionPrintPerfCounters(v.Partition);
            //}

            NetLog.Log("Quic UDP 所有分区总共统计信息: ");
            MSQuicFunc.QuicPerfCounterSnapShot(0);
            MSQuicFunc.udp_statistic_printInfo();
        }
    }
}
