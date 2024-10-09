using Google.Protobuf;
using System;
using System.Buffers;
using XKNet.Common;

namespace XKNet.Tcp.Common
{
    public class Protocol3Utility
	{
		private static byte[] cacheSendProtobufBuffer = new byte[1024];
		
		public static Span<byte> SerializePackage(IMessage data)
		{
			int Length = data.CalculateSize();
			
			if (cacheSendProtobufBuffer.Length < Length)
			{
				int newSize = cacheSendProtobufBuffer.Length * 2;
				while (newSize < Length)
				{
					newSize *= 2;
				}

                cacheSendProtobufBuffer = new byte[newSize];
				NetLog.LogWarning("cacheSendBuffer Size: " + cacheSendProtobufBuffer.Length);
			}

			Span<byte> output = new Span<byte>(cacheSendProtobufBuffer, 0, Length);
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
			return getData<T>(mPackage.mBufferSegment);
		}
	}
}
