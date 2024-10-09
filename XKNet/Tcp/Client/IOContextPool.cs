using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace XKNet.Tcp.Client
{
    internal class ReadWriteIOContextPool
    {
        Stack<SocketAsyncEventArgs> mObjectPool;
        private EventHandler<SocketAsyncEventArgs> eventHandler;
        BufferManager mBufferManager;

        private SocketAsyncEventArgs GenerateObject()
        {
            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += eventHandler;
            mBufferManager.SetBuffer(socketAsyncEventArgs);
            return socketAsyncEventArgs;
        }

        public ReadWriteIOContextPool(int nCount, BufferManager mBufferManager, EventHandler<SocketAsyncEventArgs> eventHandler)
        {
            this.eventHandler = eventHandler;
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
        private EventHandler<SocketAsyncEventArgs> eventHandler;

        private SocketAsyncEventArgs GenerateObject()
        {
            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += eventHandler;
            return socketAsyncEventArgs;
        }

        public SimpleIOContextPool(int nCount, EventHandler<SocketAsyncEventArgs> eventHandler)
        {
            this.eventHandler = eventHandler;
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
