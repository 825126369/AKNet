using AKNet.Common;

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

    internal class CXPLAT_POOL<T> where T : class, CXPLAT_POOL_Interface<T>, new()
    {
        public readonly CXPLAT_POOL_ENTRY<T> ListHead = new CXPLAT_POOL_ENTRY<T>(null);
        public uint Tag;

        public int MaxDepth;
        public int ListDepth;
        private readonly object Lock = new object();
        public const int CXPLAT_POOL_MAXIMUM_DEPTH = 0x4000;  // 16384
        public const int CXPLAT_POOL_DEFAULT_MAX_DEPTH = 256;     // Copied from EX_MAXIMUM_LOOKASIDE_DEPTH_BASE

        public void CxPlatPoolInitialize()
        {
            this.MaxDepth = CXPLAT_POOL_DEFAULT_MAX_DEPTH;
            this.ListDepth = 0;
            MSQuicFunc.CxPlatListInitializeHead(ListHead);
        }

        public virtual T Allocate()
        {
            return new T();
        }

        public virtual void Free(T t)
        {
            //直接GC掉
        }

        public void CxPlatPoolUninitialize()
        {
            //--自动GC
        }

        public T CxPlatPoolAlloc()
        {
            T t = null;

            MSQuicFunc.CxPlatLockAcquire(Lock);
            var Entry = MSQuicFunc.CxPlatListRemoveHead(ListHead);
            if (Entry != null)
            {
                NetLog.Assert(ListDepth > 0);
                ListDepth--;
                t = GetValue(Entry);
            }
            MSQuicFunc.CxPlatLockRelease(Lock);

            if(t == null)
            {
                t = Allocate();
            }

            return t;
        }

        public void CxPlatPoolFree(T t)
        {
            if (this.ListDepth >= this.MaxDepth)
            {
                //直接GC掉
            }
            else
            {
                t.Reset();
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

    internal class CXPLAT_Buffer_POOL : CXPLAT_POOL<QUIC_BUFFER>
    {
        private int Size;
        public void CxPlatPoolInitialize(int Size)
        {
            this.Size = Size;
            this.MaxDepth = CXPLAT_POOL_DEFAULT_MAX_DEPTH;
            this.ListDepth = 0;
            MSQuicFunc.CxPlatListInitializeHead(ListHead);
        }

        public override QUIC_BUFFER Allocate()
        {
            NetLog.Assert(Size > 0, "CXPLAT_Buffer_POOL Size == 0");
            return new QUIC_BUFFER(Size);
        }
    }
}
