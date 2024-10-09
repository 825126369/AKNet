using TcpProtocol;

namespace XKNetTcpCommon
{
    public static class IMessageExtention
    {
        public static void Reset(this TESTChatMessage message)
        {
            message.Id = default;
            message.TalkMsg = string.Empty;
        }
    }
}
