/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;

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
		
		public static T getData<T>(ReadOnlySpan<byte> mReadOnlySpan) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
		{
            MessageParser<T> messageParser = MessageParserPool<T>.Pop();
            T t = messageParser.ParseFrom(mReadOnlySpan);
            MessageParserPool<T>.recycle(messageParser);
            return t;
        }

		public static T getData<T>(NetPackage mPackage) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
		{
			return getData<T>(mPackage.GetBuffBody());
		}
	}
}