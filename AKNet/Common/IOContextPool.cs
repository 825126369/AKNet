/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:34
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace AKNet.Common
{
    internal class SimpleIOContextPool
    {
        readonly ConcurrentStack<SocketAsyncEventArgs> mObjectPool = new ConcurrentStack<SocketAsyncEventArgs>();
        private int nMaxCapacity = 0;

        private SocketAsyncEventArgs GenerateObject()
        {
            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            return socketAsyncEventArgs;
        }

        public SimpleIOContextPool(int initCapacity = 0, int MaxCapacity = 0)
        {
            SetMaxCapacity(MaxCapacity);
            for (int i = 0; i < initCapacity; i++)
            {
                SocketAsyncEventArgs socketAsyncEventArgs = GenerateObject();
                mObjectPool.Push(socketAsyncEventArgs);
            }
        }

        public int Count()
        {
            return mObjectPool.Count;
        }

        public SocketAsyncEventArgs Pop()
        {
            SocketAsyncEventArgs t = null;
            if (!mObjectPool.TryPop(out t))
            {
                t = GenerateObject();
            }
            return t;
        }

        public void SetMaxCapacity(int nCapacity)
        {
            this.nMaxCapacity = nCapacity;
        }

        public void recycle(SocketAsyncEventArgs t)
        {
            //防止 内存一直增加，合理的GC
            bool bRecycle = mObjectPool.Count <= 0 || mObjectPool.Count < nMaxCapacity;
            if (bRecycle)
            {
                mObjectPool.Push(t);
            }
        }

    }
}
