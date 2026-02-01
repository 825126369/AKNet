/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:45
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
namespace AKNet.Common
{
    internal interface IPoolItemInterface
    {
		void Reset();
    }

	//Object 池子
	internal class ObjectPool<T> where T : class, IPoolItemInterface, new()
	{
        private readonly Stack<T> mObjectPool = new Stack<T>();
		private readonly int nMaxCapacity = 0;
		public ObjectPool(int initCapacity = 0, int MaxCapacity = 0)
		{
            this.nMaxCapacity = MaxCapacity;
			for (int i = 0; i < initCapacity; i++)
			{
				mObjectPool.Push(new T());
			}
		}

		public int Count()
		{
			return mObjectPool.Count;
		}

		public T Pop()
		{
			T t = null;
			if (!mObjectPool.TryPop(out t))
			{
                t = new T();
            }
			return t;
		}

		public void recycle(T t)
		{
#if DEBUG
            NetLog.Assert(t.GetType().Name == typeof(T).Name, $"{t.GetType()} : {typeof(T)} ");
            NetLog.Assert(!mObjectPool.Contains(t));
#endif
            t.Reset();
            //防止 内存一直增加，合理的GC
            bool bRecycle = nMaxCapacity <= 0 || mObjectPool.Count < nMaxCapacity;
            if (bRecycle)
			{
				mObjectPool.Push(t);
			}
		}
    }

	internal class SafeObjectPool<T> where T : class, IPoolItemInterface, new()
	{
		private readonly Stack<T> mObjectPool = null;
		private readonly int nMaxCapacity = 0;
		public SafeObjectPool(int initCapacity = 16, int MaxCapacity = 0)
		{
            this.nMaxCapacity = MaxCapacity;
            mObjectPool = new Stack<T>(initCapacity);
            for (int i = 0; i < initCapacity; i++)
			{
				recycle(new T());
			}
		}

		public int Count()
		{
			return mObjectPool.Count;
		}

		public T Pop()
		{
			T t = null;
			lock (mObjectPool)
			{
				mObjectPool.TryPop(out t);
			}

			if (t == null)
			{
				t = new T();
			}
			return t;
		}

		public void recycle(T t)
		{
#if DEBUG
            //NetLog.Assert(!mObjectPool.Contains(t));
            NetLog.Assert(t.GetType().Name == typeof(T).Name, $"{t.GetType()} : {typeof(T)} ");
#endif
			t.Reset();
			//防止 内存一直增加，合理的GC
			bool bRecycle = nMaxCapacity <= 0 || mObjectPool.Count < nMaxCapacity;
			if (bRecycle)
			{
				lock (mObjectPool)
				{
					mObjectPool.Push(t);
				}
			}
		}

	}

    internal class SafeObjectPool2<T> where T : class, IPoolItemInterface, new()
    {
        private readonly ConcurrentBag<T> mObjectPool = null;
        private int nMaxCapacity = 0;
        public SafeObjectPool2(int initCapacity = 16, int MaxCapacity = 0)
        {
            SetMaxCapacity(MaxCapacity);
            mObjectPool = new ConcurrentBag<T>();
            for (int i = 0; i < initCapacity; i++)
            {
                recycle(new T());
            }
        }

        public void SetMaxCapacity(int nCapacity)
        {
            this.nMaxCapacity = nCapacity;
        }

        public int Count()
        {
            return mObjectPool.Count;
        }

        public T Pop()
        {
            T t = null;
            if (!mObjectPool.TryTake(out t))
            {
                t = new T();
            }
            return t;
        }

#if DEBUG
        private bool Contains(T t)
		{
			foreach(var v in mObjectPool)
			{
				if(v == t)
				{
					return true;
				}
			}

			return false;
		}
#endif

		public void recycle(T t)
		{
#if DEBUG
			//NetLog.Assert(!Contains(t));
			NetLog.Assert(t.GetType().Name == typeof(T).Name, $"{t.GetType()} : {typeof(T)} ");
#endif
			t.Reset();
			//防止 内存一直增加，合理的GC
			bool bRecycle = nMaxCapacity <= 0 || mObjectPool.Count < nMaxCapacity;
			if (bRecycle)
			{
				mObjectPool.Add(t);
			}
        }

    }
}