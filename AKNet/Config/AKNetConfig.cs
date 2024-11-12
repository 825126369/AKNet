using Google.Protobuf;

namespace AKNet.Common
{
    public static class AKNetConfig
    {
        public static readonly TcpConfig TcpConfig = null;
        public static readonly UdpConfig UdpConfig = null;

        internal static int nIMessagePoolMaxCapacity = 100;
        public static void SetIMessagePoolMaxCapacity(int nMaxCapacity)
        {
            nIMessagePoolMaxCapacity = nMaxCapacity;
        }

        public static void SetIMessagePoolMaxCapacity<T>(int nMaxCapacity) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
        {
            IMessagePool<T>.SetMaxCapacity(nMaxCapacity);
        }
    }
}
