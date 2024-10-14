namespace XKNet.Common
{
    public static partial class IMessageExtention
    {
        public static void Reset(this TcpProtocol.TESTChatMessage message)
        {
            message.Id = default;
            message.TalkMsg = string.Empty;
        }
    }
}
