using Google.Protobuf;

namespace AKNet.Common
{
    public static class AKNetConfig
    {
        public static readonly TcpConfig TcpConfig = null;
        public static readonly UdpConfig UdpConfig = null;
        public static int nIMessagePoolDefaultMaxCapacity = 0;
        public static void SetIMessagePoolMaxCapacity<T>(int nMaxCapacity) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
        {
            IMessagePool<T>.SetMaxCapacity(nMaxCapacity);
        }
    }
}
