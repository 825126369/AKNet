/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
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
            bool bRecycle = nMaxCapacity <= 0 || mObjectPool.Count < nMaxCapacity;
            if (bRecycle)
            {
                mObjectPool.Push(t);
            }
            else
            {
                t.Dispose();
            }
        }
    }
}
