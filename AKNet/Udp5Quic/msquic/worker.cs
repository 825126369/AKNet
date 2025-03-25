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
        static bool QuicWorkerIsIdle(QUIC_WORKER Worker)
        {
            return CxPlatListIsEmpty(Worker.Connections) && CxPlatListIsEmpty(Worker.Operations);
        }

        static long QuicWorkerInitialize(QUIC_REGISTRATION Registration, QUIC_EXECUTION_PROFILE ExecProfile, int PartitionIndex, QUIC_WORKER Worker)
        {
            Worker.Enabled = true;
            Worker.PartitionIndex = PartitionIndex;
            CxPlatListInitializeHead(Worker.Connections);
            Worker.PriorityConnectionsTail = Worker.Connections.Flink;
            CxPlatListInitializeHead(Worker.Operations);

            CxPlatPoolInitialize(false, sizeof(QUIC_STREAM), QUIC_POOL_STREAM, Worker.StreamPool);
            CxPlatPoolInitialize(false, sizeof(QUIC_RECV_CHUNK) + QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE, QUIC_POOL_SBUF, Worker.DefaultReceiveBufferPool);
            CxPlatPoolInitialize(false, sizeof(QUIC_SEND_REQUEST), QUIC_POOL_SEND_REQUEST, Worker.SendRequestPool);
            QuicSentPacketPoolInitialize(Worker.SentPacketPool);
            CxPlatPoolInitialize(false, sizeof(QUIC_API_CONTEXT), QUIC_POOL_API_CTX, Worker.ApiContextPool);
            CxPlatPoolInitialize(false, sizeof(QUIC_STATELESS_CONTEXT), QUIC_POOL_STATELESS_CTX, Worker.StatelessContextPool);
            CxPlatPoolInitialize(false, sizeof(QUIC_OPERATION), QUIC_POOL_OPER, Worker.OperPool);
            CxPlatPoolInitialize(false, sizeof(QUIC_RECV_CHUNK), QUIC_POOL_APP_BUFFER_CHUNK, Worker.AppBufferChunkPool);

            long Status = QuicTimerWheelInitialize(Worker.TimerWheel);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Worker.ExecutionContext.Context = Worker;
            Worker.ExecutionContext.Callback = QuicWorkerLoop;
            Worker.ExecutionContext.NextTimeUs = long.MaxValue;
            Worker.ExecutionContext.Ready = true;

            if (ExecProfile != QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT)
            {
                Worker.IsExternal = true;
                CxPlatAddExecutionContext(MsQuicLib.WorkerPool, Worker.ExecutionContext, PartitionIndex);
            }
            else
            {
                ushort ThreadFlags;
                switch (ExecProfile)
                {
                    default:
                    case QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_LOW_LATENCY:
                    case QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT:
                        ThreadFlags = (ushort)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_IDEAL_PROC;
                        break;
                    case QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER:
                        ThreadFlags = (ushort)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_NONE;
                        break;
                    case QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_REAL_TIME:
                        ThreadFlags = (ushort)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_AFFINITIZE | (ushort)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_HIGH_PRIORITY;
                        break;
                }

                if (MsQuicLib.ExecutionConfig != null)
                {
                    if (BoolOk((int)MsQuicLib.ExecutionConfig.Flags & (int)QUIC_EXECUTION_CONFIG_FLAGS.QUIC_EXECUTION_CONFIG_FLAG_HIGH_PRIORITY))
                    {
                        ThreadFlags |= (int)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_HIGH_PRIORITY;
                    }

                    if (BoolOk((int)MsQuicLib.ExecutionConfig.Flags & (int)QUIC_EXECUTION_CONFIG_FLAGS.QUIC_EXECUTION_CONFIG_FLAG_AFFINITIZE))
                    {
                        ThreadFlags |= (int)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_AFFINITIZE;
                    }
                }

                CXPLAT_THREAD_CONFIG ThreadConfig = new CXPLAT_THREAD_CONFIG();
                ThreadConfig.Flags = ThreadFlags;
                ThreadConfig.IdealProcessor = QuicLibraryGetPartitionProcessor(PartitionIndex);
                ThreadConfig.Name = "quic_worker";
                ThreadConfig.Callback = QuicWorkerThread;
                ThreadConfig.Context = Worker;

                Status = CxPlatThreadCreate(ThreadConfig, Worker.Thread);
                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }
            }

        Error:
            return Status;
        }

        static long QuicWorkerPoolInitialize(QUIC_REGISTRATION Registration, QUIC_EXECUTION_PROFILE ExecProfile, ref QUIC_WORKER_POOL NewWorkerPool)
        {
            int WorkerCount = ExecProfile == QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER ? 1 : MsQuicLib.PartitionCount;
            QUIC_WORKER_POOL WorkerPool = new_QUIC_WORKER_POOL();
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
                    goto Error;
                }
            }

            NewWorkerPool = WorkerPool;
        Error:
            return Status;
        }

        static void QuicWorkerQueueConnection(QUIC_WORKER Worker, QUIC_CONNECTION Connection)
        {
            NetLog.Assert(Connection.Worker != null);
            bool ConnectionQueued = false;
            bool WakeWorkerThread = false;

            Monitor.Enter(Worker.Lock);
            if (!Connection.WorkerProcessing && !Connection.HasQueuedWork)
            {
                WakeWorkerThread = QuicWorkerIsIdle(Worker);
                Connection.Stats.Schedule.LastQueueTime = mStopwatch.ElapsedMilliseconds;
                QuicConnAddRef(Connection, QUIC_CONN_REF_WORKER);
                CxPlatListInsertTail(Worker.Connections, Connection.WorkerLink);
                ConnectionQueued = true;
            }

            Connection.HasQueuedWork = true;
            Monitor.Exit(Worker.Lock);

            if (ConnectionQueued)
            {
                if (WakeWorkerThread)
                {
                    QuicWorkerThreadWake(Worker);
                }
                QuicPerfCounterIncrement(QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH);
            }
        }

        static void QuicWorkerResetQueueDelay(QUIC_WORKER Worker)
        {
            Worker.AverageQueueDelay = 0;
        }

        static bool QuicWorkerLoop(CXPLAT_EXECUTION_CONTEXT Context, CXPLAT_EXECUTION_STATE State)
        {
            QUIC_WORKER Worker = (QUIC_WORKER)Context;

            if (!Worker.Enabled)
            {
                QuicWorkerLoopCleanup(Worker);
                CxPlatEventSet(Worker.Done);
                return false;
            }

            if (!Worker.IsActive)
            {
                Worker.IsActive = true;
            }

            QuicPerfCounterTrySnapShot(State.TimeNow);

            if (Worker.TimerWheel.NextExpirationTime != long.MaxValue &&
                Worker.TimerWheel.NextExpirationTime <= State.TimeNow)
            {
                QuicWorkerProcessTimers(Worker, State.ThreadID, State.TimeNow);
                State.NoWorkCount = 0;
            }

            QUIC_CONNECTION Connection = QuicWorkerGetNextConnection(Worker);
            if (Connection != null)
            {
                QuicWorkerProcessConnection(Worker, Connection, State.ThreadID, State.TimeNow);
                Worker.ExecutionContext.Ready = true;
                State.NoWorkCount = 0;
            }

            QUIC_OPERATION Operation = QuicWorkerGetNextOperation(Worker);
            if (Operation != NULL)
            {
                QuicBindingProcessStatelessOperation(
                    Operation->Type,
                    Operation->STATELESS.Context);
                QuicOperationFree(Worker, Operation);
                QuicPerfCounterIncrement(QUIC_PERF_COUNTER_WORK_OPER_COMPLETED);
                Worker->ExecutionContext.Ready = TRUE;
                State->NoWorkCount = 0;
            }

            if (Worker.ExecutionContext.Ready)
            {
                return true;
            }

            if (MsQuicLib.ExecutionConfig && (uint64_t)MsQuicLib.ExecutionConfig->PollingIdleTimeoutUs >
                    CxPlatTimeDiff64(State.LastWorkTime, State.TimeNow))
            {
                Worker.ExecutionContext.Ready = true;
                return true;
            }
            
            Worker.IsActive = false;
            Worker.ExecutionContext.NextTimeUs = Worker.TimerWheel.NextExpirationTime;
            QuicWorkerResetQueueDelay(Worker);
            return true;
        }

        public static void QuicWorkerThread(QUIC_WORKER Context)
        {
            QUIC_WORKER Worker = (QUIC_WORKER)Context;
            CXPLAT_EXECUTION_CONTEXT EC = Worker.ExecutionContext;

            CXPLAT_EXECUTION_STATE State = new CXPLAT_EXECUTION_STATE();
            State.TimeNow = 0;
            State.LastWorkTime = 0;
            State.LastPoolProcessTime = 0;
            State.WaitTime = long.MaxValue;
            State.NoWorkCount = 0;
            State.ThreadID = Thread.CurrentThread.ManagedThreadId;

            while (true)
            {
                ++State.NoWorkCount;
                State.TimeNow = mStopwatch.ElapsedMilliseconds;
                if (!QuicWorkerLoop(EC, State))
                {
                    break;
                }

                BOOLEAN Ready = InterlockedFetchAndClearBoolean(&EC->Ready);
                if (!Ready)
                {
                    if (EC->NextTimeUs == UINT64_MAX)
                    {
                        CxPlatEventWaitForever(Worker->Ready);

                    }
                    else if (EC->NextTimeUs > State.TimeNow)
                    {
                        uint64_t Delay = US_TO_MS(EC->NextTimeUs - State.TimeNow) + 1;
                        if (Delay >= (uint64_t)UINT32_MAX)
                        {
                            Delay = UINT32_MAX - 1; // Max has special meaning for most platforms.
                        }
                        CxPlatEventWaitWithTimeout(Worker->Ready, (uint32_t)Delay);
                    }
                }
                if (State.NoWorkCount == 0)
                {
                    State.LastWorkTime = State.TimeNow;
                }
            }

            QuicTraceEvent(
                WorkerStop,
                "[wrkr][%p] Stop",
                Worker);
            CXPLAT_THREAD_RETURN(QUIC_STATUS_SUCCESS);
        }

        static void QuicWorkerQueuePriorityConnection(QUIC_WORKER Worker, QUIC_CONNECTION Connection)
        {
            NetLog.Assert(Connection.Worker != null);
            bool ConnectionQueued = false;
            bool WakeWorkerThread = false;

            Monitor.Enter(Worker.Lock);
            if (!Connection.WorkerProcessing && !Connection.HasPriorityWork)
            {
                if (!Connection.HasQueuedWork)
                {
                    WakeWorkerThread = QuicWorkerIsIdle(Worker);
                    Connection.Stats.Schedule.LastQueueTime = mStopwatch.ElapsedMilliseconds;

                    QuicTraceEvent(QuicEventId.ConnScheduleState, "[conn][%p] Scheduling: %u", Connection, QUIC_SCHEDULE_STATE.QUIC_SCHEDULE_QUEUED);
                    QuicConnAddRef(Connection,  QUIC_CONNECTION_REF.QUIC_CONN_REF_WORKER);
                    ConnectionQueued = true;
                }
                else
                {
                    CxPlatListEntryRemove(Connection.WorkerLink);
                }

                CxPlatListInsertTail(Worker.PriorityConnectionsTail, Connection.WorkerLink);
                Worker.PriorityConnectionsTail = Connection.WorkerLink.Flink;
                Connection.HasPriorityWork = true;
            }

            Connection.HasQueuedWork = true;
            Monitor.Exit(Worker.Lock);

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
