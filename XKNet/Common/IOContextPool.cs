using System.Collections.Generic;
using System.Net.Sockets;

namespace XKNet.Common
{
    internal class ReadWriteIOContextPool
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

    internal class SimpleIOContextPool
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
                }
            }
            
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
}
