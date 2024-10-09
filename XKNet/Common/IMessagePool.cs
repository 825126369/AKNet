using Google.Protobuf;
using System.Collections.Concurrent;

namespace XKNetCommon
{
    public static class MessageParserPool<T> where T : class, IMessage, IMessage<T>, new()
	{
		static ConcurrentQueue<MessageParser<T>> mObjectPool = new ConcurrentQueue<MessageParser<T>>();

		public static int Count()
		{
			return mObjectPool.Count;
		}

		public static MessageParser<T> Pop()
		{
			MessageParser<T> t = null;
			if (!mObjectPool.TryDequeue(out t))
			{
				t = new MessageParser<T>(factory);
			}

			return t;
		}

        private static T factory()
        {
            return IMessagePool<T>.Pop();
        }

        public static void recycle(MessageParser<T> t)
		{
			mObjectPool.Enqueue(t);
		}

		public static void release()
		{
			
		}
	}

	public static class IMessagePool<T> where T : class, IMessage, IMessage<T>, new()
	{
		static ConcurrentQueue<T> mObjectPool = new ConcurrentQueue<T>();

		public static int Count()
		{
			return mObjectPool.Count;
		}

		public static T Pop()
		{
			T t = null;
			if (!mObjectPool.TryDequeue(out t))
			{
				t = new T();
			}

			return t;
		}

		public static void recycle(T t)
		{
			mObjectPool.Enqueue(t);
		}

		public static void release()
		{

		}
	}
}
