using Google.Protobuf;
using System;
using System.Buffers;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    public class Protocol3Utility
	{
		public static Span<byte> SerializePackage(IMessage data, byte[] cacheSendBuffer)
		{
			int Length = data.CalculateSize();
			Span<byte> output = new Span<byte>(cacheSendBuffer, 0, Length);
			data.WriteTo(output);
			return output;
		}

		private static T getData<T>(byte[] stream, int index, int Length) where T : class, IMessage, IMessage<T>, new()
		{
			ReadOnlySequence<byte> readOnlySequence = new ReadOnlySequence<byte>(stream, index, Length);
			MessageParser<T> messageParser = MessageParserPool<T>.Pop();
			T t = messageParser.ParseFrom(readOnlySequence);
			MessageParserPool<T>.recycle(messageParser);
			return t;
		}

		public static T getData<T>(NetPackage mPackage) where T : class, IMessage, IMessage<T>, new()
		{
			return getData<T>(mPackage.buffer, Config.nUdpPackageFixedHeadSize, mPackage.Length - Config.nUdpPackageFixedHeadSize);
		}
	}
}