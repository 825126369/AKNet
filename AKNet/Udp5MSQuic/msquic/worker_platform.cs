using AKNet.Common;
using System.Collections.Generic;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal class CXPLAT_WORKER
    {
        public Thread Thread;
        public readonly object ECLock = new object();
        public CXPLAT_EXECUTION_STATE State;
        public CXPLAT_LIST_ENTRY DynamicPoolList;
        public CXPLAT_LIST_ENTRY PendingECs;
        public CXPLAT_LIST_ENTRY ExecutionContexts;

#if DEBUG // Debug statistics
        public long LoopCount;
        public long EcPollCount;
        public long EcRunCount;
        public long CqeCount;
        public bool ThreadStarted;
        public bool ThreadFinished;
#endif

        public ushort IdealProcessor;
        public bool InitializedEventQ;
        public bool InitializedShutdownSqe;
        public bool InitializedWakeSqe;
        public bool InitializedUpdatePollSqe;
        public bool InitializedThread;
        public bool InitializedECLock;
        public bool StoppingThread;
        public bool StoppedThread;
        public bool DestroyedThread;
        public bool Running;
    }

    internal class CXPLAT_WORKER_POOL
    {
        public CXPLAT_RUNDOWN_REF Rundown;
        public int WorkerCount;
        public CXPLAT_WORKER[] Workers;

        public CXPLAT_WORKER_POOL(int WorkerCount)
        {
            this.WorkerCount = WorkerCount;
            Workers = new CXPLAT_WORKER[WorkerCount];
            for (int i = 0; i < WorkerCount; i++)
            {
                Workers[i] = new CXPLAT_WORKER();
            }
        }
    }

    internal static partial class MSQuicFunc
    {
        static readonly object CxPlatWorkerLock = new object();
        static CXPLAT_WORKER[] CxPlatWorkers;
        static int CxPlatWorkerCount;

        static CXPLAT_WORKER_POOL CxPlatWorkerPoolCreate(QUIC_GLOBAL_EXECUTION_CONFIG Config)
        {
            List<int> ProcessorList = new List<int>();
            int ProcessorCount;
            if (Config != null && Config.ProcessorList.Count > 0)
            {
                ProcessorCount = Config.ProcessorList.Count;
                ProcessorList = Config.ProcessorList;
            }
            else
            {
                ProcessorCount = CxPlatProcCount();
                ProcessorList = null;
            }
            NetLog.Assert(ProcessorCount > 0 && ProcessorCount <= ushort.MaxValue);
            
            CXPLAT_WORKER_POOL WorkerPool = new CXPLAT_WORKER_POOL(ProcessorCount);
            if (WorkerPool == null)
            {
                return null;
            }
            WorkerPool.WorkerCount = ProcessorCount;
            
            uint ThreadFlags = (uint)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_IDEAL_PROC;
            if (Config != null)
            {
                if (Config.Flags.HasFlag(QUIC_GLOBAL_EXECUTION_CONFIG_FLAGS.QUIC_GLOBAL_EXECUTION_CONFIG_FLAG_NO_IDEAL_PROC))
                {
                    ThreadFlags &= ~(uint)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_IDEAL_PROC; // Remove the flag
                }
                if (Config.Flags.HasFlag(QUIC_GLOBAL_EXECUTION_CONFIG_FLAGS.QUIC_GLOBAL_EXECUTION_CONFIG_FLAG_HIGH_PRIORITY))
                {
                    ThreadFlags |= (uint)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_HIGH_PRIORITY;
                }
                if (Config.Flags.HasFlag(QUIC_GLOBAL_EXECUTION_CONFIG_FLAGS.QUIC_GLOBAL_EXECUTION_CONFIG_FLAG_AFFINITIZE))
                {
                    ThreadFlags |= (uint)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_AFFINITIZE;
                }
            }

            CXPLAT_THREAD_CONFIG ThreadConfig = new CXPLAT_THREAD_CONFIG()
            {
                Flags = ThreadFlags,
                IdealProcessor = 0,
                Name = "cxplat_worker",
                Callback = CxPlatWorkerThread,
                Context = null
            };
            
            for (int i = 0; i < WorkerPool.WorkerCount; ++i)
            {
                int IdealProcessor = ProcessorList != null ? ProcessorList[i] : i;
                NetLog.Assert(IdealProcessor < CxPlatProcCount());

                CXPLAT_WORKER Worker = WorkerPool.Workers[i];
                if (!CxPlatWorkerPoolInitWorker(Worker, IdealProcessor, ThreadConfig))
                {
                    goto Error;
                }
            }

            CxPlatRundownInitialize(WorkerPool.Rundown);
            return WorkerPool;
        Error:
            for (int i = 0; i < WorkerPool.WorkerCount; ++i)
            {
                CXPLAT_WORKER Worker = WorkerPool.Workers[i];
                CxPlatWorkerPoolDestroyWorker(Worker);
            }

            return null;
        }

        static void CxPlatWorkerPoolDestroyWorker(CXPLAT_WORKER Worker)
        {
            if (Worker.InitializedThread)
            {
                Worker.StoppingThread = true;
                Worker.Thread.Join();
#if DEBUG
                NetLog.Assert(Worker.ThreadStarted);
                NetLog.Assert(Worker.ThreadFinished);
#endif
                Worker.DestroyedThread = true;
            }
            else
            {
                // TODO - Handle synchronized cleanup for external event queues?
            }
            //if (Worker.InitializedUpdatePollSqe)
            //{
            //    CxPlatSqeCleanup(&Worker->EventQ, &Worker->UpdatePollSqe);
            //}
            //if (Worker.InitializedWakeSqe)
            //{
            //    CxPlatSqeCleanup(&Worker->EventQ, &Worker->WakeSqe);
            //}
            //if (Worker.InitializedShutdownSqe)
            //{
            //    CxPlatSqeCleanup(&Worker->EventQ, &Worker->ShutdownSqe);
            //}
            //if (Worker.InitializedEventQ)
            //{
            //    CxPlatEventQCleanup(&Worker->EventQ);
            //}
        }

        static void CxPlatWorkerPoolDelete(CXPLAT_WORKER_POOL WorkerPool)
        {
            if (WorkerPool != null)
            {
                CxPlatRundownReleaseAndWait(WorkerPool.Rundown);
                for (int i = 0; i < WorkerPool.WorkerCount; ++i)
                {
                    CXPLAT_WORKER Worker = WorkerPool.Workers[i];
                    CxPlatWorkerPoolDestroyWorker(Worker);
                }
                CxPlatRundownUninitialize(WorkerPool.Rundown);
            }
        }

        static bool CxPlatWorkerPoolInitWorker(CXPLAT_WORKER Worker, int IdealProcessor, CXPLAT_THREAD_CONFIG ThreadConfig)
        {
            CxPlatListInitializeHead(Worker.DynamicPoolList);
            Worker.InitializedECLock = true;
            Worker.IdealProcessor = IdealProcessor;
            Worker.State.WaitTime = long.MaxValue;
            Worker.State.ThreadID = int.MaxValue;

            //if (EventQ != NULL)
            //{
            //    Worker->EventQ = *EventQ;
            //}
            //else
            //{
            //    if (!CxPlatEventQInitialize(&Worker->EventQ))
            //    {
            //        QuicTraceEvent(
            //            LibraryError,
            //            "[ lib] ERROR, %s.",
            //            "CxPlatEventQInitialize");
            //        return FALSE;
            //    }
            //    Worker->InitializedEventQ = TRUE;
            //}

            //if (!CxPlatSqeInitialize(&Worker->EventQ, ShutdownCompletion, &Worker->ShutdownSqe))
            //{
            //    QuicTraceEvent(
            //        LibraryError,
            //        "[ lib] ERROR, %s.",
            //        "CxPlatSqeInitialize(shutdown)");
            //    return FALSE;
            //}
            //Worker->InitializedShutdownSqe = TRUE;

            //if (!CxPlatSqeInitialize(&Worker->EventQ, WakeCompletion, &Worker->WakeSqe))
            //{
            //    QuicTraceEvent(
            //        LibraryError,
            //        "[ lib] ERROR, %s.",
            //        "CxPlatSqeInitialize(wake)");
            //    return FALSE;
            //}
            //Worker->InitializedWakeSqe = TRUE;

            //if (!CxPlatSqeInitialize(&Worker->EventQ, UpdatePollCompletion, &Worker->UpdatePollSqe))
            //{
            //    QuicTraceEvent(
            //        LibraryError,
            //        "[ lib] ERROR, %s.",
            //        "CxPlatSqeInitialize(updatepoll)");
            //    return FALSE;
            //}
            //Worker->InitializedUpdatePollSqe = TRUE;

            if (ThreadConfig != null)
            {
                ThreadConfig.IdealProcessor = IdealProcessor;
                ThreadConfig.Context = Worker;
                if (QUIC_FAILED(CxPlatThreadCreate(ThreadConfig, Worker.Thread)))
                {
                    return false;
                }
                Worker.InitializedThread = true;
            }

            return true;
        }


        static bool CxPlatWorkersLazyStart(QUIC_EXECUTION_CONFIG Config)
        {
            CxPlatLockAcquire(CxPlatWorkerLock);
            if (CxPlatWorkers != null)
            {
                CxPlatLockRelease(CxPlatWorkerLock);
                return true;
            }

            List<ushort> ProcessorList;
            if (Config != null && Config.ProcessorList.Count > 0)
            {
                CxPlatWorkerCount = Config.ProcessorList.Count;
                ProcessorList = Config.ProcessorList;
            }
            else
            {
                CxPlatWorkerCount = CxPlatProcCount();
                ProcessorList = null;
            }
            NetLog.Assert(CxPlatWorkerCount > 0 && CxPlatWorkerCount <= ushort.MaxValue);

            CxPlatWorkers = new CXPLAT_WORKER[CxPlatWorkerCount];
            if (CxPlatWorkers == null)
            {
                CxPlatWorkerCount = 0;
                goto Error;
            }

            CXPLAT_THREAD_CONFIG ThreadConfig = new CXPLAT_THREAD_CONFIG()
            {
                Flags = CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_IDEAL_PROC,
                IdealProcessor = 0,
                Name = "cxplat_worker",
                Callback = CxPlatWorkerThread,
                Context = null,
            };

            for (int i = 0; i < CxPlatWorkerCount; ++i)
            {
                CxPlatWorkers[i].InitializedECLock = true;
                CxPlatWorkers[i].IdealProcessor = ProcessorList != null ? ProcessorList[i] : (ushort)i;
                NetLog.Assert(CxPlatWorkers[i].IdealProcessor < CxPlatProcCount());
                ThreadConfig.IdealProcessor = CxPlatWorkers[i].IdealProcessor;
                ThreadConfig.Context = CxPlatWorkers[i];
                if (!CxPlatEventQInitialize(CxPlatWorkers[i].EventQ))
                {
                    goto Error;
                }
                CxPlatWorkers[i].InitializedEventQ = true;
                if (QUIC_FAILED(CxPlatThreadCreate(ThreadConfig, CxPlatWorkers[i].Thread)))
                {
                    goto Error;
                }
                CxPlatWorkers[i].InitializedThread = true;
            }

            CxPlatRundownInitialize(CxPlatWorkerRundown);
            CxPlatLockRelease(CxPlatWorkerLock);
            return true;

        Error:
            CxPlatLockRelease(CxPlatWorkerLock);
            return false;
        }

        static void CxPlatAddExecutionContext(CXPLAT_WORKER_POOL WorkerPool, CXPLAT_EXECUTION_CONTEXT Context, int Index)
        {
            NetLog.Assert(WorkerPool != null);
            NetLog.Assert(Index < WorkerPool.Workers.Count);
            CXPLAT_WORKER Worker = WorkerPool.Workers[Index];

            Context.CxPlatContext = Worker;
            CxPlatDispatchLockAcquire(Worker.ECLock);
            bool QueueEvent = Worker.PendingECs == null;
            Context.Entry.Next = Worker.PendingECs;
            Worker.PendingECs = Context.Entry;
            CxPlatDispatchLockRelease(Worker.ECLock);

            if (QueueEvent)
            {
                
            }
        }

        static void CxPlatWakeExecutionContext(CXPLAT_EXECUTION_CONTEXT Context)
        {
            CXPLAT_WORKER Worker = Context.CxPlatContext;
            if (InterlockedFetchAndSetBoolean(ref Worker.Running))
            {
                
            }
        }

        static void CxPlatUpdateExecutionContexts(CXPLAT_WORKER Worker)
        {
            if (Volatile.Read(ref Worker.PendingECs) != null)
            {
                CxPlatLockAcquire(Worker.ECLock);
                CXPLAT_LIST_ENTRY Head = Worker.PendingECs;
                Worker.PendingECs = null;
                CxPlatLockRelease(Worker.ECLock);
                Worker.ExecutionContexts = Head;
            }
        }

        static void CxPlatWorkerThread(object Context)
        {
            CXPLAT_WORKER Worker = (CXPLAT_WORKER)Context;
            NetLog.Assert(Worker != null);

            CXPLAT_EXECUTION_STATE State = new CXPLAT_EXECUTION_STATE()
            {
                TimeNow = 0,
                LastWorkTime = CxPlatTime(),
                WaitTime = uint.MaxValue,
                NoWorkCount = 0,
                ThreadID = CxPlatCurThreadID()
            };

            Worker.Running = true;
            while (true)
            {
                ++State.NoWorkCount;

                CxPlatRunExecutionContexts(Worker, State);
                if (State.WaitTime > 0 && InterlockedFetchAndClearBoolean(Worker.Running))
                {
                    CxPlatRunExecutionContexts(Worker, State); // Run once more to handle race conditions
                }

                if (CxPlatProcessEvents(Worker, State))
                {
                    goto Shutdown;
                }

                if (State.NoWorkCount == 0)
                {
                    State.LastWorkTime = State.TimeNow;
                }
                else if (State.NoWorkCount > CXPLAT_WORKER_IDLE_WORK_THRESHOLD_COUNT)
                {
                    Thread.Sleep(0);
                    State.NoWorkCount = 0;
                }
            }
        Shutdown:
            Worker.Running = false;
        }

        static void CxPlatRunExecutionContexts(CXPLAT_WORKER Worker, CXPLAT_EXECUTION_STATE State)
        {
            if (Worker.ExecutionContexts == null)
            {
                State.WaitTime = long.MaxValue;
                return;
            }

            State.TimeNow = CxPlatTime();

            long NextTime = long.MaxValue;
            CXPLAT_LIST_ENTRY EC = Worker.ExecutionContexts;
            do
            {
                CXPLAT_EXECUTION_CONTEXT Context = CXPLAT_CONTAINING_RECORD<CXPLAT_EXECUTION_CONTEXT>(EC.Next);
                bool Ready = InterlockedFetchAndClearBoolean(ref Context.Ready);
                if (Ready || Context.NextTimeUs <= State.TimeNow)
                {
                    CXPLAT_LIST_ENTRY Next = Context.Entry.Next;
                    if (!Context.Callback(Context.Context, State))
                    {
                        EC = Next; // Remove Context from the list.
                        continue;
                    }
                    if (Context.Ready)
                    {
                        NextTime = 0;
                    }
                }
                if (Context.NextTimeUs < NextTime)
                {
                    NextTime = Context.NextTimeUs;
                }
                EC = Context.Entry.Next;
            } while (EC != Worker.ExecutionContexts);

            if (NextTime == 0)
            {
                State.WaitTime = 0;
            }
            else if (NextTime != long.MaxValue)
            {
                long Diff = NextTime - State.TimeNow;
                if (Diff == 0)
                {
                    State.WaitTime = 1;
                }
                else if (Diff < long.MaxValue)
                {
                    State.WaitTime = Diff;
                }
                else
                {
                    State.WaitTime = long.MaxValue - 1;
                }
            }
            else
            {
                State.WaitTime = long.MaxValue;
            }
        }


        static bool CxPlatProcessEvents(CXPLAT_WORKER Worker, CXPLAT_EXECUTION_STATE State)
        {
            //CXPLAT_CQE Cqes[16];
            //int CqeCount = CxPlatEventQDequeue(Worker.EventQ, Cqes, ARRAYSIZE(Cqes), State.WaitTime);
            //InterlockedFetchAndSetBoolean(ref Worker.Running);
            //if (CqeCount != 0)
            //{
            //    State.NoWorkCount = 0;
            //    for (int i = 0; i < CqeCount; ++i)
            //    {
            //        if (CxPlatCqeUserData(Cqes[i]) == null)
            //        {
            //            return true;
            //        }
            //        switch (CxPlatCqeType(Cqes[i]))
            //        {
            //            case CXPLAT_CQE_TYPE_WORKER_WAKE:
            //                break; // No-op, just wake up to do polling stuff.
            //            case CXPLAT_CQE_TYPE_WORKER_UPDATE_POLL:
            //                CxPlatUpdateExecutionContexts(Worker);
            //                break;
            //            default: // Pass the rest to the datapath
            //                CxPlatDataPathProcessCqe(&Cqes[i]);
            //                break;
            //        }
            //    }
            //    CxPlatEventQReturn(Worker.EventQ, CqeCount);
            //}
            return false;
        }

        static int CxPlatWorkerPoolGetCount(CXPLAT_WORKER_POOL WorkerPool)
        {
            return WorkerPool.WorkerCount;
        }

        static int CxPlatWorkerPoolGetIdealProcessor(CXPLAT_WORKER_POOL WorkerPool,int Index)
        {
            NetLog.Assert(WorkerPool != null);
            NetLog.Assert(Index < WorkerPool.WorkerCount);
            return WorkerPool.Workers[Index].IdealProcessor;
        }

        static void CxPlatWorkerPoolAddExecutionContext(CXPLAT_WORKER_POOL WorkerPool, CXPLAT_EXECUTION_CONTEXT Context, int Index)
        {
            NetLog.Assert(WorkerPool !=null);
            NetLog.Assert(Index < WorkerPool.WorkerCount);
            CXPLAT_WORKER Worker = WorkerPool.Workers[Index];

            Context.CxPlatContext = Worker;
            bool QueueEvent = Worker.PendingECs == null;
            Context.Entry.Next = Worker.PendingECs;
            Worker.PendingECs = Context.Entry;
            CxPlatLockRelease(Worker.ECLock);
        }


    }
}