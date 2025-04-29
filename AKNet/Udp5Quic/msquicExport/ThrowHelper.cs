namespace AKNet.Udp5Quic.Common
{
    internal static class ThrowHelper
    {
        internal static void ThrowIfMsQuicError(int status, string? message = null)
        {
            if (StatusFailed(status))
            {
                ThrowMsQuicException(status, message);
            }
        }
    }
}
