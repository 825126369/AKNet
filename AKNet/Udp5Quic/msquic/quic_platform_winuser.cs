using AKNet.Common;
using System;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{

    internal class CXPLAT_THREAD_CONFIG
    {
        public ushort Flags;
        public ushort IdealProcessor;
        public string Name;
        public Action<QUIC_WORKER> Callback;
        public QUIC_WORKER Context;
    }

    internal class CXPLAT_POOL_ENTRY
    {
        public CXPLAT_SLIST_ENTRY ListHead;
        public ulong SpecialFlag;
    }

    internal class CXPLAT_POOL
    {
        public CXPLAT_SLIST_ENTRY ListHead;
        public uint Size;
        public string Tag;
        public uint MaxDepth;
    }

    internal class CXPLAT_SQE
    {
        public OVERLAPPED Overlapped;
        public CXPLAT_EVENT_COMPLETION_HANDLER Completion;
        public bool IsQueued;
    }

    internal static partial class MSQuicFunc
    {
        static void CxPlatRefInitialize(ref long RefCount)
        {
            RefCount = 1;
        }

        static void CxPlatRundownInitialize(CXPLAT_RUNDOWN_REF Rundown)
        {
            CxPlatRefInitialize(ref Rundown.RefCount);
            Rundown.RundownComplete = null;
            NetLog.Assert((Rundown).RundownComplete != null);
        }

        static void InitializeSListHead(CXPLAT_SLIST_ENTRY ListHead)
        {
            ListHead.Next = null;
        }

        static void CxPlatPoolInitialize(bool IsPaged, uint Size, string Tag, CXPLAT_POOL Pool)
        {
            Pool.Size = Size;
            Pool.Tag = Tag;
            Pool.MaxDepth = CXPLAT_POOL_DEFAULT_MAX_DEPTH;
            InitializeSListHead(Pool.ListHead);
        }

        static void CxPlatPoolAlloc(CXPLAT_POOL Pool)
        {
#if DEBUG
            if (CxPlatGetAllocFailDenominator())
            {
                return Pool.Allocate(Pool->Size, Pool.Tag, Pool);
            }
#endif
            void* Entry = InterlockedPopEntrySList(Pool.ListHead);
            if (Entry == NULL)
            {
                Entry = Pool.Allocate(Pool->Size, Pool.Tag, Pool);
            }
#if DEBUG
            if (Entry != null)
            {
                ((CXPLAT_POOL_ENTRY)Entry).SpecialFlag = 0;
            }
#endif
            return Entry;
        }

        static int CxPlatProcCurrentNumber()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }
    }
}
