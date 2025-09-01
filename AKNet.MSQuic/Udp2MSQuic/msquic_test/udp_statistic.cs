using AKNet.Common;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TestNetClient")]
namespace AKNet.Udp2MSQuic.Common
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

            MSQuicFunc.QuicPerfCounterSnapShot(0);
            NetLog.Log("Quic UDP 所有分区总共统计信息: ");
            MSQuicFunc.QuicPrintPerfCounters();
        }
    }
}
