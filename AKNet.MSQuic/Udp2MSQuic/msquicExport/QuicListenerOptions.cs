using System;
using System.Net;

namespace AKNet.Udp2MSQuic.Common
{
    internal sealed class QuicListenerOptions
    {
        public IPEndPoint ListenEndPoint { get; set; } = null!;
        public int ListenBacklog { get; set; }
        public Func<QuicConnectionOptions> GetConnectionOptionFunc { get; set; } = null!;
        public Action<QuicConnection> AcceptConnectionFunc { get; set; } = null!;
    }
}
