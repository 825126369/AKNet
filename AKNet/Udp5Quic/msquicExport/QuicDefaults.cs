using System;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class QuicDefaults
    {
        public const int DefaultListenBacklog = 512;
        public const int DefaultClientMaxInboundBidirectionalStreams = 0;
        public const int DefaultClientMaxInboundUnidirectionalStreams = 0;
        public const int DefaultServerMaxInboundBidirectionalStreams = 100;
        public const int DefaultServerMaxInboundUnidirectionalStreams = 10;
        public const long MaxErrorCodeValue = (1L << 62) - 1;
        public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(10);
        public const int DefaultConnectionMaxData = 16 * 1024 * 1024;
        public const int DefaultStreamMaxData = 64 * 1024;
    }
}
