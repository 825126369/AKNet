using AKNet.Common;
using System;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_WORKER
    {
        public Thread Thread;

        public Action EventQ;
        public CXPLAT_SQE ShutdownSqe;
        public CXPLAT_SQE WakeSqe;
        public CXPLAT_SQE UpdatePollSqe;
        public readonly object ECLock = new object();
        public CXPLAT_LIST_ENTRY DynamicPoolList;
        public quic_platform_cxplat_slist_entry PendingECs;
        public quic_platform_cxplat_slist_entry ExecutionContexts;

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
        static void CxPlatAddExecutionContext(CXPLAT_WORKER_POOL WorkerPool, CXPLAT_EXECUTION_CONTEXT Context, ushort Index)
        {
            NetLog.Assert(WorkerPool != null);
            NetLog.Assert(Index < WorkerPool.Workers.Count);
            CXPLAT_WORKER Worker = WorkerPool.Workers[Index];

            Context.CxPlatContext = Worker;
            Monitor.Enter(Worker.ECLock);
            bool QueueEvent = Worker.PendingECs == null;
            Context.Entry.Next = Worker.PendingECs;
            Worker.PendingECs = Context.Entry;
            Monitor.Exit(Worker.ECLock);

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


    }
}
