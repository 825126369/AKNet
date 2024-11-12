/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/7 21:38:42
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AKNet.Common
{
    internal interface IPoolItemInterface
    {
		void Reset();
    }

	//Object 池子
	internal class ObjectPool<T> where T : class, IPoolItemInterface, new()
	{
		readonly Stack<T> mObjectPool = null;
		private int nMaxCapacity = 0;
		public ObjectPool(int initCapacity = 0, int nMaxCapacity = 10)
		{
			SetMaxCapacity(nMaxCapacity);
			mObjectPool = new Stack<T>(initCapacity);
			for (int i = 0; i < initCapacity; i++)
			{
				mObjectPool.Push(new T());
			}
			mObjectPool.TrimExcess();
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
			//防止 内存一直增加，合理的GC
			bool bRecycle = mObjectPool.Count <= 0 || mObjectPool.Count < nMaxCapacity;
			if (bRecycle)
			{
				t.Reset();
				mObjectPool.Push(t);
			}
		}
    }

	internal class SafeObjectPool<T> where T : class, IPoolItemInterface, new()
	{
		private readonly ConcurrentStack<T> mObjectPool = new ConcurrentStack<T>();
		private int nMaxCapacity = 0;
		public SafeObjectPool(int initCapacity = 0, int nMaxCapacity = 10)
		{
			SetMaxCapacity(nMaxCapacity);
			for (int i = 0; i < initCapacity; i++)
			{
				mObjectPool.Push(new T());
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
			NetLog.Assert(!mObjectPool.Contains(t), "重复回收！！！");
#endif
			//防止 内存一直增加，合理的GC
			bool bRecycle = mObjectPool.Count <= 0 || mObjectPool.Count < nMaxCapacity;
			if (bRecycle)
			{
				t.Reset();
				mObjectPool.Push(t);
			}
		}
	}
}