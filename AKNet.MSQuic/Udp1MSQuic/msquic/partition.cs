using AKNet.Common;
using System;
using System.Threading;

namespace AKNet.Udp1MSQuic.Common
{
    internal class QUIC_RETRY_KEY
    {
        public CXPLAT_KEY Key;
        public long Index;
    }

    internal class QUIC_PARTITION
    {
        public int Index;
        public int Processor;
        public ulong SendBatchId;
        public ulong SendPacketId;
        public ulong ReceivePacketId;
        
        public CXPLAT_HASH ResetTokenHash;
        public readonly object ResetTokenLock = new object();
        public readonly object StatelessRetryKeysLock = new object();
        public readonly QUIC_RETRY_KEY[] StatelessRetryKeys = new QUIC_RETRY_KEY[2]
        {
            new QUIC_RETRY_KEY(), new QUIC_RETRY_KEY()
        };

        public readonly CXPLAT_POOL<QUIC_CONNECTION> ConnectionPool = new CXPLAT_POOL<QUIC_CONNECTION>();
        public readonly CXPLAT_POOL<QUIC_TRANSPORT_PARAMETERS> TransportParamPool = new CXPLAT_POOL<QUIC_TRANSPORT_PARAMETERS>();
        public readonly CXPLAT_POOL<QUIC_PACKET_SPACE> PacketSpacePool = new CXPLAT_POOL<QUIC_PACKET_SPACE>();
        public readonly CXPLAT_POOL<QUIC_STREAM> StreamPool = new CXPLAT_POOL<QUIC_STREAM>(); // QUIC_STREAM
        public readonly DefaultReceiveBufferPool DefaultReceiveBufferPool = new DefaultReceiveBufferPool(); // QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE
        public readonly CXPLAT_POOL<QUIC_SEND_REQUEST> SendRequestPool = new CXPLAT_POOL<QUIC_SEND_REQUEST>(); // QUIC_SEND_REQUEST
        public readonly QUIC_SENT_PACKET_POOL SentPacketPool = new QUIC_SENT_PACKET_POOL(); // QUIC_SENT_PACKET_METADATA
        public readonly CXPLAT_POOL<QUIC_API_CONTEXT> ApiContextPool = new CXPLAT_POOL<QUIC_API_CONTEXT>(); // QUIC_API_CONTEXT
        public readonly CXPLAT_POOL<QUIC_STATELESS_CONTEXT> StatelessContextPool = new CXPLAT_POOL<QUIC_STATELESS_CONTEXT>(); // QUIC_STATELESS_CONTEXT
        public readonly CXPLAT_POOL<QUIC_OPERATION> OperPool = new CXPLAT_POOL<QUIC_OPERATION>(); // QUIC_OPERATION
        public readonly CXPLAT_POOL<QUIC_RECV_CHUNK> AppBufferChunkPool = new CXPLAT_POOL<QUIC_RECV_CHUNK>(); // QUIC_RECV_CHUNK

        public readonly long[] PerfCounters = new long[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_MAX];

        
    }

    internal static partial class MSQuicFunc
    {
        static int QuicPartitionInitialize(QUIC_PARTITION Partition, int Index, int Processor, CXPLAT_HASH_TYPE HashType, QUIC_SSBuffer ResetHashKey)
        {
            int Status = CxPlatHashCreate(HashType, ResetHashKey, out Partition.ResetTokenHash);
            if (QUIC_FAILED(Status))
            {
                return Status;
            }

            Partition.Index = Index;
            Partition.Processor = Processor;

            Partition.ConnectionPool.CxPlatPoolInitialize();
            Partition.TransportParamPool.CxPlatPoolInitialize();
            Partition.PacketSpacePool.CxPlatPoolInitialize();
            Partition.StreamPool.CxPlatPoolInitialize();
            Partition.DefaultReceiveBufferPool.CxPlatPoolInitialize(QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE);
            Partition.SendRequestPool.CxPlatPoolInitialize();
            QuicSentPacketPoolInitialize(Partition.SentPacketPool);
            Partition.ApiContextPool.CxPlatPoolInitialize();
            Partition.StatelessContextPool.CxPlatPoolInitialize();
            Partition.OperPool.CxPlatPoolInitialize();
            Partition.AppBufferChunkPool.CxPlatPoolInitialize();

            return QUIC_STATUS_SUCCESS;
        }

