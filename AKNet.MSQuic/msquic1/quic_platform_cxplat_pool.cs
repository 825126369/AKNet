/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:32
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace MSQuic1
{
    internal interface CXPLAT_POOL_Interface<T> where T : class, CXPLAT_POOL_Interface<T>, new()
    {
        CXPLAT_POOL_ENTRY<T> GetEntry();
        void Reset();
        void SetPool(CXPLAT_POOL<T> mPool);
        CXPLAT_POOL<T> GetPool();
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
        public readonly CXPLAT_LIST_ENTRY<CXPLAT_POOL_EX<T>> Link;
        public object Owner;

        public CXPLAT_POOL_EX()
        {
            Link = new CXPLAT_LIST_ENTRY<CXPLAT_POOL_EX<T>>(this);
        }
    }

    internal class CXPLAT_POOL<T> where T : class, CXPLAT_POOL_Interface<T>, new()
    {
        public readonly CXPLAT_POOL_ENTRY<T> ListHead = new CXPLAT_POOL_ENTRY<T>(null);
        public uint Tag;

        public int MaxDepth;
        public int ListDepth;
        private readonly object Lock = new object();
        public const int CXPLAT_POOL_MAXIMUM_DEPTH = 0x4000;
        public const int CXPLAT_POOL_DEFAULT_MAX_DEPTH = 256;

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

        public void Free(T t)
        {
            //直接GC掉
        }

        public void CxPlatPoolUninitialize()
        {
            MSQuicFunc.CxPlatListInitializeHead(ListHead); //把所有对象都扔掉
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

            if (t == null)
            {
                t = Allocate();
            }
            t.SetPool(this);
            return t;
        }

        public void CxPlatPoolFree(T t)
        {
            if (this.ListDepth >= this.MaxDepth)
            {
                //直接GC掉
                Free(t);
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

        public bool CxPlatPoolPrune()
        {
            T t = null;
            MSQuicFunc.CxPlatLockAcquire(Lock);
            var Entry = MSQuicFunc.CxPlatListRemoveHead(ListHead);
            if (Entry != null)
            {
                t = GetValue(Entry);
            }
            MSQuicFunc.CxPlatLockRelease(Lock);
            if (t == null)
            {
                return false;
            }
            return true;
        }

        private T GetValue(CXPLAT_LIST_ENTRY Entry)
        {
            var mPoolEntry = Entry as CXPLAT_POOL_ENTRY<T>;
            return mPoolEntry.value;
        }
    }

    internal class CXPLAT_Buffer_POOL : CXPLAT_POOL<QUIC_Pool_BUFFER>
    {
        private int Size;
        public void CxPlatPoolInitialize(int Size)
        {
            this.Size = Size;
            this.MaxDepth = CXPLAT_POOL_DEFAULT_MAX_DEPTH;
            this.ListDepth = 0;
            MSQuicFunc.CxPlatListInitializeHead(ListHead);
        }

        public override QUIC_Pool_BUFFER Allocate()
        {
            NetLog.Assert(Size > 0, "CXPLAT_Buffer_POOL Size == 0");
            return new QUIC_Pool_BUFFER(Size);
        }
    }

    internal class DefaultReceiveBufferPool : CXPLAT_POOL<QUIC_RECV_CHUNK>
    {
        private int Size;
        public void CxPlatPoolInitialize(int Size)
        {
            this.Size = Size;
            this.MaxDepth = CXPLAT_POOL_DEFAULT_MAX_DEPTH;
            this.ListDepth = 0;
            MSQuicFunc.CxPlatListInitializeHead(ListHead);
        }

        public override QUIC_RECV_CHUNK Allocate()
        {
            NetLog.Assert(Size > 0, "CXPLAT_Buffer_POOL Size == 0");
            return new QUIC_RECV_CHUNK(Size);
        }
    }

    internal static partial class MSQuicFunc
    {
        
    }
}
