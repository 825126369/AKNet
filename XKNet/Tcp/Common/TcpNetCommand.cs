
namespace XKNet.Tcp.Common
{
#if DEBUG
    public static class TcpNetCommand
#else
    internal static class TcpNetCommand
#endif
    {
        public const ushort COMMAND_HEARTBEAT = 1;
        public const ushort COMMAND_CONNECTFULL = 2;
        public const ushort COMMAND_TESTCHAT = 3;
    }
}