        static void QuicPartitionUninitialize(QUIC_PARTITION Partition)
        {
            for (int i = 0; i < Partition.StatelessRetryKeys.Length; ++i)
            {
                Partition.StatelessRetryKeys[i].Key = null;
            }

            Partition.ConnectionPool.CxPlatPoolUninitialize();
            Partition.TransportParamPool.CxPlatPoolUninitialize();
            Partition.PacketSpacePool.CxPlatPoolUninitialize();
            Partition.StreamPool.CxPlatPoolUninitialize();
            Partition.DefaultReceiveBufferPool.CxPlatPoolUninitialize();
            Partition.SendRequestPool.CxPlatPoolUninitialize();
            QuicSentPacketPoolUninitialize(Partition.SentPacketPool);
            Partition.ApiContextPool.CxPlatPoolUninitialize();
            Partition.StatelessContextPool.CxPlatPoolUninitialize();
            Partition.OperPool.CxPlatPoolUninitialize();
            Partition.AppBufferChunkPool.CxPlatPoolUninitialize();
            Partition.ResetTokenHash = null;
        }

        static void QuicPerfCounterAdd(QUIC_PARTITION Partition, QUIC_PERFORMANCE_COUNTERS Type, long Value = 1)
        {
            NetLog.Assert(Type >= 0 && Type < QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_MAX);
            Interlocked.Add(ref Partition.PerfCounters[(int)Type], Value);
        }

        static void QuicPerfCounterIncrement(QUIC_PARTITION Partition, QUIC_PERFORMANCE_COUNTERS Type)
        {
            QuicPerfCounterAdd(Partition, Type, 1);
        }

        static void QuicPerfCounterDecrement(QUIC_PARTITION Partition, QUIC_PERFORMANCE_COUNTERS Type)
        {
            QuicPerfCounterAdd(Partition, Type, -1);
        }

        static CXPLAT_KEY QuicPartitioGetStatelessRetryKey(QUIC_PARTITION Partition, long KeyIndex)
        {
            if (Partition.StatelessRetryKeys[KeyIndex & 1].Index == KeyIndex)
            {
                return Partition.StatelessRetryKeys[KeyIndex & 1].Key;
            }
            
            QUIC_SSBuffer RawKey = new byte[(int)CXPLAT_AEAD_TYPE_SIZE.CXPLAT_AEAD_AES_256_GCM_SIZE];
            MsQuicLib.BaseRetrySecret.AsSpan().CopyTo(RawKey.GetSpan());

            for (int i = 0; i < sizeof(long); ++i)
            {
                RawKey[i] ^= (byte)(KeyIndex >> (sizeof(long) - i - 1) * 8);
            }

            CXPLAT_KEY NewKey = null;
            int Status = CxPlatKeyCreate(CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM, RawKey, ref NewKey);
            if (QUIC_FAILED(Status))
            {
                return null;
            }

            CxPlatKeyFree(Partition.StatelessRetryKeys[KeyIndex & 1].Key);
            Partition.StatelessRetryKeys[KeyIndex & 1].Key = NewKey;
            Partition.StatelessRetryKeys[KeyIndex & 1].Index = KeyIndex;

            return NewKey;
        }


        static CXPLAT_KEY QuicPartitionGetCurrentStatelessRetryKey(QUIC_PARTITION Partition)
        {
            long Now = CxPlatTimeEpochMs64();
            long KeyIndex = Now / QUIC_STATELESS_RETRY_KEY_LIFETIME_MS;
            return QuicPartitioGetStatelessRetryKey(Partition, KeyIndex);
        }


