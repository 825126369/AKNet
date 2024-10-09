using TcpProtocol;

namespace XKNet.Tcp.Common
{
    internal static class IMessageExtention
    {
        public static void Reset(this TESTChatMessage message)
        {
            message.Id = default;
            message.TalkMsg = string.Empty;
        }
    }
}
