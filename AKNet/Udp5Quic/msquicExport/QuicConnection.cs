using AKNet.Common;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp5Quic.Common
{
    public sealed partial class QuicConnection : IAsyncDisposable
    {
        public static bool IsSupported => MsQuicApi.IsQuicSupported;
        public static ValueTask<QuicConnection> ConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default)
        {
            if (!IsSupported)
            {
                NetLog.LogError(MsQuicApi.NotSupportedReason ?? "General loading failure.");
            }
            return StartConnectAsync(options, cancellationToken);
        }

        static async ValueTask<QuicConnection> StartConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken)
        {
            QuicConnection connection = new QuicConnection();

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

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
        
        private readonly QUIC_CONNECTION _handle;
        private bool _disposed;

        //private readonly TaskCompletionSource _connectionCloseTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        //private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();
        //internal CancellationToken ConnectionShutdownToken => _shutdownTokenSource.Token;

        //private readonly Channel<QuicStream> _acceptQueue = Channel.CreateUnbounded<QuicStream>(new UnboundedChannelOptions()
        //{
        //    SingleWriter = true
        //});

        private SslConnectionOptions _sslConnectionOptions;
        private QUIC_CONFIGURATION _configuration;
        private bool _canAccept;
        private long _defaultStreamErrorCode;
        private long _defaultCloseErrorCode;
        private IPEndPoint _remoteEndPoint = null!;
        private IPEndPoint _localEndPoint = null!;
        

        private Action<QuicConnection, QuicStreamCapacityChangedArgs>? _streamCapacityCallback;
        private Action<QuicStreamType> _decrementStreamCapacity;
        private int _bidirectionalStreamCapacity;
        private int _unidirectionalStreamCapacity;
        private bool _remoteCertificateExposed;
        private X509Certificate2? _remoteCertificate;
        private SslApplicationProtocol _negotiatedApplicationProtocol;
        private TlsCipherSuite _negotiatedCipherSuite;
        private SslProtocols _negotiatedSslProtocol;
        private readonly MsQuicTlsSecret _tlsSecret;

        public IPEndPoint RemoteEndPoint => _remoteEndPoint;
        public IPEndPoint LocalEndPoint => _localEndPoint;

        private async void OnStreamCapacityIncreased(int bidirectionalIncrement, int unidirectionalIncrement)
        {
            if (_streamCapacityCallback is null)
            {
                return;
            }
            
            if (bidirectionalIncrement == 0 && unidirectionalIncrement == 0)
            {
                return;
            }

            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            try
            {
                _streamCapacityCallback(this, new QuicStreamCapacityChangedArgs { BidirectionalIncrement = bidirectionalIncrement, UnidirectionalIncrement = unidirectionalIncrement });
            }
            catch (Exception ex)
            {
                
            }
        }
        
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
        public TlsCipherSuite NegotiatedCipherSuite => _negotiatedCipherSuite;
        public SslProtocols SslProtocol => _negotiatedSslProtocol;
        public override string ToString() => _handle.ToString();

        private QuicConnection()
        {
            GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
            try
            {
                QUIC_CONNECTION handle;
                if (QUIC_FAILED(MSQuicFunc.MsQuicConnectionOpen(MsQuicApi.Api.Registration, NativeCallback, (void*)GCHandle.ToIntPtr(context), ref handle)))
                {
                    NetLog.LogError("ConnectionOpen failed");
                }

                _handle = handle;
            }
            catch
            {
                context.Free();
                throw;
            }

            _decrementStreamCapacity = DecrementStreamCapacity;
            _tlsSecret = MsQuicTlsSecret.Create(_handle);
        }
        
        public QuicConnection(QUIC_HANDLE handle, QUIC_NEW_CONNECTION_INFO info)
        {
            GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
            try
            {
                MSQuicFunc.MsQuicSetCallbackHandler(_handle, NativeCallback, _handle);
            }
            catch
            {
                context.Free();
                throw;
            }

            _remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(info.RemoteAddress);
            _localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(info.LocalAddress);
            _decrementStreamCapacity = DecrementStreamCapacity;
            _tlsSecret = MsQuicTlsSecret.Create(_handle);
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
                    throw new ArgumentException(SR.Format(SR.net_quic_unsupported_endpoint_type, options.RemoteEndPoint.GetType()), nameof(options));
                }

                if (address is null)
                {
                    Debug.Assert(host is not null);

                    // Given just a ServerName to connect to, MsQuic would also use the first address after the resolution
                    // (https://github.com/microsoft/msquic/issues/1181) and it would not return a well-known error code
                    // for resolution failures we could rely on. By doing the resolution in managed code, we can guarantee
                    // that a SocketException will surface to the user if the name resolution fails.
                    IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (addresses.Length == 0)
                    {
                        throw new SocketException((int)SocketError.HostNotFound);
                    }
                    address = addresses[0];
                }

                QuicAddr remoteQuicAddress = new IPEndPoint(address, port).ToQuicAddr();
                MsQuicHelpers.SetMsQuicParameter(_handle, QUIC_PARAM_CONN_REMOTE_ADDRESS, remoteQuicAddress);

                if (options.LocalEndPoint is not null)
                {
                    QuicAddr localQuicAddress = options.LocalEndPoint.ToQuicAddr();
                    MsQuicHelpers.SetMsQuicParameter(_handle, QUIC_PARAM_CONN_LOCAL_ADDRESS, localQuicAddress);
                }

                _sslConnectionOptions = new SslConnectionOptions(
                    this,
                    isClient: true,
                    options.ClientAuthenticationOptions.TargetHost ?? host ?? address.ToString(),
                    certificateRequired: true,
                    options.ClientAuthenticationOptions.CertificateRevocationCheckMode,
                    options.ClientAuthenticationOptions.RemoteCertificateValidationCallback,
                    options.ClientAuthenticationOptions.CertificateChainPolicy?.Clone());
                _configuration = MsQuicConfiguration.Create(options);

                // RFC 6066 forbids IP literals.
                // IDN mapping is handled by MsQuic.
                string sni = (TargetHostNameHelper.IsValidAddress(options.ClientAuthenticationOptions.TargetHost) ? null : options.ClientAuthenticationOptions.TargetHost) ?? host ?? string.Empty;

                IntPtr targetHostPtr = Marshal.StringToCoTaskMemUTF8(sni);
                try
                {
                    unsafe
                    {
                        ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConnectionStart(
                            _handle,
                            _configuration,
                            (ushort)remoteQuicAddress.Family,
                            (sbyte*)targetHostPtr,
                            (ushort)port),
                            "ConnectionStart failed");
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(targetHostPtr);
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

                // RFC 6066 forbids IP literals, avoid setting IP address here for consistency with SslStream
                if (TargetHostNameHelper.IsValidAddress(targetHost))
                {
                    targetHost = string.Empty;
                }

                _sslConnectionOptions = new SslConnectionOptions(
                    this,
                    isClient: false,
                    targetHost,
                    options.ServerAuthenticationOptions.ClientCertificateRequired,
                    options.ServerAuthenticationOptions.CertificateRevocationCheckMode,
                    options.ServerAuthenticationOptions.RemoteCertificateValidationCallback,
                    options.ServerAuthenticationOptions.CertificateChainPolicy?.Clone());
                _configuration = MsQuicConfiguration.Create(options, targetHost);

                unsafe
                {
                    ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConnectionSetConfiguration(
                        _handle,
                        _configuration),
                        "ConnectionSetConfiguration failed");
                }
            }

            return valueTask;
        }

        /// <summary>
        /// In order to provide meaningful increments in <see cref="_streamCapacityCallback"/>, available streams count can be only manipulated from MsQuic thread.
        /// For that purpose we pass this function to <see cref="QuicStream"/> so that it can call it from <c>START_COMPLETE</c> event handler.
        ///
        /// Note that MsQuic itself manipulates stream counts right before indicating <c>START_COMPLETE</c> event.
        /// </summary>
        /// <param name="streamType">Type of the stream to decrement appropriate field.</param>
        private void DecrementStreamCapacity(QuicStreamType streamType)
        {
            if (streamType == QuicStreamType.Unidirectional)
            {
                --_unidirectionalStreamCapacity;
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, $"{this} decremented stream count for {streamType} to {_unidirectionalStreamCapacity}.");
                }
            }
            if (streamType == QuicStreamType.Bidirectional)
            {
                --_bidirectionalStreamCapacity;
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, $"{this} decremented stream count for {streamType} to {_bidirectionalStreamCapacity}.");
                }
            }
        }

        /// <summary>
        /// Create an outbound uni/bidirectional <see cref="QuicStream" />.
        /// In case the connection doesn't have any available stream capacity, i.e.: the peer limits the concurrent stream count,
        /// the operation will pend until the stream can be opened (other stream gets closed or peer increases the stream limit).
        /// </summary>
        /// <param name="type">The type of the stream, i.e. unidirectional or bidirectional.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An asynchronous task that completes with the opened <see cref="QuicStream" />.</returns>
        public async ValueTask<QuicStream> OpenOutboundStreamAsync(QuicStreamType type, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            QuicStream? stream = null;
            try
            {
                stream = new QuicStream(_handle, type, _defaultStreamErrorCode);

                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, $"{this} New outbound {type} stream {stream}.");
                }

                await stream.StartAsync(_decrementStreamCapacity, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (stream is not null)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                }

                // Propagate ODE if disposed in the meantime.
                ObjectDisposedException.ThrowIf(_disposed, this);

                // Propagate connection error when the connection was closed (remotely = ABORTED / locally = INVALID_STATE).
                if (ex is QuicException qex && qex.QuicError == QuicError.InternalError &&
                   (qex.HResult == QUIC_STATUS_ABORTED || qex.HResult == QUIC_STATUS_INVALID_STATE))
                {
                    await _connectionCloseTcs.Task.ConfigureAwait(false);
                }
                throw;
            }
            return stream;
        }

        /// <summary>
        /// Accepts an inbound <see cref="QuicStream" />.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An asynchronous task that completes with the accepted <see cref="QuicStream" />.</returns>
        public async ValueTask<QuicStream> AcceptInboundStreamAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_canAccept)
            {
                throw new InvalidOperationException(SR.net_quic_accept_not_allowed);
            }

            GCHandle keepObject = GCHandle.Alloc(this);
            try
            {
                return await _acceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Throw(ex.InnerException);
                throw;
            }
            finally
            {
                keepObject.Free();
            }
        }

        /// <summary>
        /// Closes the connection with the application provided code, see <see href="https://www.rfc-editor.org/rfc/rfc9000.html#immediate-close">RFC 9000: Connection Termination</see> for more details.
        /// </summary>
        /// <remarks>
        /// Connection close is not graceful in regards to its streams, i.e.: calling <see cref="CloseAsync(long, CancellationToken)"/> will immediately close all streams associated with this connection.
        /// Make sure, that all streams have been closed and all their data consumed before calling this method;
        /// otherwise, all the data that were received but not consumed yet, will be lost.
        ///
        /// If <see cref="CloseAsync(long, CancellationToken)"/> is not called before <see cref="DisposeAsync">disposing</see> the connection,
        /// the <see cref="QuicConnectionOptions.DefaultCloseErrorCode"/> will be used by <see cref="DisposeAsync"/> to close the connection.
        /// </remarks>
        /// <param name="errorCode">Application provided code with the reason for closure.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An asynchronous task that completes when the connection is closed.</returns>
        public ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ThrowHelper.ValidateErrorCode(nameof(errorCode), errorCode, $"{nameof(CloseAsync)}.{nameof(errorCode)}");

            if (_shutdownTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, $"{this} Closing connection, Error code = {errorCode}");
                }

                unsafe
                {
                    MsQuicApi.Api.ConnectionShutdown(
                        _handle,
                        QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE,
                        (ulong)errorCode);
                }
            }

            return valueTask;
        }

        private unsafe int HandleEventConnected(ref CONNECTED_DATA data)
        {
            _negotiatedApplicationProtocol = new SslApplicationProtocol(new Span<byte>(data.NegotiatedAlpn, data.NegotiatedAlpnLength).ToArray());

            QUIC_HANDSHAKE_INFO info = MsQuicHelpers.GetMsQuicParameter<QUIC_HANDSHAKE_INFO>(_handle, QUIC_PARAM_TLS_HANDSHAKE_INFO);

            // QUIC_CIPHER_SUITE and QUIC_TLS_PROTOCOL_VERSION use the same values as the corresponding TlsCipherSuite and SslProtocols members.
            _negotiatedCipherSuite = (TlsCipherSuite)info.CipherSuite;
            _negotiatedSslProtocol = (SslProtocols)info.TlsProtocolVersion;

            // currently only TLS 1.3 is defined for QUIC
            Debug.Assert(_negotiatedSslProtocol == SslProtocols.Tls13, $"Unexpected TLS version {info.TlsProtocolVersion}");

            QuicAddr remoteAddress = MsQuicHelpers.GetMsQuicParameter<QuicAddr>(_handle, QUIC_PARAM_CONN_REMOTE_ADDRESS);
            _remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(&remoteAddress);

            QuicAddr localAddress = MsQuicHelpers.GetMsQuicParameter<QuicAddr>(_handle, QUIC_PARAM_CONN_LOCAL_ADDRESS);
            _localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(&localAddress);

            // Final (1-RTT) secrets have been derived, log them if desired to allow decrypting application traffic.
            _tlsSecret?.WriteSecret();

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"{this} Connection connected {LocalEndPoint} -> {RemoteEndPoint} for {_negotiatedApplicationProtocol} protocol");
            }
            _connectedTcs.TrySetResult();
            return QUIC_STATUS_SUCCESS;
        }
        private unsafe int HandleEventShutdownInitiatedByTransport(ref SHUTDOWN_INITIATED_BY_TRANSPORT_DATA data)
        {
            Exception exception = ExceptionDispatchInfo.SetCurrentStackTrace(ThrowHelper.GetExceptionForMsQuicStatus(data.Status, (long)data.ErrorCode));
            _connectedTcs.TrySetException(exception);
            _connectionCloseTcs.TrySetException(exception);
            _acceptQueue.Writer.TryComplete(exception);
            return QUIC_STATUS_SUCCESS;
        }
        private unsafe int HandleEventShutdownInitiatedByPeer(ref SHUTDOWN_INITIATED_BY_PEER_DATA data)
        {
            Exception exception = ExceptionDispatchInfo.SetCurrentStackTrace(ThrowHelper.GetConnectionAbortedException((long)data.ErrorCode));
            _connectionCloseTcs.TrySetException(exception);
            _acceptQueue.Writer.TryComplete(exception);
            return QUIC_STATUS_SUCCESS;
        }
        private unsafe int HandleEventShutdownComplete()
        {
            // make sure we log at least some secrets in case of shutdown before handshake completes.
            _tlsSecret?.WriteSecret();

            Exception exception = ExceptionDispatchInfo.SetCurrentStackTrace(_disposed ? new ObjectDisposedException(GetType().FullName) : ThrowHelper.GetOperationAbortedException());
            _connectionCloseTcs.TrySetException(exception);
            _acceptQueue.Writer.TryComplete(exception);
            _connectedTcs.TrySetException(exception);
            _shutdownTokenSource.Cancel();
            _shutdownTcs.TrySetResult(final: true);
            return QUIC_STATUS_SUCCESS;
        }
        private unsafe int HandleEventLocalAddressChanged(ref LOCAL_ADDRESS_CHANGED_DATA data)
        {
            _localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(data.Address);
            return QUIC_STATUS_SUCCESS;
        }
        private unsafe int HandleEventPeerAddressChanged(ref PEER_ADDRESS_CHANGED_DATA data)
        {
            _remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(data.Address);
            return QUIC_STATUS_SUCCESS;
        }
        private unsafe int HandleEventPeerStreamStarted(ref PEER_STREAM_STARTED_DATA data)
        {
            QuicStream stream = new QuicStream(_handle, data.Stream, data.Flags, _defaultStreamErrorCode);

            if (NetEventSource.Log.IsEnabled())
            {
                QuicStreamType type = data.Flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL) ? QuicStreamType.Unidirectional : QuicStreamType.Bidirectional;
                NetEventSource.Info(this, $"{this} New inbound {type} stream {stream}, Id = {stream.Id}.");
            }

            if (!_acceptQueue.Writer.TryWrite(stream))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(this, $"{this} Unable to enqueue incoming stream {stream}");
                }

                stream.Dispose();
                return QUIC_STATUS_SUCCESS;
            }

            data.Flags |= QUIC_STREAM_OPEN_FLAGS.DELAY_ID_FC_UPDATES;
            return QUIC_STATUS_SUCCESS;
        }
        private unsafe int HandleEventStreamsAvailable(ref STREAMS_AVAILABLE_DATA data)
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
            OnStreamCapacityIncreased(bidirectionalIncrement, unidirectionalIncrement);
            return QUIC_STATUS_SUCCESS;
        }
        private unsafe int HandleEventPeerCertificateReceived(ref PEER_CERTIFICATE_RECEIVED_DATA data)
        {
            //
            // The certificate validation is an expensive operation and we don't want to delay MsQuic
            // worker thread. So we offload the validation to the .NET thread pool. Incidentally, this
            // also prevents potential user RemoteCertificateValidationCallback from blocking MsQuic
            // worker threads.
            //

            // Handshake keys should be available by now, log them now if desired.
            _tlsSecret?.WriteSecret();

            var task = _sslConnectionOptions.StartAsyncCertificateValidation((IntPtr)data.Certificate, (IntPtr)data.Chain);
            if (task.IsCompletedSuccessfully)
            {
                return task.Result ? QUIC_STATUS_SUCCESS : QUIC_STATUS_BAD_CERTIFICATE;
            }

            return QUIC_STATUS_PENDING;
        }

        private unsafe int HandleConnectionEvent(ref QUIC_CONNECTION_EVENT connectionEvent)
            => connectionEvent.Type switch
            {
                QUIC_CONNECTION_EVENT_TYPE.CONNECTED => HandleEventConnected(ref connectionEvent.CONNECTED),
                QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_TRANSPORT => HandleEventShutdownInitiatedByTransport(ref connectionEvent.SHUTDOWN_INITIATED_BY_TRANSPORT),
                QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_PEER => HandleEventShutdownInitiatedByPeer(ref connectionEvent.SHUTDOWN_INITIATED_BY_PEER),
                QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_COMPLETE => HandleEventShutdownComplete(),
                QUIC_CONNECTION_EVENT_TYPE.LOCAL_ADDRESS_CHANGED => HandleEventLocalAddressChanged(ref connectionEvent.LOCAL_ADDRESS_CHANGED),
                QUIC_CONNECTION_EVENT_TYPE.PEER_ADDRESS_CHANGED => HandleEventPeerAddressChanged(ref connectionEvent.PEER_ADDRESS_CHANGED),
                QUIC_CONNECTION_EVENT_TYPE.PEER_STREAM_STARTED => HandleEventPeerStreamStarted(ref connectionEvent.PEER_STREAM_STARTED),
                QUIC_CONNECTION_EVENT_TYPE.STREAMS_AVAILABLE => HandleEventStreamsAvailable(ref connectionEvent.STREAMS_AVAILABLE),
                QUIC_CONNECTION_EVENT_TYPE.PEER_CERTIFICATE_RECEIVED => HandleEventPeerCertificateReceived(ref connectionEvent.PEER_CERTIFICATE_RECEIVED),
                _ => QUIC_STATUS_SUCCESS,
            };

