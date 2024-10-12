using Google.Protobuf;
using System;
using System.Buffers;

namespace XKNet.Common
{
	// Protobuf 不是线程安全的，并发情况下，protobuf 序列化，反序列化都会报错！！！
    public static class Protocol3Utility
	{
		public static Span<byte> SerializePackage(IMessage data, byte[] cacheSendBuffer)
		{
			int Length = data.CalculateSize();
			Span<byte> output = new Span<byte>(cacheSendBuffer, 0, Length);
			data.WriteTo(output);
			return output;
		}

		private static T getData<T>(ArraySegment<byte> mBufferSegment) where T : class, IMessage, IMessage<T>, new()
		{
			ReadOnlySequence<byte> readOnlySequence = new ReadOnlySequence<byte>(mBufferSegment);
			MessageParser<T> messageParser = MessageParserPool<T>.Pop();
			T t = messageParser.ParseFrom(readOnlySequence);
			MessageParserPool<T>.recycle(messageParser);
			return t;
		}

		public static T getData<T>(NetPackage mPackage) where T : class, IMessage, IMessage<T>, new()
		{
			return getData<T>(mPackage.GetArraySegment());
		}
	}
}