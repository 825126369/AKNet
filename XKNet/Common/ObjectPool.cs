using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;

namespace XKNet.Common
{
    //Object 池子
    internal class ObjectPool<T> where T : class, new()
	{
		Stack<T> mObjectPool = null;

		public ObjectPool(int initCapacity = 0)
		{
			mObjectPool = new Stack<T>(initCapacity);
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
		NetLog.Assert(!mObjectPool.Contains(t));
#endif
			mObjectPool.Push(t);
		}

		public void release()
		{
			mObjectPool.Clear();
			mObjectPool = null;
		}
	}

	internal class SafeObjectPool<T> where T : class, new()
	{
		private ConcurrentStack<T> mObjectPool = new ConcurrentStack<T>();

		public SafeObjectPool(int initCapacity = 0)
		{
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
			NetLog.Assert(t.GetType() == typeof(T), $"{t.GetType()} : {typeof(T)} ");
			NetLog.Assert(!mObjectPool.Contains(t));
#endif
			mObjectPool.Push(t);
		}

		public void release()
		{
			mObjectPool.Clear();
			mObjectPool = null;
		}
	}

	internal class ListPool<T>
	{
		Stack<List<T>> mObjectPool = null;

		public ListPool(int initCapacity = 0, int nListCapacity = 0)
		{
			mObjectPool = new Stack<List<T>>(initCapacity);
			for (int i = 0; i < initCapacity; i++)
			{
				mObjectPool.Push(new List<T>(nListCapacity));
			}
		}

		public void recycle(List<T> mList)
		{
#if DEBUG
			NetLog.Assert(!mObjectPool.Contains(mList));
#endif
			mList.Clear();
			mObjectPool.Push(mList);
		}

		public List<T> Pop()
		{
			List<T> mList = null;
			if (!mObjectPool.TryPop(out mList))
			{
				mList = new List<T>();
			}

			return mList;
		}

		public void release()
		{
			mObjectPool.Clear();
			mObjectPool = null;
		}
	}

    internal class ArrayGCPool<T>
    {
        Dictionary<int, Queue<T[]>> mPoolDic = null;

        public ArrayGCPool()
        {
            mPoolDic = new Dictionary<int, Queue<T[]>>();
        }

        public void recycle(T[] array)
        {
            Array.Clear(array, 0, array.Length);

            Queue<T[]> arrayQueue = null;
            if (!mPoolDic.TryGetValue(array.Length, out arrayQueue))
            {
                arrayQueue = new Queue<T[]>();
                mPoolDic.Add(array.Length, arrayQueue);
            }

            arrayQueue.Enqueue(array);
        }

        public T[] Pop(int Length)
        {
            Queue<T[]> arrayQueue = null;
            if (!mPoolDic.TryGetValue(Length, out arrayQueue))
            {
                arrayQueue = new Queue<T[]>();
            }

            T[] array = null;
            if (arrayQueue.Count > 0)
            {
                array = arrayQueue.Dequeue();
            }
            else
            {
                array = new T[Length];
            }
            return array;
        }

        public void release()
        {
            mPoolDic = null;
        }
    }

    internal class SafeArrayGCPool<T>
    {
        ConcurrentDictionary<int, ConcurrentQueue<T[]>> mPoolDic = new ConcurrentDictionary<int, ConcurrentQueue<T[]>>();

        public SafeArrayGCPool()
        {
            mPoolDic = new ConcurrentDictionary<int, ConcurrentQueue<T[]>>();
        }

        public void recycle(T[] array)
        {
            Array.Clear(array, 0, array.Length);

            ConcurrentQueue<T[]> arrayQueue = null;
            if (!mPoolDic.TryGetValue(array.Length, out arrayQueue))
            {
                arrayQueue = new ConcurrentQueue<T[]>();
                mPoolDic.TryAdd(array.Length, arrayQueue);
            }

            arrayQueue.Enqueue(array);
        }

        public T[] Pop(int Length)
        {
            ConcurrentQueue<T[]> arrayQueue = null;
            if (!mPoolDic.TryGetValue(Length, out arrayQueue))
            {
                arrayQueue = new ConcurrentQueue<T[]>();
            }

            T[] array = null;
            if (!arrayQueue.TryDequeue(out array))
            {
                array = new T[Length];
            }

            return array;
        }

        public void release()
        {
            mPoolDic = null;
        }
    }

}