        static CXPLAT_KEY QuicPartitionGetStatelessRetryKeyForTimestamp(QUIC_PARTITION Partition,long Timestamp)
        {
            long Now = CxPlatTimeEpochMs64();
            long CurrentKeyIndex = Now / QUIC_STATELESS_RETRY_KEY_LIFETIME_MS;
            long KeyIndex = Timestamp / QUIC_STATELESS_RETRY_KEY_LIFETIME_MS;

            if (KeyIndex < CurrentKeyIndex - 1 || KeyIndex > CurrentKeyIndex)
            {
                return null;
            }

            return QuicPartitioGetStatelessRetryKey(Partition, KeyIndex);
        }

        //性能计数器描述
        static readonly string[] QUIC_PERFORMANCE_COUNTERS_DESC = new string[]
        {
            "创建的总连接数",
            "失败的连接总数",
            "被应用程序层主动拒绝的连接尝试总数",
            "成功恢复的连接总数",
            "当前处于活动状态（已分配）的连接数。",
            "当前处于“已连接”状态（即握手完成，数据可以传输）的连接数",
            "因协议错误（如无效帧、违反状态机规则）而关闭的连接总数。",
            "因客户端和服务器无法就应用层协议（如 h3(自定义的协议))，达成一致而被拒绝的连接尝试总数。",
            "当前处于活动状态的流（stream）总数。",
            "根据 ACK 信息推断出的疑似丢失的数据包总数。这是网络质量（特别是丢包率）的关键指标。",
            "因任何原因（格式错误、缓冲区溢出）被 QUIC 实现直接丢弃的数据包总数。",
            "解密失败的数据包总数。这可能由密钥错误、数据损坏或重放攻击引起。",
            "接收到的 UDP 数据报总数。",
            "发送的 UDP 数据报总数。",
            "接收到的 UDP 负载（payload）总字节数（不包括 UDP/IP 头）。",
            "发送的 UDP 负载（payload）总字节数（不包括 UDP/IP 头）。",
            "从底层网络接收到 UDP 数据报的事件总数（可能一次事件接收多个数据报）。",
            "调用底层 UDP 发送 API 的总次数（一次调用可能发送多个数据报）。",
            "应用程序通过 QUIC 连接发送的总字节数（在 QUIC 层之上测量）。",
            "应用程序通过 QUIC 连接接收到的总字节数",
            "当前排队等待处理的连接数（例如，新连接请求）",
            "当前排队等待处理的连接级操作数。",
            "历史累计的连接级操作入队总数。",
            "历史累计的已完成连接级操作总数。",
            "当前排队等待处理的工作线程操作数。",
            "历史累计的工作线程操作入队总数。",
            "历史累计的已完成工作线程操作总数 ",
            "成功验证的网络路径（IP地址/端口对）挑战总数", //QUIC_PERF_COUNTER_PATH_VALIDATED,
            "验证失败的网络路径挑战总数。", //QUIC_PERF_COUNTER_PATH_FAILURE
            "发送的无状态重置（stateless reset）数据包总数", //QUIC_PERF_COUNTER_SEND_STATELESS_RESET,
            "发送的无状态重试（stateless retry）数据包总数。", //QUIC_PERF_COUNTER_SEND_STATELESS_RETRY。
            "因QUIC实例内部工作负载过高（例如，CPU、内存或队列满）而被拒绝的连接总数。这是系统过载的信号。", //QUIC_PERF_COUNTER_CONN_LOAD_REJECT,
            " ", //QUIC_PERF_COUNTER_MAX,
        };

        public static void QuicPartitionPrintPerfCounters(QUIC_PARTITION Partition)
        {
            for(int i = 0; i < (int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_MAX; i++)
            {
                QUIC_PERFORMANCE_COUNTERS nType = (QUIC_PERFORMANCE_COUNTERS)i;
                long nCount = Partition.PerfCounters[i];
                NetLog.Log($"{nType} {QUIC_PERFORMANCE_COUNTERS_DESC[i]} : {nCount}");
            }
        }

    }

}
