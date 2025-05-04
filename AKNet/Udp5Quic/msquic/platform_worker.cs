//using AKNet.Common;
//using System.Collections.Generic;
//using System.Threading;

//namespace AKNet.Udp5Quic.Common
//{
//    internal class CXPLAT_WORKER
//    {
//        public Thread Thread;
//        public readonly object ECLock = new object();
//        public CXPLAT_LIST_ENTRY DynamicPoolList;
//        public CXPLAT_SLIST_ENTRY PendingECs;
//        public CXPLAT_SLIST_ENTRY ExecutionContexts;

//        public ushort IdealProcessor;
//        public bool InitializedEventQ;
//        public bool InitializedShutdownSqe;
//        public bool InitializedWakeSqe;
//        public bool InitializedUpdatePollSqe;
//        public bool InitializedThread;
//        public bool InitializedECLock;
//        public bool StoppingThread;
//        public bool StoppedThread;
//        public bool DestroyedThread;

//        public bool ThreadStarted;
//        public bool ThreadFinished;
//        public bool Running;
//    }

//    internal enum CXPLAT_THREAD_FLAGS
//    {
//        CXPLAT_THREAD_FLAG_NONE = 0x0000,
//        CXPLAT_THREAD_FLAG_SET_IDEAL_PROC = 0x0001,
//        CXPLAT_THREAD_FLAG_SET_AFFINITIZE = 0x0002,
//        CXPLAT_THREAD_FLAG_HIGH_PRIORITY = 0x0004
//    }

//    internal static partial class MSQuicFunc
//    {
//        static readonly object CxPlatWorkerLock = new object();
//        static CXPLAT_WORKER[] CxPlatWorkers;
//        static int CxPlatWorkerCount;
//        static bool CxPlatWorkersLazyStart(QUIC_EXECUTION_CONFIG Config)
//        {
//            CxPlatLockAcquire(CxPlatWorkerLock);
//            if (CxPlatWorkers != null)
//            {
//                CxPlatLockRelease(CxPlatWorkerLock);
//                return true;
//            }

//            List<ushort> ProcessorList;
//            if (Config != null && Config.ProcessorList.Count > 0)
//            {
//                CxPlatWorkerCount = Config.ProcessorList.Count;
//                ProcessorList = Config.ProcessorList;
//            }
//            else
//            {
//                CxPlatWorkerCount = CxPlatProcCount();
//                ProcessorList = null;
//            }
//            NetLog.Assert(CxPlatWorkerCount > 0 && CxPlatWorkerCount <= ushort.MaxValue);
            
//            CxPlatWorkers = new CXPLAT_WORKER[CxPlatWorkerCount];
//            if (CxPlatWorkers == null)
//            {
//                CxPlatWorkerCount = 0;
//                goto Error;
//            }

//            CXPLAT_THREAD_CONFIG ThreadConfig = new CXPLAT_THREAD_CONFIG()
//            {
//               Flags = CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_IDEAL_PROC,
//               IdealProcessor =  0,
//               Name = "cxplat_worker",
//               Callback = CxPlatWorkerThread,
//               Context = null,
//            };
                
//            for (int i = 0; i < CxPlatWorkerCount; ++i)
//            {
//                CxPlatWorkers[i].InitializedECLock = true;
//                CxPlatWorkers[i].IdealProcessor = ProcessorList != null ? ProcessorList[i] : (ushort)i;
//                NetLog.Assert(CxPlatWorkers[i].IdealProcessor < CxPlatProcCount());
//                ThreadConfig.IdealProcessor = CxPlatWorkers[i].IdealProcessor;
//                ThreadConfig.Context = CxPlatWorkers[i];
//                if (!CxPlatEventQInitialize(CxPlatWorkers[i].EventQ))
//                {
//                    goto Error;
//                }
//                CxPlatWorkers[i].InitializedEventQ = true;
//                if (QUIC_FAILED(CxPlatThreadCreate(ThreadConfig, CxPlatWorkers[i].Thread)))
//                {
//                    goto Error;
//                }
//                CxPlatWorkers[i].InitializedThread = true;
//            }

//            CxPlatRundownInitialize(CxPlatWorkerRundown);
//            CxPlatLockRelease(CxPlatWorkerLock);
//            return true;

//        Error:
//            CxPlatLockRelease(CxPlatWorkerLock);
//            return false;
//        }

//        static void CxPlatAddExecutionContext(CXPLAT_WORKER_POOL WorkerPool, CXPLAT_EXECUTION_CONTEXT Context, int Index)
//        {
//            NetLog.Assert(WorkerPool != null);
//            NetLog.Assert(Index < WorkerPool.Workers.Count);
//            CXPLAT_WORKER Worker = WorkerPool.Workers[Index];

//            Context.CxPlatContext = Worker;
//            CxPlatDispatchLockAcquire(Worker.ECLock);
//            bool QueueEvent = Worker.PendingECs == null;
//            Context.Entry.Next = Worker.PendingECs;
//            Worker.PendingECs = Context.Entry;
//            CxPlatDispatchLockRelease(Worker.ECLock);

//            if (QueueEvent)
//            {
//               CxPlatEventQEnqueue(Worker);
//            }
//        }

//        static void CxPlatWakeExecutionContext(CXPLAT_EXECUTION_CONTEXT Context)
//        {
//            CXPLAT_WORKER Worker = Context.CxPlatContext;
//            if (InterlockedFetchAndSetBoolean(ref Worker.Running))
//            {
//                CxPlatEventQEnqueue(Worker);
//            }
//        }

