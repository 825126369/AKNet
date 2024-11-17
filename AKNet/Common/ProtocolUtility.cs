/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:34
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;

namespace AKNet.Common
{
    public static class Protocol3Utility
	{
        public static ReadOnlySpan<byte> SerializePackage(IMessage data)
        {
            MainThreadCheck.Check();
            return SerializePackage(data, EnSureSendBufferOk(data));
        }

        public static ReadOnlySpan<byte> SerializePackage(IMessage data, byte[] cacheSendBuffer)
		{
            MainThreadCheck.Check();
            int Length = data.CalculateSize();
            Span<byte> output = new Span<byte>(cacheSendBuffer, 0, Length);
			data.WriteTo(output);
			return output;
		}
		
		public static T getData<T>(ReadOnlySpan<byte> mReadOnlySpan) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
		{
            MainThreadCheck.Check();
            T t = MessageParserPool<T>.Parser.ParseFrom(mReadOnlySpan);
            return t;
        }

		public static T getData<T>(NetPackage mPackage) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
		{
            MainThreadCheck.Check();
            return getData<T>(mPackage.GetProtoBuff());
		}

        private static byte[] cacheSendProtobufBuffer = new byte[1024];
        private static byte[] EnSureSendBufferOk(IMessage data)
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
            }
            return cacheSendProtobufBuffer;
        }
    }
}