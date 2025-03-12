using System;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_LIST_ENTRY
    {
        public CXPLAT_LIST_ENTRY Flink;
        public CXPLAT_LIST_ENTRY Blink;
    }

    internal class CXPLAT_SLIST_ENTRY
    {
        public CXPLAT_SLIST_ENTRY Next;
    }

    internal class CXPLAT_EXECUTION_CONTEXT
    {
        CXPLAT_SLIST_ENTRY Entry;
        void* Context;
        void* CxPlatContext;
        CXPLAT_EXECUTION_FN Callback;
        ulong NextTimeUs;
        public bool Ready;
    }

    internal class CXPLAT_RUNDOWN_REF
    {
        public long RefCount;
        public Action RundownComplete;
    }

    internal class CXPLAT_WORKER_POOL
    {
        public CXPLAT_WORKER Workers;
        public readonly object WorkerLock = new object();
        public CXPLAT_RUNDOWN_REF Rundown;
        public uint WorkerCount;
    }

}
