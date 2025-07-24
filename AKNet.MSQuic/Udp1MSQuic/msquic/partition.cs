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
        public readonly QUIC_RETRY_KEY[] StatelessRetryKeys = new QUIC_RETRY_KEY[2];

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
    }

}
