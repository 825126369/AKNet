using System;
using System.Net;
using System.Net.Security;

namespace AKNet.Udp2MSQuic.Common
{
    internal sealed class QuicConnectionOptions
    {
        public SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
        public Action CloseFinishFunc { get; set; }
        public SslServerAuthenticationOptions ServerAuthenticationOptions;
    }
}
