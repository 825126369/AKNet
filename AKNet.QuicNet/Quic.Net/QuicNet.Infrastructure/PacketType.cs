namespace AKNet.QuicNet.Common
{
    public enum PacketType : UInt16
    {
        Initial = 0x0,
        ZeroRTTProtected = 0x1,
        Handshake = 0x2,
        RetryPacket = 0x3
    }
}
