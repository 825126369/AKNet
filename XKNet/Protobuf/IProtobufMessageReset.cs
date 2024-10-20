using XKNet.Common;

namespace TestProtocol
{
    public sealed partial class TESTChatMessage : IProtobufResetInterface
    {
        public void Reset()
        {
            Id = 0;
            TalkMsg = string.Empty;
        }
    }
}

namespace TcpProtocol
{
    internal sealed partial class HeartBeat : IProtobufResetInterface
    {
        public void Reset()
        {
            
        }
    }
}

namespace UdpPointtopointProtocols
{
    internal sealed partial class PackageCheckResult : IProtobufResetInterface
    {
        public void Reset()
        {
            NSureOrderId = 0;
        }
    }
}

