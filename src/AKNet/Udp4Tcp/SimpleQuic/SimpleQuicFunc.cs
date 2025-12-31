namespace AKNet.Udp4Tcp.Common
{
    internal enum ConnectionType
    {
        Client,
        Server,
    }

    internal enum ConnectionAsyncOperation
    {
        None = 0,
        Accept,
        Connect,
        Disconnect,
        Receive,
        Send,
    }

    internal enum ConnectionError
    {
        Success = 1,
        Error = 2,
    }

    internal enum E_LOGIC_RESULT
    {
        Success = 0,
        Error = 1,
    }

    internal static class SimpleQuicFunc
    {
        public static bool FAILED(E_LOGIC_RESULT Status)
        {
            return Status == E_LOGIC_RESULT.Error;
        }

        public static bool SUCCEEDED(E_LOGIC_RESULT Status)
        {
            return Status == E_LOGIC_RESULT.Success;
        }
    }
}
