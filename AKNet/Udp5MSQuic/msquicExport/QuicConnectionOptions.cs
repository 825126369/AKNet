using System;
using System.Net;
using System.Net.Security;

namespace AKNet.Udp5MSQuic.Common
{
    internal sealed class QuicConnectionOptions
    {
        public SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
        public Action ConnectFinishFunc { get; set; }
        public Action CloseFinishFunc { get; set; }
        public Action<QuicStream> ReceiveStreamDataFunc { get; set; }
        public SslServerAuthenticationOptions ServerAuthenticationOptions;
    }
}
