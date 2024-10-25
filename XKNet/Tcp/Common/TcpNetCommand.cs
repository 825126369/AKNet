
namespace XKNet.Tcp.Common
{
#if DEBUG
    public static class TcpNetCommand
#else
    internal static class TcpNetCommand
#endif
    {
        public const ushort COMMAND_HEARTBEAT = 1;
        public static bool orInnerCommand(ushort nPackageId)
        {
            return nPackageId == COMMAND_HEARTBEAT;
        }
    }
}

