using System;
using System.Net;
using System.Net.Security;

namespace AKNet.Udp5MSQuic.Common
{
    internal sealed class QuicConnectionOptions
    {
        public SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; } = null!;
        public EndPoint RemoteEndPoint { get; set; } = null!;
        public Action ConnectFinishFunc { get; set; } = null!;
        public Action CloseFinishFunc { get; set; } = null!;
        public Func<QuicStream, QUIC_STREAM_EVENT.RECEIVE_DATA, long> ReceiveStreamDataFunc { get; set; } = null!;
        public SslServerAuthenticationOptions ServerAuthenticationOptions;
    }
}
