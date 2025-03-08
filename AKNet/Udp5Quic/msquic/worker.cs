using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_WORKER_POOL
    {
        public ushort LastWorker;
        public List<QUIC_WORKER> Workers = new List<QUIC_WORKER>();
    }

    internal class QUIC_WORKER
    {
        CXPLAT_EXECUTION_CONTEXT ExecutionContext;

        //
        // Event to signal when the execution context (i.e. worker thread) is
        // complete.
        //
        CXPLAT_EVENT Done;

        //
        // Indicates if this work is handled by an external (to QUIC) execution context.
        //
        BOOLEAN IsExternal;

        //
        // TRUE if the worker is currently running.
        //
        BOOLEAN Enabled;

        //
        // TRUE if the worker is currently processing connections.
        //
        BOOLEAN IsActive;

        //
        // The index into the partition array (of processors).
        //
        uint16_t PartitionIndex;

        //
        // The average queue delay connections experience, in microseconds.
        //
        uint32_t AverageQueueDelay;

        //
        // Timers for the worker's connections.
        //
        QUIC_TIMER_WHEEL TimerWheel;

        //
        // An event to kick the thread.
        //
        CXPLAT_EVENT Ready;

        //
        // A thread for draining operations from queued connections.
        //
        CXPLAT_THREAD Thread;

        //
        // Serializes access to the connection and operation lists.
        //
        CXPLAT_DISPATCH_LOCK Lock;

        //
        // Queue of connections with operations to be processed.
        //
        CXPLAT_LIST_ENTRY Connections;
        CXPLAT_LIST_ENTRY** PriorityConnectionsTail;

        //
        // Queue of stateless operations to be processed.
        //
        CXPLAT_LIST_ENTRY Operations;
        uint32_t OperationCount;
        uint64_t DroppedOperationCount;

        CXPLAT_POOL StreamPool; // QUIC_STREAM
        CXPLAT_POOL DefaultReceiveBufferPool; // QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE
        CXPLAT_POOL SendRequestPool; // QUIC_SEND_REQUEST
        QUIC_SENT_PACKET_POOL SentPacketPool; // QUIC_SENT_PACKET_METADATA
        CXPLAT_POOL ApiContextPool; // QUIC_API_CONTEXT
        CXPLAT_POOL StatelessContextPool; // QUIC_STATELESS_CONTEXT
        CXPLAT_POOL OperPool; // QUIC_OPERATION
        CXPLAT_POOL AppBufferChunkPool; // QUIC_RECV_CHUNK

    }
}
