using AKNet.Common;

namespace AKNet.Udp5Quic.Common
{
    internal interface CXPLAT_POOL_Interface<T> where T : class, new()
    {
        CXPLAT_POOL_ENTRY<T> GetEntry();
    }

    internal class CXPLAT_POOL_ENTRY<T> : CXPLAT_SLIST_ENTRY
    {
        public readonly T value;
        public CXPLAT_POOL_ENTRY(T value)
        {
            this.value = value;
        }
    }

    internal class CXPLAT_POOL<T> where T : class, CXPLAT_POOL_Interface<T>, new()
    {
        public CXPLAT_POOL_ENTRY<T> ListHead = null;
        public int ListDepth;
        private readonly object Lock = new object();

        public const int CXPLAT_POOL_MAXIMUM_DEPTH = 0x4000;  // 16384
        public const int CXPLAT_POOL_DEFAULT_MAX_DEPTH = 256;     // Copied from EX_MAXIMUM_LOOKASIDE_DEPTH_BASE

        public void CxPlatPoolInitialize()
        {
            this.ListDepth = CXPLAT_POOL_DEFAULT_MAX_DEPTH;
            MSQuicFunc.InitializeSListHead(ListHead);
        }

        public void CxPlatPoolUninitialize()
        {
            //--自动GC
        }

        public CXPLAT_SLIST_ENTRY CxPlatPoolAlloc()
        {
            MSQuicFunc.CxPlatLockAcquire(Lock);
            var Entry = MSQuicFunc.CxPlatListPopEntry(ListHead);
            if (Entry != null)
            {
                NetLog.Assert(ListDepth > 0);
                ListDepth--;
            }
            MSQuicFunc.CxPlatLockRelease(Lock);
            if (Entry == null)
            {
                T t = new T();
                Entry = t.GetEntry();
            }
            return Entry;
        }

        public void CxPlatPoolFree(CXPLAT_SLIST_ENTRY Entry)
        {
            if (this.ListDepth >= CXPLAT_POOL_MAXIMUM_DEPTH)
            {
                //直接GC掉
            }
            else
            {
                MSQuicFunc.CxPlatLockAcquire(Lock);
                MSQuicFunc.CxPlatListPushEntry(ListHead, Entry);
                ListDepth++;
                MSQuicFunc.CxPlatLockRelease(Lock);
            }
        }
    }
}
