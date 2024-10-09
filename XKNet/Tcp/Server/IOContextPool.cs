using System.Collections.Generic;
using System.Net.Sockets;
using XKNetCommon;

namespace XKNetTcpServer
{
    public class ReadWriteIOContextPool
    {
        Stack<SocketAsyncEventArgs> mObjectPool;
        BufferManager mBufferManager;

        private SocketAsyncEventArgs GenerateObject()
        {
            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            mBufferManager.SetBuffer(socketAsyncEventArgs);
            return socketAsyncEventArgs;
        }

        public ReadWriteIOContextPool(int nCount, BufferManager mBufferManager)
        {
            this.mBufferManager = mBufferManager;
            mObjectPool = new Stack<SocketAsyncEventArgs>(nCount);

            for (int i = 0; i < nCount; i++)
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

            lock (mObjectPool)
            {
                if (mObjectPool.Count > 0)
                {
                    t = mObjectPool.Pop();
                }
            }

#if DEBUG
            NetLog.Assert(t != null);
#endif
            return t;
        }

        public void recycle(SocketAsyncEventArgs t)
        {
            lock (mObjectPool)
            {
                mObjectPool.Push(t);
            }
        }
    }

    public class SimpleIOContextPool
    {
        Stack<SocketAsyncEventArgs> mObjectPool;

        private SocketAsyncEventArgs GenerateObject()
        {
            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            return socketAsyncEventArgs;
        }

        public SimpleIOContextPool(int nCount)
        {
            mObjectPool = new Stack<SocketAsyncEventArgs>(nCount);

            for (int i = 0; i < nCount; i++)
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

            lock (mObjectPool)
            {
                if (mObjectPool.Count > 0)
                {
                    t = mObjectPool.Pop();
                }else
                {
                    t = new SocketAsyncEventArgs();
                }
            }
            
            return t;
        }
    }
}
