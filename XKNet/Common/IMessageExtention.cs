using TestProtocol;

namespace XKNet.Common
{
    public static partial class IMessageExtention
    {
        internal static void Reset(this TESTChatMessage message)
        {
            message.Id = default;
            message.TalkMsg = string.Empty;
        }
    }
}
