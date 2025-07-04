using System;
using System.Net;
using System.Net.Security;

namespace AKNet.Udp5MSQuic.Common
{
    internal sealed class QuicClientConnectionOptions
    {
        public SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; } = null!;
        public EndPoint RemoteEndPoint { get; set; } = null!;
        public Action ConnectFinishFunc { get; set; }
    }

    internal sealed class QuicServerConnectionOptions
    {
        public SslServerAuthenticationOptions ServerAuthenticationOptions;
        public Action ConnectFinishFunc { get; set; }
    }
}
