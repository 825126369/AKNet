namespace AKNet.Udp5Quic.Common
{
    public enum QuicError
    {
        Success,
        InternalError,
        ConnectionAborted,
        StreamAborted,
        ConnectionTimeout = 6,
        ConnectionRefused = 8,
        VersionNegotiationError,
        ConnectionIdle,
        OperationAborted = 12,
        AlpnInUse,
        TransportError,
        CallbackError,
    }
}
