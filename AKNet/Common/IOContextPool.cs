/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:39
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.Common
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
