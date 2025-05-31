using AKNet.Common;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp5MSQuic.Common
{
    internal partial class QuicConnection
    {
        private readonly QUIC_CONNECTION _handle;
        private bool _disposed;

        private SslConnectionOptions _sslConnectionOptions;
        private QUIC_CONFIGURATION _configuration;
        private bool _canAccept;
        private ulong _defaultStreamErrorCode;
        private ulong _defaultCloseErrorCode;
        private IPEndPoint _remoteEndPoint = null!;
        private IPEndPoint _localEndPoint = null!;
            
        private Action<QuicConnection, QuicStreamCapacityChangedArgs>? _streamCapacityCallback;
        private Action<QuicStreamType> _decrementStreamCapacity;
        private int _bidirectionalStreamCapacity;
        private int _unidirectionalStreamCapacity;
        private bool _remoteCertificateExposed;
        private X509Certificate2? _remoteCertificate;
        private SslApplicationProtocol _negotiatedApplicationProtocol;
        //private TlsCipherSuite _negotiatedCipherSuite;
        private SslProtocols _negotiatedSslProtocol;
        private readonly MsQuicTlsSecret _tlsSecret;
        public IPEndPoint RemoteEndPoint => _remoteEndPoint;
        public IPEndPoint LocalEndPoint => _localEndPoint;
        private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();
        internal CancellationToken ConnectionShutdownToken => _shutdownTokenSource.Token;

        public string TargetHostName => _sslConnectionOptions.TargetHost;
        public X509Certificate? RemoteCertificate
        {
            get
            {
                _remoteCertificateExposed = true;
                return _remoteCertificate;
            }
        }
        
        public SslApplicationProtocol NegotiatedApplicationProtocol => _negotiatedApplicationProtocol;
        public SslProtocols SslProtocol => _negotiatedSslProtocol;
        private readonly ConcurrentQueueAsync<QuicStream> _acceptQueue = new ConcurrentQueueAsync<QuicStream>();
        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();
        private readonly ValueTaskSource _connectedTcs = new ValueTaskSource();
        private readonly ResettableValueTaskSource _shutdownTcs = new ResettableValueTaskSource()
        {
            CancellationAction = target =>
            {
                try
                {
                    if (target is QuicConnection connection)
                    {
                        connection._shutdownTcs.TrySetResult();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // We collided with a Dispose in another thread. This can happen
                    // when using CancellationTokenSource.CancelAfter.
                    // Ignore the exception
                }
            }
        };

        private readonly TaskCompletionSource<ulong> _connectionCloseTcs = new TaskCompletionSource<ulong>(TaskCreationOptions.RunContinuationsAsynchronously);

        public static ValueTask<QuicConnection> ConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default)
        {
            return StartConnectAsync(options, cancellationToken);
        }

        static async ValueTask<QuicConnection> StartConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken)
        {
            QuicConnection connection = new QuicConnection();

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (options.HandshakeTimeout != Timeout.InfiniteTimeSpan && options.HandshakeTimeout != TimeSpan.Zero)
            {
                linkedCts.CancelAfter(options.HandshakeTimeout);
            }

            try
            {
                await connection.FinishConnectAsync(options, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                NetLog.LogError(ex.ToString());
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }

            return connection;
        }

        public QuicConnection()
        {
            QUIC_CONNECTION handle = null;
            if (QUIC_FAILED(MSQuicFunc.MsQuicConnectionOpen(MsQuicApi.Api.Registration, NativeCallback, this, out handle)))
            {
                NetLog.LogError("ConnectionOpen failed");
            }

            _handle = handle;
            _decrementStreamCapacity = DecrementStreamCapacity;
        }
        
        public QuicConnection(QUIC_CONNECTION handle, QUIC_NEW_CONNECTION_INFO info)
        {
            MSQuicFunc.MsQuicSetCallbackHandler_For_QUIC_CONNECTION(handle, NativeCallback, this);
            _remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(info.RemoteAddress);
            _localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(info.LocalAddress);
            _decrementStreamCapacity = DecrementStreamCapacity;
            _tlsSecret = MsQuicTlsSecret.Create(handle);
        }

        private async ValueTask FinishConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default)
        {
            if (_connectedTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken))
            {
                _canAccept = options.MaxInboundBidirectionalStreams > 0 || options.MaxInboundUnidirectionalStreams > 0;
                _defaultStreamErrorCode = options.DefaultStreamErrorCode;
                _defaultCloseErrorCode = options.DefaultCloseErrorCode;
                _streamCapacityCallback = options.StreamCapacityCallback;

                if (!options.RemoteEndPoint.TryParse(out string? host, out IPAddress? address, out int port))
                {
                    NetLog.LogError("IP");
                }

                if (address == null)
                {
                    Debug.Assert(host != null);

                    IPAddress[] addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (addresses.Length == 0)
                    {
                        NetLog.LogError("Dns GetHostAddressesAsync Error");
                    }
                    address = addresses[0];
                }

                QUIC_ADDR remoteQuicAddress = new QUIC_ADDR(address, port);
                MsQuicHelpers.SetMsQuicParameter(_handle, MSQuicFunc.QUIC_PARAM_CONN_REMOTE_ADDRESS, remoteQuicAddress.ToSSBuffer());

                if (options.LocalEndPoint != null)
                {
                    QUIC_ADDR localQuicAddress = new QUIC_ADDR(options.LocalEndPoint);
                    MsQuicHelpers.SetMsQuicParameter(_handle, MSQuicFunc.QUIC_PARAM_CONN_LOCAL_ADDRESS, localQuicAddress.ToSSBuffer());
                }

                _sslConnectionOptions = new SslConnectionOptions(
                    this,
                    isClient: true,
                    options.ClientAuthenticationOptions.TargetHost ?? host ?? address.ToString(),
                    certificateRequired: true,
                    options.ClientAuthenticationOptions.CertificateRevocationCheckMode,
                    options.ClientAuthenticationOptions.RemoteCertificateValidationCallback,
                    null);

                _configuration = MsQuicConfiguration.Create(options);
                string sni = string.Empty;

                if (MsQuicHelpers.QUIC_FAILED(MSQuicFunc.MsQuicConnectionStart(_handle, _configuration, remoteQuicAddress)))
                {
                    NetLog.LogError("ConnectionStart failed");
                }
            }
            await valueTask.ConfigureAwait(false);
        }

        internal ValueTask FinishHandshakeAsync(QuicServerConnectionOptions options, string targetHost, CancellationToken cancellationToken = default)
        {
            if (_connectedTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken))
            {
                _canAccept = options.MaxInboundBidirectionalStreams > 0 || options.MaxInboundUnidirectionalStreams > 0;
                _defaultStreamErrorCode = options.DefaultStreamErrorCode;
                _defaultCloseErrorCode = options.DefaultCloseErrorCode;
                _streamCapacityCallback = options.StreamCapacityCallback;

                targetHost = string.Empty;
                _sslConnectionOptions = new SslConnectionOptions(
                    this,
                    isClient: false,
                    targetHost,
                    options.ServerAuthenticationOptions.ClientCertificateRequired,
                    options.ServerAuthenticationOptions.CertificateRevocationCheckMode,
                    options.ServerAuthenticationOptions.RemoteCertificateValidationCallback, null);

                _configuration = MsQuicConfiguration.Create(options, targetHost);
                if (QUIC_FAILED(MSQuicFunc.MsQuicConnectionSetConfiguration(_handle, _configuration)))
                {
                    NetLog.LogError("ConnectionSetConfiguration failed");
                }
            }
            return valueTask;
        }
        
        private void DecrementStreamCapacity(QuicStreamType streamType)
        {
            if (streamType == QuicStreamType.Unidirectional)
            {
                --_unidirectionalStreamCapacity;
            }
            if (streamType == QuicStreamType.Bidirectional)
            {
                --_bidirectionalStreamCapacity;
            }
        }
        
        public async ValueTask<QuicStream> OpenOutboundStreamAsync(QuicStreamType type, CancellationToken cancellationToken = default)
        {
            QuicStream stream = null;
            try
            {
                stream = new QuicStream(_handle, type, _defaultStreamErrorCode);
                await stream.StartAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (stream != null)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                }
                NetLog.LogError(ex.ToString());
            }
            return stream;
        }
        
        public async Task<QuicStream> AcceptInboundStreamAsync(CancellationToken cancellationToken = default)
        {
            if (!_canAccept)
            {
                NetLog.LogError("_canAccept false");
                return null;
            }

            try
            {
                return await _acceptQueue.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                NetLog.LogError(ex.ToString());
            }

            await Task.CompletedTask;
            return null;
        }

        public async Task CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            if (_shutdownTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
            {
                MSQuicFunc.MsQuicConnectionShutdown(_handle, QUIC_CONNECTION_SHUTDOWN_FLAGS.QUIC_CONNECTION_SHUTDOWN_FLAG_NONE, (ulong)errorCode);
            }
            await Task.CompletedTask;
        }

        private ulong HandleEventConnected(ref QUIC_CONNECTION_EVENT.CONNECTED_DATA data)
        {
            _negotiatedApplicationProtocol = new SslApplicationProtocol(data.NegotiatedAlpn.GetSpan().ToArray());
            QUIC_HANDSHAKE_INFO info = MsQuicHelpers.GetMsQuicParameter<QUIC_HANDSHAKE_INFO>(_handle, MSQuicFunc.QUIC_PARAM_TLS_HANDSHAKE_INFO);

            QUIC_CIPHER_SUITE _negotiatedCipherSuite = info.CipherSuite;
            _negotiatedSslProtocol = (SslProtocols)info.TlsProtocolVersion;
            NetLog.Assert(_negotiatedSslProtocol == SslProtocols.Tls12, $"Unexpected TLS version {info.TlsProtocolVersion}");

            QUIC_ADDR remoteAddress = MsQuicHelpers.GetMsQuicParameter<QUIC_ADDR>(_handle, MSQuicFunc.QUIC_PARAM_CONN_REMOTE_ADDRESS);
            _remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(remoteAddress);

            QUIC_ADDR localAddress = MsQuicHelpers.GetMsQuicParameter<QUIC_ADDR>(_handle, MSQuicFunc.QUIC_PARAM_CONN_LOCAL_ADDRESS);
            _localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(localAddress);
            _tlsSecret?.WriteSecret();
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleEventShutdownInitiatedByTransport(ref QUIC_CONNECTION_EVENT.SHUTDOWN_INITIATED_BY_TRANSPORT_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventShutdownInitiatedByPeer(ref QUIC_CONNECTION_EVENT.SHUTDOWN_INITIATED_BY_PEER_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleEventShutdownComplete()
        {
            _tlsSecret?.WriteSecret();
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        private ulong HandleEventLocalAddressChanged(ref QUIC_CONNECTION_EVENT.LOCAL_ADDRESS_CHANGED_DATA data)
        {
            _localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(data.Address);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleEventPeerAddressChanged(ref QUIC_CONNECTION_EVENT.PEER_ADDRESS_CHANGED_DATA data)
        {
            _remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(data.Address);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleEventPeerStreamStarted(ref QUIC_CONNECTION_EVENT.PEER_STREAM_STARTED_DATA data)
        {
            QuicStream stream = new QuicStream(_handle, data.Stream, data.Flags, _defaultStreamErrorCode);
            data.Flags |= QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_DELAY_ID_FC_UPDATES;
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleEventStreamsAvailable(ref QUIC_CONNECTION_EVENT.STREAMS_AVAILABLE_DATA data)
        {
            int bidirectionalIncrement = 0;
            int unidirectionalIncrement = 0;
            if (data.BidirectionalCount > 0)
            {
                bidirectionalIncrement = data.BidirectionalCount - _bidirectionalStreamCapacity;
                _bidirectionalStreamCapacity = data.BidirectionalCount;
            }

            if (data.UnidirectionalCount > 0)
            {
                unidirectionalIncrement = data.UnidirectionalCount - _unidirectionalStreamCapacity;
                _unidirectionalStreamCapacity = data.UnidirectionalCount;
            }
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleEventPeerCertificateReceived(ref QUIC_CONNECTION_EVENT.PEER_CERTIFICATE_RECEIVED_DATA data)
        {
            _tlsSecret?.WriteSecret();
            //_sslConnectionOptions.StartAsyncCertificateValidation((data.Certificate, data.Chain));
            return MSQuicFunc.QUIC_STATUS_PENDING;
        }

        private ulong HandleConnectionEvent(ref QUIC_CONNECTION_EVENT connectionEvent)
        {
            NetLog.Log("HandleConnectionEvent: " + connectionEvent.Type.ToString());
            switch (connectionEvent.Type)
            {
                case QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_CONNECTED:
                    HandleEventConnected(ref connectionEvent.CONNECTED);
                    break;
                case QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_SHUTDOWN_INITIATED_BY_TRANSPORT:
                    HandleEventShutdownInitiatedByTransport(ref connectionEvent.SHUTDOWN_INITIATED_BY_TRANSPORT);
                    break;
                case QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_SHUTDOWN_INITIATED_BY_PEER:
                    HandleEventShutdownInitiatedByPeer(ref connectionEvent.SHUTDOWN_INITIATED_BY_PEER);
                    break;
                case QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_SHUTDOWN_COMPLETE:
                    HandleEventShutdownComplete();
                    break;
                case QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_LOCAL_ADDRESS_CHANGED:
                    HandleEventLocalAddressChanged(ref connectionEvent.LOCAL_ADDRESS_CHANGED);
                    break;
                case QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_PEER_ADDRESS_CHANGED:
                    HandleEventPeerAddressChanged(ref connectionEvent.PEER_ADDRESS_CHANGED);
                    break;
                case QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_PEER_STREAM_STARTED:
                    HandleEventPeerStreamStarted(ref connectionEvent.PEER_STREAM_STARTED);
                    break;
                case QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_STREAMS_AVAILABLE:
                    HandleEventStreamsAvailable(ref connectionEvent.STREAMS_AVAILABLE);
                    break;
                case QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_PEER_CERTIFICATE_RECEIVED:
                    HandleEventPeerCertificateReceived(ref connectionEvent.PEER_CERTIFICATE_RECEIVED);
                    break;
            }

            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        
        private static ulong NativeCallback(QUIC_CONNECTION connection, object context, QUIC_CONNECTION_EVENT connectionEvent)
        {
            try
            {
                var _handle = context as QuicConnection;
                return _handle.HandleConnectionEvent(ref connectionEvent);
            }
            catch (Exception ex)
            {
                return MSQuicFunc.QUIC_STATUS_INTERNAL_ERROR;
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            //if (Interlocked.Exchange(ref _disposed, true))
            //{
            //    return;
            //}

            await Task.CompletedTask;

            //if (_shutdownTcs.TryGetValueTask(out ValueTask valueTask, this))
            //{
            //    unsafe
            //    {
            //        MsQuicApi.Api.ConnectionShutdown(
            //            _handle,
            //            QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE,
            //            (ulong)_defaultCloseErrorCode);
            //    }
            //}
            //else if (!valueTask.IsCompletedSuccessfully)
            //{
            //    unsafe
            //    {
            //        MsQuicApi.Api.ConnectionShutdown(
            //            _handle,
            //            QUIC_CONNECTION_SHUTDOWN_FLAGS.SILENT,
            //            (ulong)_defaultCloseErrorCode);
            //    }
            //}

            // Wait for SHUTDOWN_COMPLETE, the last event, so that all resources can be safely released.
            //await _shutdownTcs.GetFinalTask(this).ConfigureAwait(false);
            //Debug.Assert(_connectedTcs.IsCompleted);
            //Debug.Assert(_connectionCloseTcs.Task.IsCompleted);
            //_handle.Dispose();
            //_shutdownTokenSource.Dispose();
            //_connectionCloseTcs.Task.ObserveException();
            //_configuration?.Dispose();

            //// Dispose remote certificate only if it hasn't been accessed via getter, in which case the accessing code becomes the owner of the certificate lifetime.
            //if (!_remoteCertificateExposed)
            //{
            //    _remoteCertificate?.Dispose();
            //}

            //// Flush the queue and dispose all remaining streams.
            //_acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(GetType().FullName)));
            //while (_acceptQueue.Reader.TryRead(out QuicStream? stream))
            //{
            //    await stream.DisposeAsync().ConfigureAwait(false);
            //}
        }

        public static bool QUIC_SUCCESSED(ulong Status)
        {
            return Status != 0;
        }

        public static bool QUIC_FAILED(ulong Status)
        {
            return Status != 0;
        }
    }
}
