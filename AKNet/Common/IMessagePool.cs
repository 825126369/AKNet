/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:39
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AKNet.Common
{
    internal static class MessageParserPool<T> where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
	{
		static ConcurrentStack<MessageParser<T>> mObjectPool = new ConcurrentStack<MessageParser<T>>();

		public static int Count()
		{
			return mObjectPool.Count;
		}

		public static MessageParser<T> Pop()
		{
			MessageParser<T> t = null;
			if (!mObjectPool.TryPop(out t))
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
#if DEBUG
            NetLog.Assert(!mObjectPool.Contains(t));
#endif
            mObjectPool.Push(t);
		}

		public static void release()
		{
			
		}
	}

    public interface IProtobufResetInterface
    {
        void Reset();
    }

    public static class IMessagePool<T> where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
	{
		static ConcurrentStack<T> mObjectPool = new ConcurrentStack<T>();

		public static int Count()
		{
			return mObjectPool.Count;
		}

		public static T Pop()
		{
			T t = null;
			if (!mObjectPool.TryPop(out t))
			{
				t = new T();
			}
			return t;
		}

#if DEBUG
		//Protobuf内部实现了相等器,所以不能直接通过 == 来比较是否包含 
		private static bool orContain(T t)
		{
			foreach (var v in mObjectPool)
			{
				if (Object.ReferenceEquals(v, t))
				{
					return true;
				}
			}
			return false;
		}
#endif

		public static void recycle(T t)
		{
#if DEBUG
			bool bContain = orContain(t);
			NetLog.Assert(!bContain);
			if (!bContain)
#endif
			{
				t.Reset();
				mObjectPool.Push(t);
			}
		}

		public static void release()
		{

		}
	}
}
