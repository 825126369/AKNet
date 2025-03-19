using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_LIST_ENTRY
    {
        public CXPLAT_LIST_ENTRY Flink;
        public CXPLAT_LIST_ENTRY Blink;
    }

    internal class CXPLAT_LIST_ENTRY_QUIC_CONNECTION : CXPLAT_LIST_ENTRY
    {
        public QUIC_CONNECTION mQUIC_CONNECTION;

        public CXPLAT_LIST_ENTRY_QUIC_CONNECTION(QUIC_CONNECTION quicConnection)
        {
            mQUIC_CONNECTION = quicConnection;
        }

        public CXPLAT_LIST_ENTRY_QUIC_CONNECTION()
        {

        }
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
        static QUIC_CONNECTION CXPLAT_CONTAINING_RECORD_QUIC_CONNECTION(CXPLAT_LIST_ENTRY Entry)
        {
            return (Entry as CXPLAT_LIST_ENTRY_QUIC_CONNECTION).mQUIC_CONNECTION;
        }

        static void QuicListEntryValidate(CXPLAT_LIST_ENTRY Entry)
        {
            NetLog.Assert(Entry.Flink.Blink == Entry && Entry.Blink.Flink == Entry);
        }

        static bool CxPlatListIsEmpty(CXPLAT_LIST_ENTRY ListHead)
        {
            return ListHead.Flink == ListHead;
        }

        static void CxPlatListInitializeHead(CXPLAT_LIST_ENTRY ListHead)
        {
            ListHead.Flink = ListHead.Blink = ListHead;
        }

        static void CxPlatListInsertHead(CXPLAT_LIST_ENTRY ListHead, CXPLAT_LIST_ENTRY Entry)
        {
            QuicListEntryValidate(ListHead);
            CXPLAT_LIST_ENTRY Flink = ListHead.Flink;
            Entry.Flink = Flink;
            Entry.Blink = ListHead;
            Flink.Blink = Entry;
            ListHead.Flink = Entry;
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

        static CXPLAT_LIST_ENTRY CxPlatListRemoveHead(CXPLAT_LIST_ENTRY ListHead)
        {
            QuicListEntryValidate(ListHead);
            CXPLAT_LIST_ENTRY Entry = ListHead.Flink;
            CXPLAT_LIST_ENTRY Flink = Entry.Flink;
            ListHead.Flink = Flink;
            Flink.Blink = ListHead;
            return Entry;
        }

    }
}
