namespace AKNet.QuicNet.Common
{
    public delegate void ClientConnectedEvent(QuicConnection connection);
    public delegate void StreamOpenedEvent(QuicStream stream);
    public delegate void StreamDataReceivedEvent(QuicStream stream, byte[] data);
    public delegate void ConnectionClosedEvent(QuicConnection connection);
}
