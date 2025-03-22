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
}
