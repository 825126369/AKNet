using AKNet.Common;
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

    internal class CXPLAT_POOL_EX
    {
        public CXPLAT_POOL Base;
        public CXPLAT_LIST_ENTRY Link;
        // void* Owner;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicListEntryValidate(CXPLAT_LIST_ENTRY Entry)
        {
            NetLog.Assert(Entry.Flink.Blink == Entry && Entry.Blink.Flink == Entry);
        }

        static bool CxPlatListIsEmpty(CXPLAT_LIST_ENTRY ListHead)
        {
            return ListHead.Flink == ListHead;
        }

        static bool CxPlatListEntryRemove(CXPLAT_LIST_ENTRY Entry)
        {
            QuicListEntryValidate(Entry);
            CXPLAT_LIST_ENTRY Flink = Entry.Flink;
            CXPLAT_LIST_ENTRY Blink = Entry.Blink;
            Blink.Flink = Flink;
            Flink.Blink = Blink;
            return Flink == Blink;
        }
    }
}
