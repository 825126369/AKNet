using Google.Protobuf;
using System;
using System.Buffers;

namespace XKNet.Common
{
	public static class Protocol3Utility
	{
		public static ReadOnlySpan<byte> SerializePackage(IMessage data, byte[] cacheSendBuffer)
		{
			int Length = data.CalculateSize();
			Span<byte> output = new Span<byte>(cacheSendBuffer, 0, Length);
			data.WriteTo(output);
			return output;
		}


		private static T getData<T>(ReadOnlySpan<byte> mReadOnlySpan) where T : class, IMessage, IMessage<T>, new()
		{
			int nOriLength = mReadOnlySpan.Length;
			try
			{
				MessageParser<T> messageParser = MessageParserPool<T>.Pop();
				T t = messageParser.ParseFrom(mReadOnlySpan);
				MessageParserPool<T>.recycle(messageParser);
				return t;
			}
			catch (Exception e)
			{
				NetLog.LogError($"{e.Message} | {e.StackTrace} | {mReadOnlySpan.Length} | {nOriLength}");
			}

			return null;
		}

		public static T getData<T>(NetPackage mPackage) where T : class, IMessage, IMessage<T>, new()
		{
			return getData<T>(mPackage.GetMsgSpin());
		}
	}
}