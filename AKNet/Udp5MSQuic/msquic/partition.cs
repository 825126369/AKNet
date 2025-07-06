namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_RETRY_KEY
    {
        public CXPLAT_KEY Key;
        public int Index;
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
            Partition.StreamPool.CxPlatPoolInitialize();
            Partition.DefaultReceiveBufferPool.CxPlatPoolInitialize(QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE);
            Partition.SendRequestPool.CxPlatPoolInitialize();
            Partition.StatelessContextPool.CxPlatPoolInitialize();
            Partition.OperPool.CxPlatPoolInitialize();
            Partition.AppBufferChunkPool.CxPlatPoolInitialize();
            QuicSentPacketPoolInitialize(Partition.SentPacketPool);
            return QUIC_STATUS_SUCCESS;
        }
    }

}
