/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/7 21:38:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using System.Collections.Generic;

namespace AKNet.Common
{
    internal static class MessageParserPool<T> where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
	{
		public static readonly MessageParser<T> Parser = new MessageParser<T>(factory);
        private static T factory()
        {
            return IMessagePool<T>.Pop();
        }
	}

    public interface IProtobufResetInterface
    {
        void Reset();
    }

	public static class IMessagePool<T> where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
	{
		readonly static Stack<T> mObjectPool = new Stack<T>();
		private static int nMaxCapacity = 0;

		static IMessagePool()
		{
			SetMaxCapacity(AKNetConfig.nIMessagePoolDefaultMaxCapacity);
		}

        public static void SetMaxCapacity(int nCapacity)
        {
            nMaxCapacity = nCapacity;
        }

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
            NetLog.Assert(!orContain(t));
#endif

            t.Reset();
            //防止 内存一直增加，合理的GC
            bool bRecycle = mObjectPool.Count <= 0 || mObjectPool.Count < nMaxCapacity;
			if (bRecycle)
			{
				mObjectPool.Push(t);
			}
		}
	}
}
