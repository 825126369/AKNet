using System;
using System.Net;
using System.Net.Security;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    public sealed class QuicReceiveWindowSizes
    {
        public int Connection { get; set; } = QuicDefaults.DefaultConnectionMaxData;
        public int LocallyInitiatedBidirectionalStream { get; set; } = QuicDefaults.DefaultStreamMaxData;
        public int RemotelyInitiatedBidirectionalStream { get; set; } = QuicDefaults.DefaultStreamMaxData;
        public int UnidirectionalStream { get; set; } = QuicDefaults.DefaultStreamMaxData;
    }

    /// <summary>
    /// Arguments for <see cref="QuicConnectionOptions.StreamCapacityCallback"/>.
    /// </summary>
    public readonly struct QuicStreamCapacityChangedArgs
    {
        public int BidirectionalIncrement { get; init; }
        public int UnidirectionalIncrement { get; init; }
    }

    /// <summary>
    /// Shared options for both client (outbound) and server (inbound) <see cref="QuicConnection" />.
    /// </summary>
    public abstract class QuicConnectionOptions
    {
        /// <summary>
        /// Prevent sub-classing by code outside of this assembly.
        /// </summary>
        internal QuicConnectionOptions()
        { }


        public int MaxInboundBidirectionalStreams { get; set; }
        public int MaxInboundUnidirectionalStreams { get; set; }
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.Zero;
        public long DefaultStreamErrorCode { get; set; } = -1;.
        public long DefaultCloseErrorCode { get; set; } = -1;

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
    
    public sealed class QuicClientConnectionOptions : QuicConnectionOptions
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
    
    public sealed class QuicServerConnectionOptions : QuicConnectionOptions
    {
        public QuicServerConnectionOptions()
        {
            MaxInboundBidirectionalStreams = QuicDefaults.DefaultServerMaxInboundBidirectionalStreams;
            MaxInboundUnidirectionalStreams = QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams;
        }
        
        public SslServerAuthenticationOptions ServerAuthenticationOptions { get; set; } = null!;
    }
}
