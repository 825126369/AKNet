using System;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class SLIST_ENTRY 
    {
        public SLIST_ENTRY Next;
    }

    internal class CXPLAT_POOL_ENTRY
    {
        public SLIST_ENTRY ListHead;
        public ulong SpecialFlag;
    }

    internal class CXPLAT_POOL
    {
        //SLIST_HEADER ListHead;
        //public uint Size;
        //public uint Tag;
        //public uint MaxDepth;
        //CXPLAT_POOL_ALLOC_FN Allocate;
        //CXPLAT_POOL_FREE_FN Free;
    }

    internal class CXPLAT_SQE
    {
        OVERLAPPED Overlapped;
        CXPLAT_EVENT_COMPLETION_HANDLER Completion;
        public bool IsQueued;
    }

    internal static partial class MSQuicFunc
    {
        static void CxPlatPoolInitialize(bool IsPaged, uint Size, uint Tag, CXPLAT_POOL Pool)
        {
            //Pool.Size = Size;
            //Pool.Tag = Tag;
            //Pool.MaxDepth = CXPLAT_POOL_DEFAULT_MAX_DEPTH;
            //Pool.Allocate = CxPlatPoolGenericAlloc;
            //Pool.Free = CxPlatPoolGenericFree;
            //InitializeSListHead(&(Pool)->ListHead);
            //UNREFERENCED_PARAMETER(IsPaged);
        }

        static int CxPlatProcCurrentNumber()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }
    }
}
