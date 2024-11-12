using Google.Protobuf;

namespace AKNet.Common
{
    public class IMessagePoolConfig
    {
        public int nIMessagePoolMaxCapacity = 100;
        public static void SetIMessagePoolMaxCapacity<T>(int nMaxCapacity) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
        {
            IMessagePool<T>.SetMaxCapacity(nMaxCapacity);
        }
    }
}
