namespace AKNet.Socket
{
    public enum SocketShutdown
    {
        // Shutdown sockets for receive.
        Receive = 0x00,
        // Shutdown socket for send.
        Send = 0x01,
        // Shutdown socket for both send and receive.
        Both = 0x02,
    }
}
