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
        public Thread Thread;
        public CXPLAT_EXECUTION_CONTEXT ExecutionContext;
        public Action Ready;
        public Action Done;

        public bool IsExternal;
        public bool Enabled;
        public bool IsActive;
        public int PartitionIndex;
        public int AverageQueueDelay;
        public QUIC_TIMER_WHEEL TimerWheel;
        public readonly object Lock = new object();
        public CXPLAT_LIST_ENTRY Connections;

        public CXPLAT_LIST_ENTRY PriorityConnectionsTail;
        public CXPLAT_LIST_ENTRY Operations;

        public int OperationCount;
        public int DroppedOperationCount;

        public CXPLAT_POOL StreamPool; // QUIC_STREAM
        public CXPLAT_POOL DefaultReceiveBufferPool; // QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE
        public CXPLAT_POOL SendRequestPool; // QUIC_SEND_REQUEST
        public QUIC_SENT_PACKET_POOL SentPacketPool; // QUIC_SENT_PACKET_METADATA
        public CXPLAT_POOL ApiContextPool; // QUIC_API_CONTEXT
        public CXPLAT_POOL StatelessContextPool; // QUIC_STATELESS_CONTEXT
        public CXPLAT_POOL OperPool; // QUIC_OPERATION
        public CXPLAT_POOL AppBufferChunkPool; // QUIC_RECV_CHUNK
    }

    internal static partial class MSQuicFunc
    {
        static long QuicWorkerInitialize(QUIC_REGISTRATION Registration,QUIC_EXECUTION_PROFILE ExecProfile,int PartitionIndex, QUIC_WORKER Worker)        
        {
            Worker.Enabled = true;
            Worker.PartitionIndex = PartitionIndex;
            CxPlatListInitializeHead(Worker.Connections);
            Worker.PriorityConnectionsTail = Worker.Connections.Flink;
            CxPlatListInitializeHead(Worker.Operations);

                CxPlatPoolInitialize(false, sizeof(QUIC_STREAM), QUIC_POOL_STREAM, &Worker->StreamPool);
                CxPlatPoolInitialize(false, sizeof(QUIC_RECV_CHUNK)+QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE, QUIC_POOL_SBUF, &Worker->DefaultReceiveBufferPool);
                CxPlatPoolInitialize(false, sizeof(QUIC_SEND_REQUEST), QUIC_POOL_SEND_REQUEST, &Worker->SendRequestPool);
                QuicSentPacketPoolInitialize(Worker.SentPacketPool);
                CxPlatPoolInitialize(false, sizeof(QUIC_API_CONTEXT), QUIC_POOL_API_CTX, &Worker->ApiContextPool);
                CxPlatPoolInitialize(false, sizeof(QUIC_STATELESS_CONTEXT), QUIC_POOL_STATELESS_CTX, &Worker->StatelessContextPool);
                CxPlatPoolInitialize(false, sizeof(QUIC_OPERATION), QUIC_POOL_OPER, &Worker->OperPool);
                CxPlatPoolInitialize(false, sizeof(QUIC_RECV_CHUNK), QUIC_POOL_APP_BUFFER_CHUNK, &Worker->AppBufferChunkPool);

                QUIC_STATUS Status = QuicTimerWheelInitialize(&Worker->TimerWheel);
            if (QUIC_FAILED(Status)) {
                goto Error;
            }

            Worker->ExecutionContext.Context = Worker;
            Worker->ExecutionContext.Callback = QuicWorkerLoop;
            Worker->ExecutionContext.NextTimeUs = UINT64_MAX;
            Worker->ExecutionContext.Ready = TRUE;

        #ifndef _KERNEL_MODE // Not supported on kernel mode
            if (ExecProfile != QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT) {
                Worker->IsExternal = TRUE;
                CxPlatAddExecutionContext(&MsQuicLib.WorkerPool, &Worker->ExecutionContext, PartitionIndex);
        } else
        #endif // _KERNEL_MODE
        {
            uint16_t ThreadFlags;
            switch (ExecProfile)
            {
                default:
                case QUIC_EXECUTION_PROFILE_LOW_LATENCY:
                case QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT:
                    ThreadFlags = CXPLAT_THREAD_FLAG_SET_IDEAL_PROC;
                    break;
                case QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER:
                    ThreadFlags = CXPLAT_THREAD_FLAG_NONE;
                    break;
                case QUIC_EXECUTION_PROFILE_TYPE_REAL_TIME:
                    ThreadFlags = CXPLAT_THREAD_FLAG_SET_AFFINITIZE | CXPLAT_THREAD_FLAG_HIGH_PRIORITY;
                    break;
            }

            if (MsQuicLib.ExecutionConfig)
            {
                if (MsQuicLib.ExecutionConfig->Flags & QUIC_EXECUTION_CONFIG_FLAG_HIGH_PRIORITY)
                {
                    ThreadFlags |= CXPLAT_THREAD_FLAG_HIGH_PRIORITY;
                }
                if (MsQuicLib.ExecutionConfig->Flags & QUIC_EXECUTION_CONFIG_FLAG_AFFINITIZE)
                {
                    ThreadFlags |= CXPLAT_THREAD_FLAG_SET_AFFINITIZE;
                }
            }

            CXPLAT_THREAD_CONFIG ThreadConfig = {
                    ThreadFlags,
                    QuicLibraryGetPartitionProcessor(PartitionIndex),
                    "quic_worker",
                    QuicWorkerThread,
                    Worker
                };

            Status = CxPlatThreadCreate(&ThreadConfig, &Worker->Thread);
            if (QUIC_FAILED(Status))
            {
                QuicTraceEvent(
                    WorkerErrorStatus,
                    "[wrkr][%p] ERROR, %u, %s.",
                    Worker,
                    Status,
                    "CxPlatThreadCreate");
                goto Error;
            }
        }

        Error:

        if (QUIC_FAILED(Status))
        {
            CxPlatEventSet(Worker->Done);
            QuicWorkerUninitialize(Worker);
        }

        return Status;
        }

        static long QuicWorkerPoolInitialize(QUIC_REGISTRATION Registration, QUIC_EXECUTION_PROFILE ExecProfile, ref QUIC_WORKER_POOL NewWorkerPool)
        {
            int WorkerCount = ExecProfile == QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER ? 1 : MsQuicLib.PartitionCount;
            QUIC_WORKER_POOL WorkerPool = CXPLAT_ALLOC_NONPAGED(WorkerPoolSize, QUIC_POOL_WORKER);
            if (WorkerPool == null)
            {
                return QUIC_STATUS_OUT_OF_MEMORY;
            }

            long Status = QUIC_STATUS_SUCCESS;
            for (int i = 0; i < WorkerCount; i++)
            {
                Status = QuicWorkerInitialize(Registration, ExecProfile, i, WorkerPool.Workers[i]);
                if (QUIC_FAILED(Status))
                {
                    for (int j = 0; j < i; j++)
                    {
                        QuicWorkerUninitialize(WorkerPool.Workers[j]);
                    }
                    goto Error;
                }
            }

            NewWorkerPool = WorkerPool;

        Error:
            if (QUIC_FAILED(Status))
            {
                CXPLAT_FREE(WorkerPool, QUIC_POOL_WORKER);
            }
            return Status;
        }

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