#pragma warning disable CS3016
        [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
        private static unsafe int NativeCallback(QUIC_HANDLE* connection, void* context, QUIC_CONNECTION_EVENT* connectionEvent)
        {
            GCHandle stateHandle = GCHandle.FromIntPtr((IntPtr)context);

            // Check if the instance hasn't been collected.
            if (!stateHandle.IsAllocated || stateHandle.Target is not QuicConnection instance)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(null, $"Received event {connectionEvent->Type} for [conn][{(nint)connection:X11}] while connection is already disposed");
                }
                return QUIC_STATUS_INVALID_STATE;
            }

            try
            {
                // Process the event.
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(instance, $"{instance} Received event {connectionEvent->Type} {connectionEvent->ToString()}");
                }
                return instance.HandleConnectionEvent(ref *connectionEvent);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(instance, $"{instance} Exception while processing event {connectionEvent->Type}: {ex}");
                }
                return QUIC_STATUS_INTERNAL_ERROR;
            }
        }

        /// <summary>
        /// If not closed explicitly by <see cref="CloseAsync(long, CancellationToken)" />, closes the connection with the <see cref="QuicConnectionOptions.DefaultCloseErrorCode"/>.
        /// And releases all resources associated with the connection.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, true))
            {
                return;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"{this} Disposing.");
            }

            // Check if the connection has been shut down and if not, shut it down.
            if (_shutdownTcs.TryGetValueTask(out ValueTask valueTask, this))
            {
                unsafe
                {
                    MsQuicApi.Api.ConnectionShutdown(
                        _handle,
                        QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE,
                        (ulong)_defaultCloseErrorCode);
                }
            }
            else if (!valueTask.IsCompletedSuccessfully)
            {
                unsafe
                {
                    MsQuicApi.Api.ConnectionShutdown(
                        _handle,
                        QUIC_CONNECTION_SHUTDOWN_FLAGS.SILENT,
                        (ulong)_defaultCloseErrorCode);
                }
            }

            // Wait for SHUTDOWN_COMPLETE, the last event, so that all resources can be safely released.
            await _shutdownTcs.GetFinalTask(this).ConfigureAwait(false);
            Debug.Assert(_connectedTcs.IsCompleted);
            Debug.Assert(_connectionCloseTcs.Task.IsCompleted);
            _handle.Dispose();
            _shutdownTokenSource.Dispose();
            _connectionCloseTcs.Task.ObserveException();
            _configuration?.Dispose();

            // Dispose remote certificate only if it hasn't been accessed via getter, in which case the accessing code becomes the owner of the certificate lifetime.
            if (!_remoteCertificateExposed)
            {
                _remoteCertificate?.Dispose();
            }

            // Flush the queue and dispose all remaining streams.
            _acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(GetType().FullName)));
            while (_acceptQueue.Reader.TryRead(out QuicStream? stream))
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
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
