using AKNet.Common;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_POOL_ENTRY
    {
        public CXPLAT_SLIST_ENTRY ListHead;
        public ulong SpecialFlag;
    }

    //internal class CXPLAT_POOL<T> where T: IPoolItemInterface
    //{
    //    public CXPLAT_SLIST_ENTRY ListHead;
    //    public uint Size;
    //    public string Tag;
    //    public uint MaxDepth;

    //    static void CxPlatPoolInitialize(bool IsPaged, uint Size, string Tag, CXPLAT_POOL Pool)
    //    {
    //        Pool.Size = Size;
    //        Pool.Tag = Tag;
    //        Pool.MaxDepth = CXPLAT_POOL_DEFAULT_MAX_DEPTH;
    //        InitializeSListHead(Pool.ListHead);
    //    }

    //    static void CxPlatPoolAlloc(CXPLAT_POOL Pool)
    //    {
    //        CXPLAT_SLIST_ENTRY Entry = InterlockedPopEntrySList(Pool.ListHead);
    //        if (Entry == null)
    //        {
    //            Entry = Pool.Allocate(Pool.Size, Pool.Tag, Pool);
    //        }
    //        return Entry;
    //    }

    //    static void CxPlatPoolFree(CXPLAT_POOL Pool, CXPLAT_SLIST_ENTRY Entry)
    //    {
    //        if (QueryDepthSList(Pool.ListHead) >= Pool.MaxDepth)
    //        {
    //            Pool.Free(Entry, Pool.Tag, Pool);
    //        }
    //        else
    //        {
    //            InterlockedPushEntrySList(Pool.ListHead, (CXPLAT_SLIST_ENTRY)Entry);
    //        }
    //    }
    //}

    internal class CXPLAT_SQE
    {
        public OVERLAPPED Overlapped;
        public CXPLAT_EVENT_COMPLETION_HANDLER Completion;
        public bool IsQueued;
    }
}
