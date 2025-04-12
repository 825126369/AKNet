using AKNet.Common;
using System;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_WORKER
    {
        public Thread Thread;

        public CXPLAT_EVENT EventQ;
        public int ShutdownSqe;
        public int WakeSqe;
        public int UpdatePollSqe;
        public readonly object ECLock = new object();
        public CXPLAT_LIST_ENTRY DynamicPoolList;
        public CXPLAT_SLIST_ENTRY PendingECs;
        public CXPLAT_SLIST_ENTRY ExecutionContexts;

        public ulong LoopCount;
        public ulong EcPollCount;
        public ulong EcRunCount;
        public ulong CqeCount;

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

        public bool ThreadStarted;
        public bool ThreadFinished;
        public bool Running;
    }

    internal static partial class MSQuicFunc
    {
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
                CxPlatEventQEnqueue(Worker.EventQ, Worker.UpdatePollSqe);
            }
        }

        static void CxPlatWakeExecutionContext(CXPLAT_EXECUTION_CONTEXT Context)
        {
            CXPLAT_WORKER Worker = (CXPLAT_WORKER)Context.CxPlatContext;
            if (Interlocked.Read(ref Worker.Running) != 0)
            {
                CxPlatEventQEnqueue(Worker.EventQ, Worker.WakeSqe);
            }
        }

        static bool CxPlatProcessEvents(CXPLAT_WORKER Worker, CXPLAT_EXECUTION_STATE State)
        {
            CXPLAT_CQE Cqes[16];
            uint32_t CqeCount = CxPlatEventQDequeue(&Worker->EventQ, Cqes, ARRAYSIZE(Cqes), State->WaitTime);
            InterlockedFetchAndSetBoolean(&Worker->Running);
            if (CqeCount != 0)
            {
#if DEBUG // Debug statistics
                Worker->CqeCount += CqeCount;
#endif
                State->NoWorkCount = 0;
                for (uint32_t i = 0; i < CqeCount; ++i)
                {
                    if (CxPlatCqeUserData(&Cqes[i]) == NULL)
                    {
#if DEBUG
                        CXPLAT_DBG_ASSERT(Worker->StoppingThread);
#endif
                        return TRUE; // NULL user data means shutdown.
                    }
                    switch (CxPlatCqeType(&Cqes[i]))
                    {
                        case CXPLAT_CQE_TYPE_WORKER_WAKE:
                            break; // No-op, just wake up to do polling stuff.
                        case CXPLAT_CQE_TYPE_WORKER_UPDATE_POLL:
                            CxPlatUpdateExecutionContexts(Worker);
                            break;
                        default: // Pass the rest to the datapath
                            CxPlatDataPathProcessCqe(&Cqes[i]);
                            break;
                    }
                }
                CxPlatEventQReturn(&Worker->EventQ, CqeCount);
            }
            return FALSE;
        }


    }
}