//        void
//CxPlatUpdateExecutionContexts(
//    _In_ CXPLAT_WORKER* Worker
//    )
//        {
//            if (QuicReadPtrNoFence(&Worker->PendingECs))
//            {
//                CxPlatLockAcquire(&Worker->ECLock);
//                CXPLAT_SLIST_ENTRY* Head = Worker->PendingECs;
//                Worker->PendingECs = NULL;
//                CxPlatLockRelease(&Worker->ECLock);

//                CXPLAT_SLIST_ENTRY** Tail = &Head;
//                while (*Tail)
//                {
//                    Tail = &(*Tail)->Next;
//                }

//                *Tail = Worker->ExecutionContexts;
//                Worker->ExecutionContexts = Head;
//            }
//        }

//        static void CxPlatWorkerThread(object Context)
//        {
//            CXPLAT_WORKER Worker = (CXPLAT_WORKER)Context;
//            NetLog.Assert(Worker != null);

//            CXPLAT_EXECUTION_STATE State = new CXPLAT_EXECUTION_STATE()
//            {
//                TimeNow = 0,
//                LastWorkTime = CxPlatTime(),
//                WaitTime = uint.MaxValue, 
//                NoWorkCount = 0, 
//                ThreadID = CxPlatCurThreadID() 
//            };

//            Worker.Running = true;
//            while (true)
//            {
//                ++State.NoWorkCount;

//                CxPlatRunExecutionContexts(Worker, State);
//                if (State.WaitTime > 0 && InterlockedFetchAndClearBoolean(Worker.Running))
//                {
//                    CxPlatRunExecutionContexts(Worker, State); // Run once more to handle race conditions
//                }

//                if (CxPlatProcessEvents(Worker, State))
//                {
//                    goto Shutdown;
//                }

//                if (State.NoWorkCount == 0)
//                {
//                    State.LastWorkTime = State.TimeNow;
//                }
//                else if (State.NoWorkCount >  CXPLAT_WORKER_IDLE_WORK_THRESHOLD_COUNT)
//                {
//                    Thread.Sleep(0);
//                    State.NoWorkCount = 0;
//                }
//            }
//        Shutdown:
//            Worker.Running = false;
//        }

//        static void CxPlatRunExecutionContexts(CXPLAT_WORKER Worker, CXPLAT_EXECUTION_STATE State)
//        {
//            if (Worker.ExecutionContexts == null)
//            {
//                State.WaitTime = long.MaxValue;
//                return;
//            }

//            State.TimeNow = CxPlatTime();

//            long NextTime = long.MaxValue;
//            CXPLAT_SLIST_ENTRY EC = Worker.ExecutionContexts;
//            do
//            {
//                CXPLAT_EXECUTION_CONTEXT Context = CXPLAT_CONTAINING_RECORD<CXPLAT_EXECUTION_CONTEXT>(EC);
//                bool Ready = InterlockedFetchAndClearBoolean(Context.Ready);
//                if (Ready || Context.NextTimeUs <= State.TimeNow)
//                {
//                    CXPLAT_SLIST_ENTRY Next = Context.Entry.Next;
//                    if (!Context.Callback(Context.Context, State))
//                    {
//                        EC = Next; // Remove Context from the list.
//                        continue;
//                    }
//                    if (Context.Ready)
//                    {
//                        NextTime = 0;
//                    }
//                }
//                if (Context.NextTimeUs < NextTime)
//                {
//                    NextTime = Context.NextTimeUs;
//                }
//                EC = Context.Entry.Next;
//            } while (EC != null);

//            if (NextTime == 0)
//            {
//                State.WaitTime = 0;
//            }
//            else if (NextTime != long.MaxValue)
//            {
//                long Diff = NextTime - State.TimeNow;
//                if (Diff == 0)
//                {
//                    State.WaitTime = 1;
//                }
//                else if (Diff < long.MaxValue)
//                {
//                    State.WaitTime = Diff;
//                }
//                else
//                {
//                    State.WaitTime = long.MaxValue - 1;
//                }
//            }
//            else
//            {
//                State.WaitTime = long.MaxValue;
//            }
//        }


//        static bool CxPlatProcessEvents(CXPLAT_WORKER Worker, CXPLAT_EXECUTION_STATE State)
//        {
//            //CXPLAT_CQE Cqes[16];
//            //int CqeCount = CxPlatEventQDequeue(Worker.EventQ, Cqes, ARRAYSIZE(Cqes), State.WaitTime);
//            //InterlockedFetchAndSetBoolean(ref Worker.Running);
//            //if (CqeCount != 0)
//            //{
//            //    State.NoWorkCount = 0;
//            //    for (int i = 0; i < CqeCount; ++i)
//            //    {
//            //        if (CxPlatCqeUserData(Cqes[i]) == null)
//            //        {
//            //            return true;
//            //        }
//            //        switch (CxPlatCqeType(Cqes[i]))
//            //        {
//            //            case CXPLAT_CQE_TYPE_WORKER_WAKE:
//            //                break; // No-op, just wake up to do polling stuff.
//            //            case CXPLAT_CQE_TYPE_WORKER_UPDATE_POLL:
//            //                CxPlatUpdateExecutionContexts(Worker);
//            //                break;
//            //            default: // Pass the rest to the datapath
//            //                CxPlatDataPathProcessCqe(&Cqes[i]);
//            //                break;
//            //        }
//            //    }
//            //    CxPlatEventQReturn(Worker.EventQ, CqeCount);
//            //}
//            return false;
//        }

//    }
//}
