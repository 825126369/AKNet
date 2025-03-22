using System;

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
        quic_platform_cxplat_slist_entry Entry;
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

    internal class CXPLAT_POOL_EX
    {
        public CXPLAT_POOL Base;
        public CXPLAT_LIST_ENTRY Link;
    }

    internal static partial class MSQuicFunc
    {
        
    }
}
