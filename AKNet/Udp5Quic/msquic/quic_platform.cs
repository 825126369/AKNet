using System;
using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    internal enum CXPLAT_THREAD_FLAGS
    {
        CXPLAT_THREAD_FLAG_NONE = 0x0000,
        CXPLAT_THREAD_FLAG_SET_IDEAL_PROC = 0x0001,
        CXPLAT_THREAD_FLAG_SET_AFFINITIZE = 0x0002,
        CXPLAT_THREAD_FLAG_HIGH_PRIORITY = 0x0004
    }

    internal class CXPLAT_EXECUTION_CONTEXT
    {
        public CXPLAT_SLIST_ENTRY Entry;
        public QUIC_WORKER Context;
        public CXPLAT_WORKER CxPlatContext;
        public CXPLAT_EXECUTION_FN Callback;
        public long NextTimeUs;
        public bool Ready;
    }

    internal class CXPLAT_RUNDOWN_REF
    {
        public long RefCount;
        public Action RundownComplete;
    }

    internal class CXPLAT_WORKER_POOL
    {
        public readonly List<CXPLAT_WORKER> Workers = new List<CXPLAT_WORKER>();
        public readonly object WorkerLock = new object();
        public CXPLAT_RUNDOWN_REF Rundown;
    }

    internal class CXPLAT_POOL_EX
    {
        public CXPLAT_POOL Base;
        public CXPLAT_LIST_ENTRY Link;
    }

    internal class CXPLAT_EXECUTION_STATE
    {
        public long TimeNow;               // in microseconds
        public long LastWorkTime;          // in microseconds
        public long LastPoolProcessTime;   // in microseconds
        public long WaitTime;
        public int NoWorkCount;
        public int ThreadID;
    }

    internal static partial class MSQuicFunc
    {
        
    }
}
