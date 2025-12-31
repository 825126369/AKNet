/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:17
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using Google.Protobuf;
using System;

namespace AKNet.Extentions.Protobuf
{
    public static class Proto3Tool
    {
        public static ReadOnlySpan<byte> SerializePackage(IMessage data)
        {
            return SerializePackage(data, EnSureSendBufferOk(data));
        }

        public static ReadOnlySpan<byte> SerializePackage(IMessage data, byte[] cacheSendBuffer)
		{
            int Length = data.CalculateSize();
            Span<byte> output = new Span<byte>(cacheSendBuffer, 0, Length);
			data.WriteTo(output);
			return output;
		}
		
		public static T GetData<T>(ReadOnlySpan<byte> mReadOnlySpan) where T : class, IMessage, IMessage<T>, new()
		{
            T t = MessageParserEx<T>.Parser.ParseFrom(mReadOnlySpan);
            return t;
        }

        public static T GetData<T>(NetPackage mPackage) where T : class, IMessage, IMessage<T>, new()
        {
            T t = MessageParserEx<T>.Parser.ParseFrom(mPackage.GetData());
            return t;
        }

        public static T GetPoolData<T>(ReadOnlySpan<byte> mReadOnlySpan) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
        {
            T t = MessageParserPool<T>.Parser.ParseFrom(mReadOnlySpan);
            return t;
        }

        public static T GetPoolData<T>(NetPackage mPackage) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
		{
            T t = MessageParserPool<T>.Parser.ParseFrom(mPackage.GetData());
            return t;
        }

        private static byte[] cacheSendProtobufBuffer = new byte[1024];
        private static byte[] EnSureSendBufferOk(IMessage data)
        {
            int Length = data.CalculateSize();
            BufferTool.EnSureBufferOk_Power2(ref cacheSendProtobufBuffer, Length);
            return cacheSendProtobufBuffer;
        }
    }
}