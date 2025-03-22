using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Threading;

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
        public readonly object Lock = new object();

        //
        // Queue of connections with operations to be processed.
        //
        quic_platform_cxplat_list_entry Connections;
        quic_platform_cxplat_list_entry** PriorityConnectionsTail;

        //
        // Queue of stateless operations to be processed.
        //
        quic_platform_cxplat_list_entry Operations;
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

    internal static partial class MSQuicFunc
    {
        static void QuicWorkerQueueConnection(QUIC_WORKER Worker, QUIC_CONNECTION Connection)
        {
            NetLog.Assert(Connection.Worker != null);
            bool ConnectionQueued = false;
            bool WakeWorkerThread = false;

            Monitor.Enter(Worker.Lock);

            if (!Connection->WorkerProcessing && !Connection->HasQueuedWork)
            {
                WakeWorkerThread = QuicWorkerIsIdle(Worker);
                Connection->Stats.Schedule.LastQueueTime = CxPlatTimeUs32();
                QuicTraceEvent(
                    ConnScheduleState,
                    "[conn][%p] Scheduling: %u",
                    Connection,
                    QUIC_SCHEDULE_QUEUED);
                QuicConnAddRef(Connection, QUIC_CONN_REF_WORKER);
                CxPlatListInsertTail(&Worker->Connections, &Connection->WorkerLink);
                ConnectionQueued = TRUE;
            }

            Connection->HasQueuedWork = TRUE;

            CxPlatDispatchLockRelease(&Worker->Lock);

            if (ConnectionQueued)
            {
                if (WakeWorkerThread)
                {
                    QuicWorkerThreadWake(Worker);
                }
                QuicPerfCounterIncrement(QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH);
            }
        }
    }
}
