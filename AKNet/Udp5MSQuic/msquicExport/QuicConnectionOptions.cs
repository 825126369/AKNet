using System;
using System.Net;
using System.Net.Security;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal sealed class QuicReceiveWindowSizes
    {
        public int Connection { get; set; } = QuicDefaults.DefaultConnectionMaxData;
        public int LocallyInitiatedBidirectionalStream { get; set; } = QuicDefaults.DefaultStreamMaxData;
        public int RemotelyInitiatedBidirectionalStream { get; set; } = QuicDefaults.DefaultStreamMaxData;
        public int UnidirectionalStream { get; set; } = QuicDefaults.DefaultStreamMaxData;
    }

    internal readonly struct QuicStreamCapacityChangedArgs
    {
        public int BidirectionalIncrement { get; }
        public int UnidirectionalIncrement { get; }
    }
    
    internal class QuicConnectionOptions
    {
        public int MaxInboundBidirectionalStreams { get; set; }
        public int MaxInboundUnidirectionalStreams { get; set; }
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.Zero;
        public ulong DefaultStreamErrorCode { get; set; } = 0;
        public ulong DefaultCloseErrorCode { get; set; } = 0;

        internal QuicReceiveWindowSizes? _initialReceiveWindowSizes;
        public QuicReceiveWindowSizes InitialReceiveWindowSizes
        {
            get => _initialReceiveWindowSizes ??= new QuicReceiveWindowSizes();
            set => _initialReceiveWindowSizes = value;
        }
        public TimeSpan KeepAliveInterval { get; set; } = Timeout.InfiniteTimeSpan;
        public TimeSpan HandshakeTimeout { get; set; } = QuicDefaults.HandshakeTimeout;
        public Action<QuicConnection, QuicStreamCapacityChangedArgs>? StreamCapacityCallback { get; set; }
    }

    internal sealed class QuicClientConnectionOptions : QuicConnectionOptions
    {
        public QuicClientConnectionOptions()
        {
            MaxInboundBidirectionalStreams = QuicDefaults.DefaultClientMaxInboundBidirectionalStreams;
            MaxInboundUnidirectionalStreams = QuicDefaults.DefaultClientMaxInboundUnidirectionalStreams;
        }

        public SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; } = null!;
        
        public EndPoint RemoteEndPoint { get; set; } = null!;
        public IPEndPoint? LocalEndPoint { get; set; }
    }

    internal sealed class QuicServerConnectionOptions : QuicConnectionOptions
    {
        public QuicServerConnectionOptions()
        {
            MaxInboundBidirectionalStreams = QuicDefaults.DefaultServerMaxInboundBidirectionalStreams;
            MaxInboundUnidirectionalStreams = QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams;
        }
        
        public SslServerAuthenticationOptions ServerAuthenticationOptions;
    }
}
