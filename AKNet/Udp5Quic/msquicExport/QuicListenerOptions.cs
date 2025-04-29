using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp5Quic.Common
{
    public sealed class QuicListenerOptions
    {
        public IPEndPoint ListenEndPoint { get; set; } = null!;
        public List<SslApplicationProtocol> ApplicationProtocols { get; set; } = null!;
        public int ListenBacklog { get; set; }
        public Func<QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask<QuicServerConnectionOptions>> ConnectionOptionsCallback { get; set; } = null!;
        internal void Validate(string argumentName)
        {
            NetLog.Assert(argumentName, SR.net_quic_not_null_listener, ListenEndPoint);
            ValidateNotNull(argumentName, SR.net_quic_not_null_listener, ConnectionOptionsCallback);
            if (ApplicationProtocols is null || ApplicationProtocols.Count <= 0)
            {
                throw new ArgumentNullException(argumentName, SR.Format(SR.net_quic_not_null_not_empty_listener, nameof(ApplicationProtocols)));
            }
            if (ListenBacklog == 0)
            {
                ListenBacklog = QuicDefaults.DefaultListenBacklog;
            }
        }
    }
}
