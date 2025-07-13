using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_WORKER_POOL
    {
        public ushort LastWorker;
        public List<QUIC_WORKER> Workers = new List<QUIC_WORKER>();
    }

    internal class QUIC_WORKER
    {
        public readonly CXPLAT_EXECUTION_CONTEXT ExecutionContext = new CXPLAT_EXECUTION_CONTEXT();
        public QUIC_PARTITION Partition;
        public Thread Thread;
        public EventWaitHandle Ready = null;
        public EventWaitHandle Done = null;

        public bool IsExternal;
        public bool Enabled;
        public bool IsActive;
        public int PartitionIndex;
        public long AverageQueueDelay;
        public int OperationCount;
        public int DroppedOperationCount;

        public readonly object Lock = new object();
        public readonly QUIC_TIMER_WHEEL TimerWheel = new QUIC_TIMER_WHEEL();

        public readonly CXPLAT_LIST_ENTRY<QUIC_CONNECTION> Connections = new CXPLAT_LIST_ENTRY<QUIC_CONNECTION>(null);
        public CXPLAT_LIST_ENTRY PriorityConnectionsTail;
        public readonly CXPLAT_LIST_ENTRY<QUIC_OPERATION> Operations = new CXPLAT_LIST_ENTRY<QUIC_OPERATION>(null);
    }

    internal static partial class MSQuicFunc
    {
        static bool QuicWorkerIsOverloaded(QUIC_WORKER Worker)
        {
            return Worker.AverageQueueDelay > MsQuicLib.Settings.MaxWorkerQueueDelayUs;
        }

        static bool QuicWorkerIsIdle(QUIC_WORKER Worker)
        {
            return CxPlatListIsEmpty(Worker.Connections) && CxPlatListIsEmpty(Worker.Operations);
        }

        static void QuicWorkerAssignConnection(QUIC_WORKER Worker,QUIC_CONNECTION Connection)
        {
            NetLog.Assert(Connection.Worker != Worker);
            Connection.Worker = Worker;
        }

        static int QuicWorkerInitialize(QUIC_REGISTRATION Registration, QUIC_EXECUTION_PROFILE ExecProfile, QUIC_PARTITION Partition, QUIC_WORKER Worker)
        {
            Worker.Enabled = true;
            Worker.Partition = Partition;
            CxPlatEventInitialize(out Worker.Done, true, false);
            CxPlatEventInitialize(out Worker.Ready, false, false);
            CxPlatListInitializeHead(Worker.Connections);
            Worker.PriorityConnectionsTail = Worker.Connections;
            CxPlatListInitializeHead(Worker.Operations);

            int Status = QuicTimerWheelInitialize(Worker.TimerWheel);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Worker.ExecutionContext.Context = Worker;
            Worker.ExecutionContext.Callback = QuicWorkerLoop;
            Worker.ExecutionContext.NextTimeUs = long.MaxValue;
            Worker.ExecutionContext.Ready = 1;
            
            if (!_KERNEL_MODE && ExecProfile != QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT)
            {
                Worker.IsExternal = true;
                CxPlatWorkerPoolAddExecutionContext(MsQuicLib.WorkerPool, Worker.ExecutionContext, Partition.Index);
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
                    if (MsQuicLib.ExecutionConfig.Flags.HasFlag(QUIC_GLOBAL_EXECUTION_CONFIG_FLAGS.QUIC_GLOBAL_EXECUTION_CONFIG_FLAG_HIGH_PRIORITY))
                    {
                        ThreadFlags |= (int)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_HIGH_PRIORITY;
                    }

                    if (MsQuicLib.ExecutionConfig.Flags.HasFlag(QUIC_GLOBAL_EXECUTION_CONFIG_FLAGS.QUIC_GLOBAL_EXECUTION_CONFIG_FLAG_AFFINITIZE))
                    {
                        ThreadFlags |= (int)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_AFFINITIZE;
                    }
                }

                CXPLAT_THREAD_CONFIG ThreadConfig = new CXPLAT_THREAD_CONFIG();
                ThreadConfig.Flags = ThreadFlags;
                ThreadConfig.IdealProcessor = Partition.Processor;
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

        static int QuicWorkerPoolInitialize(QUIC_REGISTRATION Registration, QUIC_EXECUTION_PROFILE ExecProfile, out QUIC_WORKER_POOL NewWorkerPool)
        {
            NewWorkerPool = null;
            int WorkerCount = ExecProfile == QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER ? 1 : MsQuicLib.PartitionCount;
            QUIC_WORKER_POOL WorkerPool = new QUIC_WORKER_POOL();
            if (WorkerPool == null)
            {
                return QUIC_STATUS_OUT_OF_MEMORY;
            }

            int Status = QUIC_STATUS_SUCCESS;
            for (int i = 0; i < WorkerCount; i++)
            {
                WorkerPool.Workers.Add(new QUIC_WORKER());
                Status = QuicWorkerInitialize(Registration, ExecProfile, MsQuicLib.Partitions[i], WorkerPool.Workers[i]);
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
            return Status;
        }

        static void QuicWorkerUninitialize(QUIC_WORKER Worker)
        {
            Worker.Enabled = false;
            if (Worker.ExecutionContext.Context != null)
            {
                if (MsQuicLib.CustomExecutions)
                {
                    QuicWorkerLoopCleanup(Worker);
                }
                else
                {
                    QuicWorkerThreadWake(Worker);
                    CxPlatEventWaitForever(Worker.Done);
                }
            }
            CxPlatEventUninitialize(Worker.Done);

            if (!Worker.IsExternal)
            {
                if (Worker.Thread != null)
                {
                    CxPlatThreadWait(Worker.Thread);
                    CxPlatThreadDelete(Worker.Thread);
                }
            }
            CxPlatEventUninitialize(Worker.Ready);

            NetLog.Assert(CxPlatListIsEmpty(Worker.Connections));
            Worker.PriorityConnectionsTail = Worker.Connections;
            NetLog.Assert(CxPlatListIsEmpty(Worker.Operations));;
            QuicTimerWheelUninitialize(Worker.TimerWheel);
        }

        static void QuicWorkerQueueConnection(QUIC_WORKER Worker, QUIC_CONNECTION Connection)
        {
            NetLog.Assert(Connection.Worker != null);
            bool ConnectionQueued = false;
            bool WakeWorkerThread = false;

            CxPlatDispatchLockAcquire(Worker.Lock);
            if (!Connection.WorkerProcessing && !Connection.HasQueuedWork)
            {
                WakeWorkerThread = QuicWorkerIsIdle(Worker);
                Connection.Stats.Schedule.LastQueueTime = CxPlatTimeUs();
                QuicConnAddRef(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_WORKER);
                CxPlatListInsertTail(Worker.Connections, Connection.WorkerLink);
                ConnectionQueued = true;
            }

            Connection.HasQueuedWork = true;
            CxPlatDispatchLockRelease(Worker.Lock);

            if (ConnectionQueued)
            {
                if (WakeWorkerThread)
                {
                    QuicWorkerThreadWake(Worker);
                }
                QuicPerfCounterIncrement(Worker.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH);
            }
        }

        static void QuicWorkerResetQueueDelay(QUIC_WORKER Worker)
        {
            Worker.AverageQueueDelay = 0;
        }

        static void QuicWorkerLoopCleanup(QUIC_WORKER Worker)
        {
            long Dequeue = 0;
            while (!CxPlatListIsEmpty(Worker.Connections))
            {
                QUIC_CONNECTION Connection = CXPLAT_CONTAINING_RECORD<QUIC_CONNECTION>(CxPlatListRemoveHead(Worker.Connections));
                if (Worker.PriorityConnectionsTail == Connection.WorkerLink)
                {
                    Worker.PriorityConnectionsTail = Worker.Connections;
                }
                if (!Connection.State.ExternalOwner)
                {
                    QuicConnOnShutdownComplete(Connection);
                }
                QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_WORKER);
                --Dequeue;
            }
            QuicPerfCounterAdd(Worker.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH, Dequeue);

            Dequeue = 0;
            while (!CxPlatListIsEmpty(Worker.Operations))
            {
                QUIC_OPERATION Operation = CXPLAT_CONTAINING_RECORD<QUIC_OPERATION>(CxPlatListRemoveHead(Worker.Operations));
                QuicOperationFree(Operation);
                --Dequeue;
            }
            QuicPerfCounterAdd(Worker.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_WORK_OPER_QUEUE_DEPTH, Dequeue);
        }

        static void QuicWorkerProcessTimers(QUIC_WORKER Worker, int ThreadID, long TimeNow)
        {
            CXPLAT_LIST_ENTRY ExpiredTimers = new CXPLAT_LIST_ENTRY<QUIC_CONNECTION>(null);
            CxPlatListInitializeHead(ExpiredTimers);
            QuicTimerWheelGetExpired(Worker.TimerWheel, TimeNow, ExpiredTimers);

            while (!CxPlatListIsEmpty(ExpiredTimers))
            {
                CXPLAT_LIST_ENTRY Entry = CxPlatListRemoveHead(ExpiredTimers);
                Entry.Next = null;
                QUIC_CONNECTION Connection = CXPLAT_CONTAINING_RECORD<QUIC_CONNECTION>(Entry);

                Connection.WorkerThreadID = ThreadID;
                QuicConnTimerExpired(Connection, TimeNow);
                Connection.WorkerThreadID = 0;
                QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_WORKER);
            }
        }

        static QUIC_CONNECTION QuicWorkerGetNextConnection(QUIC_WORKER Worker)
        {
            QUIC_CONNECTION Connection = null;
            if (Worker.Enabled && !CxPlatListIsEmpty(Worker.Connections))
            {
                CxPlatDispatchLockAcquire(Worker.Lock);
                if (!CxPlatListIsEmpty(Worker.Connections))
                {
                    Connection = CXPLAT_CONTAINING_RECORD<QUIC_CONNECTION>(CxPlatListRemoveHead(Worker.Connections));
                    if (Worker.PriorityConnectionsTail == Connection.WorkerLink) //这里表达的意思应该是 移除的链接，刚好是这个末尾
                    {
                        Worker.PriorityConnectionsTail = Worker.Connections;
                    }

                    NetLog.Assert(!Connection.WorkerProcessing);
                    NetLog.Assert(Connection.HasQueuedWork);
                    Connection.HasQueuedWork = false;
                    Connection.HasPriorityWork = false;
                    Connection.WorkerProcessing = true;
                    QuicPerfCounterDecrement(Worker.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH);
                }
                CxPlatDispatchLockRelease(Worker.Lock);
            }
            return Connection;
        }

        static void QuicWorkerUpdateQueueDelay(QUIC_WORKER Worker,long TimeInQueueUs)
        {
            Worker.AverageQueueDelay = (7 * Worker.AverageQueueDelay + TimeInQueueUs) / 8;
        }

        static void QuicWorkerProcessConnection(QUIC_WORKER Worker, QUIC_CONNECTION Connection, int ThreadID, long TimeNow)
        {
            if (Connection.Stats.Schedule.LastQueueTime != 0)
            {
                long Delay = CxPlatTimeDiff(Connection.Stats.Schedule.LastQueueTime, TimeNow);
                if (Delay >= int.MaxValue || Delay < 0)
                {
                    Delay = 0;
                }
                QuicWorkerUpdateQueueDelay(Worker, Delay);
            }

            Connection.WorkerThreadID = ThreadID;
            Connection.Stats.Schedule.DrainCount++;

            if (Connection.State.UpdateWorker)
            {
                Connection.State.UpdateWorker = false;
                QuicTimerWheelUpdateConnection(Worker.TimerWheel, Connection);

                QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_IDEAL_PROCESSOR_CHANGED;
                Event.IDEAL_PROCESSOR_CHANGED.IdealProcessor = Worker.Partition.Processor;
                Event.IDEAL_PROCESSOR_CHANGED.PartitionIndex = Worker.PartitionIndex;
                QuicConnIndicateEvent(Connection, ref Event);
            }
            
            bool StillHasPriorityWork = false;
            bool StillHasWorkToDo = QuicConnDrainOperations(Connection, ref StillHasPriorityWork) | Connection.State.UpdateWorker;
            Connection.WorkerThreadID = 0;

            CxPlatDispatchLockAcquire(Worker.Lock);
            Connection.WorkerProcessing = false;
            Connection.HasQueuedWork |= StillHasWorkToDo;

            bool DoneWithConnection = true;
            if (!Connection.State.UpdateWorker)
            {
                if (Connection.HasQueuedWork)
                {
                    Connection.Stats.Schedule.LastQueueTime = CxPlatTimeUs();
                    if (StillHasPriorityWork)
                    {
                        CxPlatListInsertTail(Worker.PriorityConnectionsTail, Connection.WorkerLink);
                        Worker.PriorityConnectionsTail = Worker.PriorityConnectionsTail.Next;
                        Connection.HasPriorityWork = true;
                    }
                    else
                    {
                        CxPlatListInsertTail(Worker.Connections, Connection.WorkerLink);
                    }
                    DoneWithConnection = false;
                }
            }

            CxPlatDispatchLockRelease(Worker.Lock);
            if (DoneWithConnection)
            {
                if (Connection.State.UpdateWorker)
                {
                    QuicTimerWheelRemoveConnection(Worker.TimerWheel, Connection);
                    NetLog.Assert(Connection.Registration != null);
                    QuicRegistrationQueueNewConnection(Connection.Registration, Connection);
                    NetLog.Assert(Worker != Connection.Worker);
                    QuicWorkerMoveConnection(Connection.Worker, Connection, StillHasPriorityWork);
                }
                QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_WORKER);
            }
        }

        static void QuicWorkerMoveConnection(QUIC_WORKER Worker, QUIC_CONNECTION Connection, bool IsPriority)
        {
            NetLog.Assert(Connection.Worker != null);
            NetLog.Assert(Connection.HasQueuedWork);
            CxPlatDispatchLockAcquire(Worker.Lock); 

            bool WakeWorkerThread = QuicWorkerIsIdle(Worker);
            Connection.Stats.Schedule.LastQueueTime = CxPlatTimeUs();
            if (IsPriority)
            {
                CxPlatListInsertTail(Worker.PriorityConnectionsTail, Connection.WorkerLink);
                Worker.PriorityConnectionsTail = Worker.PriorityConnectionsTail.Next;
                Connection.HasPriorityWork = true;
            }
            else
            {
                CxPlatListInsertTail(Worker.Connections, Connection.WorkerLink);
            }

            QuicConnAddRef(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_WORKER);
            CxPlatDispatchLockRelease(Worker.Lock);
            if (WakeWorkerThread)
            {
                QuicWorkerThreadWake(Worker);
            }
        }

        static QUIC_OPERATION QuicWorkerGetNextOperation(QUIC_WORKER Worker)
        {
            QUIC_OPERATION Operation = null;
            if (Worker.Enabled && Worker.OperationCount != 0)
            {
                CxPlatDispatchLockAcquire(Worker.Lock);
                Operation = CXPLAT_CONTAINING_RECORD<QUIC_OPERATION>(CxPlatListRemoveHead(Worker.Operations));
#if DEBUG
                Operation.Link.Next = null;
#endif
                Worker.OperationCount--;
                QuicPerfCounterDecrement(Worker.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_WORK_OPER_QUEUE_DEPTH);
                CxPlatDispatchLockRelease(Worker.Lock);
            }
            return Operation;
        }

        static bool QuicWorkerLoop(QUIC_WORKER Worker, CXPLAT_EXECUTION_STATE State)
        {
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
            if (Worker.TimerWheel.NextExpirationTime != long.MaxValue && Worker.TimerWheel.NextExpirationTime <= State.TimeNow)
            {
                QuicWorkerProcessTimers(Worker, State.ThreadID, State.TimeNow);
                State.NoWorkCount = 0;
            }

            QUIC_CONNECTION Connection = QuicWorkerGetNextConnection(Worker);
            if (Connection != null)
            {
                //在这里 处理命令
                QuicWorkerProcessConnection(Worker, Connection, State.ThreadID, State.TimeNow);
                Worker.ExecutionContext.Ready = 1;
                State.NoWorkCount = 0;
            }

            QUIC_OPERATION Operation = QuicWorkerGetNextOperation(Worker);
            if (Operation != null)
            {
                QuicBindingProcessStatelessOperation(Operation.Type, Operation.STATELESS.Context);
                QuicOperationFree(Operation);
                QuicPerfCounterIncrement(Worker.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_WORK_OPER_COMPLETED);
                Worker.ExecutionContext.Ready = 1;
                State.NoWorkCount = 0;
            }

            if (BoolOk(Worker.ExecutionContext.Ready))
            {
                return true;
            }

            if (MsQuicLib.ExecutionConfig != null && MsQuicLib.ExecutionConfig.PollingIdleTimeoutUs > CxPlatTimeDiff(State.LastWorkTime, State.TimeNow))
            {
                Worker.ExecutionContext.Ready = 1;
                return true;
            }

            Worker.IsActive = false;
            Worker.ExecutionContext.NextTimeUs = Worker.TimerWheel.NextExpirationTime;
            QuicWorkerResetQueueDelay(Worker);
            return true;
        }

        public static void QuicWorkerThread(object Context)
        {
            QUIC_WORKER Worker = Context as QUIC_WORKER;

            CXPLAT_EXECUTION_CONTEXT EC = Worker.ExecutionContext;
            CXPLAT_EXECUTION_STATE State = new CXPLAT_EXECUTION_STATE()
            {
                TimeNow = 0,
                LastWorkTime = CxPlatTimeUs(),
                WaitTime = int.MaxValue,
                NoWorkCount = 0,
                ThreadID = CxPlatCurThreadID()
            };

            while (true)
            {
                ++State.NoWorkCount;
                State.TimeNow = CxPlatTimeUs();
                if (!QuicWorkerLoop(EC.Context, State))
                {
                    break;
                }

                bool Ready = BoolOk(InterlockedFetchAndClearBoolean(ref EC.Ready));
                if (!Ready)
                {
                    if (EC.NextTimeUs == long.MaxValue)
                    {
                        CxPlatEventWaitForever(Worker.Ready);
                    }
                    else if (EC.NextTimeUs > State.TimeNow)
                    {
                        long Delay = (int)US_TO_MS(EC.NextTimeUs - State.TimeNow) + 1;
                        if (Delay > int.MaxValue)
                        {
                            Delay = int.MaxValue;
                        }
                        CxPlatEventWaitWithTimeout(Worker.Ready, (int)Delay);
                    }
                }

                if (State.NoWorkCount == 0)
                {
                    State.LastWorkTime = State.TimeNow;
                }
            }
            return;
        }

        static void QuicWorkerQueuePriorityConnection(QUIC_WORKER Worker, QUIC_CONNECTION Connection)
        {
            NetLog.Assert(Connection.Worker != null);
            bool ConnectionQueued = false;
            bool WakeWorkerThread = false;

            CxPlatDispatchLockAcquire(Worker.Lock);
            if (!Connection.WorkerProcessing && !Connection.HasPriorityWork)
            {
                if (!Connection.HasQueuedWork)
                {
                    WakeWorkerThread = QuicWorkerIsIdle(Worker);
                    Connection.Stats.Schedule.LastQueueTime = CxPlatTimeUs();
                    QuicConnAddRef(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_WORKER);
                    ConnectionQueued = true;
                }
                else
                {
                    CxPlatListEntryRemove(Connection.WorkerLink);
                }

                CxPlatListInsertTail(Worker.PriorityConnectionsTail, Connection.WorkerLink);
                Worker.PriorityConnectionsTail = Worker.PriorityConnectionsTail.Next;
                Connection.HasPriorityWork = true;
            }

            Connection.HasQueuedWork = true;
            CxPlatDispatchLockRelease(Worker.Lock);

            if (ConnectionQueued)
            {
                if (WakeWorkerThread)
                {
                    QuicWorkerThreadWake(Worker);
                }
                QuicPerfCounterIncrement(Worker.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH);
            }
        }

        static void QuicWorkerQueueOperation(QUIC_WORKER Worker, QUIC_OPERATION Operation)
        {
            CxPlatDispatchLockAcquire(Worker.Lock);

            bool WakeWorkerThread;
            if (Worker.OperationCount < MsQuicLib.Settings.MaxStatelessOperations && QuicLibraryTryAddRefBinding(Operation.STATELESS.Context.Binding))
            {
                Operation.STATELESS.Context.HasBindingRef = true;
                WakeWorkerThread = QuicWorkerIsIdle(Worker);
                CxPlatListInsertTail(Worker.Operations, Operation.Link);
                Worker.OperationCount++;
                Operation = null;
                QuicPerfCounterIncrement(Worker.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_WORK_OPER_QUEUE_DEPTH);
                QuicPerfCounterIncrement(Worker.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_WORK_OPER_QUEUED);
            }
            else
            {
                WakeWorkerThread = false;
                Worker.DroppedOperationCount++;
            }

            CxPlatDispatchLockRelease(Worker.Lock);

            if (Operation != null)
            {
                QUIC_BINDING Binding = Operation.STATELESS.Context.Binding;
                QUIC_RX_PACKET Packet = Operation.STATELESS.Context.Packet;
                QuicPacketLogDrop(Binding, Packet, "Worker operation limit reached");
                QuicOperationFree(Operation);
            }
            else if (WakeWorkerThread)
            {
                QuicWorkerThreadWake(Worker);
            }
        }

        static void QuicWorkerThreadWake(QUIC_WORKER Worker)
        {
            if (_KERNEL_MODE)
            {
                NetLog.Assert(false);
            }
            else
            {
                Worker.ExecutionContext.Ready = 1; // Run the execution context
                if (Worker.IsExternal)
                {
                    CxPlatWakeExecutionContext(Worker.ExecutionContext);
                }
                else
                {
                    CxPlatEventSet(Worker.Ready);
                }
            }
        }

        static void QuicWorkerPoolUninitialize(QUIC_WORKER_POOL WorkerPool)
        {
            for (int i = 0; i < WorkerPool.Workers.Count; i++)
            {
                QuicWorkerUninitialize(WorkerPool.Workers[i]);
            }
            WorkerPool.Workers.Clear();
        }
    }
}
