using AKNet.Common;
using AKNet.Platform;
using System.Collections.Generic;
using System.Threading;

namespace MSQuic2
{
    using CXPLAT_CQE = OVERLAPPED_ENTRY;
    internal class CXPLAT_WORKER : CXPLAT_POOL_Interface<CXPLAT_WORKER>
    {
        public CXPLAT_POOL<CXPLAT_WORKER> mPool;
        public readonly CXPLAT_POOL_ENTRY<CXPLAT_WORKER> POOL_ENTRY = null;

        public CXPLAT_THREAD Thread;
        public CXPLAT_EVENTQ EventQ = new CXPLAT_EVENTQ();
        public CXPLAT_SQE ShutdownSqe = new CXPLAT_SQE();
        public CXPLAT_SQE WakeSqe = new CXPLAT_SQE();
        public CXPLAT_SQE UpdatePollSqe = new CXPLAT_SQE();

        public readonly object ECLock = new object();
        public readonly CXPLAT_EXECUTION_STATE State = new CXPLAT_EXECUTION_STATE();
        public readonly CXPLAT_LIST_ENTRY<CXPLAT_POOL_EX<CXPLAT_WORKER>> DynamicPoolList = new CXPLAT_LIST_ENTRY<CXPLAT_POOL_EX<CXPLAT_WORKER>>(null);
        public CXPLAT_LIST_ENTRY<CXPLAT_EXECUTION_CONTEXT> PendingECs;
        public CXPLAT_LIST_ENTRY ExecutionContexts;

#if DEBUG // Debug statistics
        public long LoopCount;
        public long EcPollCount;
        public long EcRunCount;
        public long CqeCount;
        public bool ThreadStarted;
        public bool ThreadFinished;
#endif

        public int IdealProcessor;
        public bool InitializedEventQ;
        public bool InitializedShutdownSqe;
        public bool InitializedWakeSqe;
        public bool InitializedUpdatePollSqe;
        public bool InitializedThread;
        public bool InitializedECLock;
        public bool StoppingThread;
        public bool StoppedThread;
        public bool DestroyedThread;
        public int Running;
        public CXPLAT_WORKER()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<CXPLAT_WORKER>(this);
        }

