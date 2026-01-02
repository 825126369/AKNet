using AKNet.Common;
using System.Collections.Generic;

namespace AKNet.Udp4Tcp.Common
{
    internal class SSocketAsyncEventArgsPool
    {
        public LogicWorker mLogicWorker;
        private readonly Stack<SSocketAsyncEventArgs> mObjectPool = new Stack<SSocketAsyncEventArgs>();
        private readonly int nMaxCapacity = 0;

        public SSocketAsyncEventArgsPool(LogicWorker mLogicWorker, int initCapacity = 0, int MaxCapacity = 0)
        {
            this.mLogicWorker = mLogicWorker;
            this.nMaxCapacity = MaxCapacity;
            for (int i = 0; i < initCapacity; i++)
            {
                mObjectPool.Push(Alloc());
            }
        }

        private SSocketAsyncEventArgs Alloc()
        {
            return mLogicWorker.mSocketItem.AllocSSocketAsyncEventArgs();
        }

        public int Count()
        {
            return mObjectPool.Count;
        }

        public SSocketAsyncEventArgs Pop()
        {
            SSocketAsyncEventArgs t = null;
            if (!mObjectPool.TryPop(out t))
            {
                t = Alloc();
            }
            return t;
        }

        public void recycle(SSocketAsyncEventArgs t)
        {
#if DEBUG
            NetLog.Assert(!mObjectPool.Contains(t));
#endif
            t.UserToken = null;
            t.RemoteEndPoint = null;
            //防止 内存一直增加，合理的GC
            bool bRecycle = nMaxCapacity <= 0 || mObjectPool.Count < nMaxCapacity;
            if (bRecycle)
            {
                mObjectPool.Push(t);
            }
        }
    }
}
