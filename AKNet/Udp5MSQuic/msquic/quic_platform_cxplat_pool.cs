﻿using AKNet.Common;

namespace AKNet.Udp5MSQuic.Common
{
    internal interface CXPLAT_POOL_Interface<T> where T : class, new()
    {
        CXPLAT_POOL_ENTRY<T> GetEntry();
        void Reset();
    }
    
    internal class CXPLAT_POOL_ENTRY<T> : CXPLAT_LIST_ENTRY
    {
        public readonly T value;
        public CXPLAT_POOL_ENTRY(T value)
        {
            this.value = value;
        }
    }

    internal class CXPLAT_POOL_EX<T> : CXPLAT_POOL<T> where T : class, CXPLAT_POOL_Interface<T>, new()
    {
        public CXPLAT_LIST_ENTRY Link;
    }

    internal class CXPLAT_POOL<T> where T : class, CXPLAT_POOL_Interface<T>, new()
    {
        public readonly CXPLAT_POOL_ENTRY<T> ListHead = new CXPLAT_POOL_ENTRY<T>(null);
        public int ListDepth;
        private readonly object Lock = new object();
        public const int CXPLAT_POOL_MAXIMUM_DEPTH = 0x4000;  // 16384
        public const int CXPLAT_POOL_DEFAULT_MAX_DEPTH = 256;     // Copied from EX_MAXIMUM_LOOKASIDE_DEPTH_BASE

        public void CxPlatPoolInitialize()
        {
            this.ListDepth = CXPLAT_POOL_DEFAULT_MAX_DEPTH;
            MSQuicFunc.CxPlatListInitializeHead(ListHead);
        }

        public void CxPlatPoolUninitialize()
        {
            //--自动GC
        }

        public T CxPlatPoolAlloc()
        {
            MSQuicFunc.CxPlatLockAcquire(Lock);
            var Entry = MSQuicFunc.CxPlatListRemoveHead(ListHead);
            if (Entry != null)
            {
                NetLog.Assert(ListDepth > 0);
                ListDepth--;
            }
            MSQuicFunc.CxPlatLockRelease(Lock);

            T t = null;
            if (Entry != null)
            {
                return GetValue(Entry);
            }
            else
            {
                t = new T();
            }
            return t;
        }

        public void CxPlatPoolFree(T t)
        {
            if (this.ListDepth >= CXPLAT_POOL_MAXIMUM_DEPTH)
            {
                //直接GC掉
            }
            else
            {
                MSQuicFunc.CxPlatLockAcquire(Lock);
                MSQuicFunc.CxPlatListInsertTail(ListHead, t.GetEntry());
                ListDepth++;
                MSQuicFunc.CxPlatLockRelease(Lock);
            }
        }

        private T GetValue(CXPLAT_LIST_ENTRY Entry)
        {
            var mPoolEntry = Entry as CXPLAT_POOL_ENTRY<T>;
            return mPoolEntry.value;
        }
    }
}