        public CXPLAT_POOL_ENTRY<CXPLAT_WORKER> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }

        public void SetPool(CXPLAT_POOL<CXPLAT_WORKER> mPool)
        {
            this.mPool = mPool;
        }

        public CXPLAT_POOL<CXPLAT_WORKER> GetPool()
        {
            return this.mPool;
        }
    }

    internal class CXPLAT_WORKER_POOL
    {
        public readonly CXPLAT_RUNDOWN_REF Rundown = new CXPLAT_RUNDOWN_REF();
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
        public const long DYNAMIC_POOL_PROCESSING_PERIOD = 1000000; // 1 second
        public const int DYNAMIC_POOL_PRUNE_COUNT = 8;

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

            for (int i = 0; i < WorkerPool.WorkerCount; ++i)
            {
                int IdealProcessor = ProcessorList != null ? ProcessorList[i] : i;
                NetLog.Assert(IdealProcessor < CxPlatProcCount());
                CXPLAT_WORKER Worker = WorkerPool.Workers[i];

                CXPLAT_THREAD_CONFIG ThreadConfig = new CXPLAT_THREAD_CONFIG()
                {
                    Flags = ThreadFlags,
                    IdealProcessor = IdealProcessor,
                    Name = $"cxplat_worker{i}",
                    Callback = CxPlatWorkerThread,
                    Context = Worker
                };
                
                if (!CxPlatWorkerPoolInitWorker(Worker, IdealProcessor, null, ThreadConfig))
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
                OSPlatformFunc.CxPlatEventQEnqueue(Worker.EventQ, Worker.ShutdownSqe);
                CxPlatThreadWait(Worker.Thread);
                CxPlatThreadDelete(Worker.Thread);
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
            if (Worker.InitializedUpdatePollSqe)
            {
               OSPlatformFunc.CxPlatSqeCleanup(Worker.EventQ, Worker.UpdatePollSqe);
            }
            if (Worker.InitializedWakeSqe)
            {
                OSPlatformFunc.CxPlatSqeCleanup(Worker.EventQ, Worker.WakeSqe);
            }
            if (Worker.InitializedShutdownSqe)
            {
                OSPlatformFunc.CxPlatSqeCleanup(Worker.EventQ, Worker.ShutdownSqe);
            }
            if (Worker.InitializedEventQ)
            {
                OSPlatformFunc.CxPlatEventQCleanup(Worker.EventQ);
            }
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

        static bool CxPlatWorkerPoolInitWorker(CXPLAT_WORKER Worker, int IdealProcessor, CXPLAT_EVENTQ EventQ, CXPLAT_THREAD_CONFIG ThreadConfig)
        {
            CxPlatListInitializeHead(Worker.DynamicPoolList);
            Worker.InitializedECLock = true;
            Worker.IdealProcessor = IdealProcessor;
            Worker.State.WaitTime = int.MaxValue;
            Worker.State.ThreadID = int.MaxValue;

            if (EventQ != null)
            {
                Worker.EventQ = EventQ;
            }
            else
            {
                if (!OSPlatformFunc.CxPlatEventQInitialize(Worker.EventQ))
                {
                    return false;
                }
                Worker.InitializedEventQ = true;
            }

            if (!OSPlatformFunc.CxPlatSqeInitialize(Worker.EventQ, ShutdownCompletion, Worker, Worker.ShutdownSqe))
            {
                return false;
            }
            Worker.InitializedShutdownSqe = true;

            if (!OSPlatformFunc.CxPlatSqeInitialize(Worker.EventQ, WakeCompletion, Worker, Worker.WakeSqe))
            {
                return false;
            }
            Worker.InitializedWakeSqe = true;

            if (!OSPlatformFunc.CxPlatSqeInitialize(Worker.EventQ, UpdatePollCompletion, Worker, Worker.UpdatePollSqe))
            {
                return false;
            }
            Worker.InitializedUpdatePollSqe = true;

            if (ThreadConfig != null)
            {
                if (QUIC_FAILED(CxPlatThreadCreate(ThreadConfig, out Worker.Thread)))
                {
                    return false;
                }
                Worker.InitializedThread = true;
            }

            return true;
        }

        static void UpdatePollCompletion(CXPLAT_CQE Cqe)
        {
            CXPLAT_WORKER Worker = OSPlatformFunc.CxPlatCqeGetSqe(Cqe).Contex as CXPLAT_WORKER;
            CxPlatUpdateExecutionContexts(Worker);
        }

        static void ShutdownCompletion(CXPLAT_CQE Cqe)
        {
            CXPLAT_WORKER Worker = OSPlatformFunc.CxPlatCqeGetSqe(Cqe).Contex as CXPLAT_WORKER;
            Worker.StoppedThread = true;
        }

        static void WakeCompletion(CXPLAT_CQE Cqe)
        {

        }

        static void CxPlatWakeExecutionContext(CXPLAT_EXECUTION_CONTEXT Context)
        {
            CXPLAT_WORKER Worker = Context.CxPlatContext;
            if (InterlockedFetchAndSetBoolean(ref Worker.Running) == 0)
            {
                OSPlatformFunc.CxPlatEventQEnqueue(Worker.EventQ, Worker.WakeSqe);
            }
        }

        static void CxPlatWorkerPoolAddExecutionContext(CXPLAT_WORKER_POOL WorkerPool, CXPLAT_EXECUTION_CONTEXT Context, int Index)
        {
            NetLog.Assert(WorkerPool != null);
            NetLog.Assert(Index < WorkerPool.WorkerCount);

            CXPLAT_WORKER Worker = WorkerPool.Workers[Index];
            Context.CxPlatContext = Worker;
            CxPlatLockAcquire(Worker.ECLock);
            bool QueueEvent = Worker.PendingECs == null;
            Context.Entry.Next = Worker.PendingECs;
            Worker.PendingECs = Context.Entry;
            CxPlatLockRelease(Worker.ECLock);

            if (QueueEvent)
            {
                OSPlatformFunc.CxPlatEventQEnqueue(Worker.EventQ, Worker.UpdatePollSqe);
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

                //将链表 Head 整体插入到 Worker->ExecutionContexts 链表的最前面。
                CXPLAT_LIST_ENTRY Tail = Head;
                while (Tail != null && Tail.Next != null)
                {
                    Tail = Tail.Next;
                }

                if (Tail != null)
                {
                    Tail.Next = Worker.ExecutionContexts;
                }
                Worker.ExecutionContexts = Head;
            }
        }

        static void CxPlatWorkerThread(object Context)
        {
            CXPLAT_WORKER Worker = (CXPLAT_WORKER)Context;
            NetLog.Assert(Worker != null);
#if DEBUG
            Worker.ThreadStarted = true;
#endif
            Worker.State.ThreadID = CxPlatCurThreadID();
            Worker.Running = 1;
            while (!Worker.StoppedThread)
            {
                ++Worker.State.NoWorkCount;
#if DEBUG
                ++Worker.LoopCount;
#endif
                Worker.State.TimeNow = CxPlatTimeUs();

                CxPlatRunExecutionContexts(Worker);
                if (Worker.State.WaitTime > 0 && InterlockedFetchAndClearBoolean(ref Worker.Running) == 1)
                {
                    Worker.State.TimeNow = CxPlatTimeUs();
                    CxPlatRunExecutionContexts(Worker);
                }

                CxPlatProcessEvents(Worker);// 这里是处理网络事件
                if (Worker.State.NoWorkCount == 0)
                {
                    Worker.State.LastWorkTime = Worker.State.TimeNow;
                }
                else if (Worker.State.NoWorkCount > CXPLAT_WORKER_IDLE_WORK_THRESHOLD_COUNT)
                {
                    //Sleep(0)	主动让出时间片（仅当有其他线程可以运行时）
                    //Sleep(1)    强制当前线程休眠至少 1ms，触发上下文切换
                    //Sleep(N)    休眠 N 毫秒，线程进入等待状态，CPU 被释放给其他线程
                    //所以，如果你只是想尝试让出 CPU 给同优先级的其他线程，Sleep(0) 是一个轻量的选择。
                    Thread.Sleep(0);
                    Worker.State.NoWorkCount = 0;
                }

                //if (Worker.State.TimeNow - Worker.State.LastPoolProcessTime > DYNAMIC_POOL_PROCESSING_PERIOD)
                //{
                //    CxPlatProcessDynamicPoolAllocators(Worker);
                //    Worker.State.LastPoolProcessTime = Worker.State.TimeNow;
                //}
            }

            Worker.Running = 0;
#if DEBUG
            Worker.ThreadFinished = true;
#endif
        }

        static void CxPlatRunExecutionContexts(CXPLAT_WORKER Worker)
        {
            if (Worker.ExecutionContexts == null)
            {
                Worker.State.WaitTime = int.MaxValue;
                return;
            }

#if DEBUG // Debug statistics
            ++Worker.EcPollCount;
#endif
            long NextTime = long.MaxValue;
            CXPLAT_LIST_ENTRY EC = Worker.ExecutionContexts;
            do
            {
                CXPLAT_EXECUTION_CONTEXT Context = CXPLAT_CONTAINING_RECORD<CXPLAT_EXECUTION_CONTEXT>(EC);
                bool Ready = BoolOk(InterlockedFetchAndClearBoolean(ref Context.Ready));
                if (Ready || Context.NextTimeUs <= Worker.State.TimeNow)
                {
#if DEBUG // Debug statistics
                    ++Worker.EcRunCount;
#endif
                    CXPLAT_LIST_ENTRY Next = Context.Entry.Next;
                    if (!Context.Callback(Context.Context, Worker.State)) //返回true 再次运行
                    {
                        EC = Next; // Remove Context from the list.
                        continue;
                    }
                    if (BoolOk(Context.Ready))
                    {
                        NextTime = 0;
                    }
                }
                if (Context.NextTimeUs < NextTime)
                {
                    NextTime = Context.NextTimeUs;
                }
                EC = Context.Entry.Next;
            } while (EC != null);

            if (NextTime == 0)
            {
                Worker.State.WaitTime = 0;
            }
            else if (NextTime != long.MaxValue)
            {
                long Diff = NextTime - Worker.State.TimeNow;
                Diff = US_TO_MS(Diff);
                if (Diff == 0)
                {
                    Worker.State.WaitTime = 1;
                }
                else if (Diff < long.MaxValue)
                {
                    Worker.State.WaitTime = Diff;
                }
                else
                {
                    Worker.State.WaitTime = int.MaxValue;
                }
            }
            else
            {
                Worker.State.WaitTime = int.MaxValue;
            }
        }

        static void CxPlatProcessEvents(CXPLAT_WORKER Worker)
        {
            NetLog.Assert(Worker.State.WaitTime <= int.MaxValue);
            int CqeCount = OSPlatformFunc.CxPlatEventQDequeue(Worker.EventQ, (int)Worker.State.WaitTime);
            InterlockedFetchAndSetBoolean(ref Worker.Running);
            if (CqeCount != 0)
            {
#if DEBUG
                Worker.CqeCount += CqeCount;
#endif
                Worker.State.NoWorkCount = 0;
                for (int i = 0; i < CqeCount; ++i)
                {
                    CXPLAT_SQE Sqe = OSPlatformFunc.CxPlatCqeGetSqe(Worker.EventQ.events.Span[i]);
                    if (Sqe != null)
                    {
                        NetLog.Assert(Sqe.Completion != null);
                        Sqe.Completion(Worker.EventQ.events.Span[i]);
                    }
                }
                OSPlatformFunc.CxPlatEventQReturn(Worker.EventQ, CqeCount);
            }
        }

        static int CxPlatWorkerPoolGetCount(CXPLAT_WORKER_POOL WorkerPool)
        {
            return WorkerPool.WorkerCount;
        }

        static int CxPlatWorkerPoolGetIdealProcessor(CXPLAT_WORKER_POOL WorkerPool, int Index)
        {
            NetLog.Assert(WorkerPool != null);
            NetLog.Assert(Index < WorkerPool.WorkerCount);
            return WorkerPool.Workers[Index].IdealProcessor;
        }

        static void CxPlatWorkerPoolRelease(CXPLAT_WORKER_POOL WorkerPool)
        {
            CxPlatRundownRelease(WorkerPool.Rundown);
        }

        static CXPLAT_EVENTQ CxPlatWorkerPoolGetEventQ(CXPLAT_WORKER_POOL WorkerPool,int Index)
        {
            NetLog.Assert(WorkerPool != null);
            NetLog.Assert(Index < WorkerPool.WorkerCount);
            return WorkerPool.Workers[Index].EventQ;
        }

        static void CxPlatProcessDynamicPoolAllocator(CXPLAT_POOL_EX<CXPLAT_WORKER> Pool)
        {
            for (int i = 0; i < DYNAMIC_POOL_PRUNE_COUNT; ++i)
            {
                if (!Pool.CxPlatPoolPrune())
                {
                    return;
                }
            }
        }

        static void CxPlatProcessDynamicPoolAllocators(CXPLAT_WORKER Worker)
        {
            CxPlatLockAcquire(Worker.ECLock);
            CXPLAT_LIST_ENTRY Entry = Worker.DynamicPoolList.Next;
            while (Entry != Worker.DynamicPoolList)
            {
                CXPLAT_POOL_EX<CXPLAT_WORKER> Pool = CXPLAT_CONTAINING_RECORD<CXPLAT_POOL_EX<CXPLAT_WORKER>>(Entry);
                Entry = Entry.Next;
                CxPlatProcessDynamicPoolAllocator(Pool);
            }
            CxPlatLockRelease(Worker.ECLock);
        }
    }
}