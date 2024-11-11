using Google.Protobuf;

namespace AKNet.Common
{
    public static class AKNetConfig
    {
        internal static int nIMessagePoolMaxCapacity = 100;
        internal static int nUdpPackagePoolMaxCapacity = 100;

        public static readonly TcpConfig TcpConfig = null;
        public static readonly UdpConfig UdpConfig = null;

        public static void SetIMessagePoolMaxCapacity(int nMaxCapacity)
        {
            nIMessagePoolMaxCapacity = nMaxCapacity;
        }

        public static void SetIMessagePoolMaxCapacity<T>(int nMaxCapacity) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
        {
            IMessagePool<T>.SetMaxCapacity(nMaxCapacity);
        }

        public static void SetUdpPackagePoolMaxCapacity(int nMaxCapacity) 
        {
            nUdpPackagePoolMaxCapacity = nMaxCapacity;
        }
    }
}
