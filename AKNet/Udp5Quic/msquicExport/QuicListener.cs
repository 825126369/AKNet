using AKNet.Common;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp5Quic.Common
{

    public sealed class QuicListener : IAsyncDisposable
    {
        public static bool IsSupported => MsQuicApi.IsQuicSupported;

        public static ValueTask<QuicListener> ListenAsync(QuicListenerOptions options, CancellationToken cancellationToken = default)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.SystemNetQuic_PlatformNotSupported, MsQuicApi.NotSupportedReason ?? "General loading failure."));
            }

            QuicListener listener = new QuicListener(options);
            NetLog.Log($"{listener} Listener listens on {listener.LocalEndPoint}");
            return ValueTask.FromResult(listener);
        }


        private readonly QUIC_LISTENER _handle;
        private bool _disposed;
        
        private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();
        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();
        private readonly Func<QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask<QuicServerConnectionOptions>> _connectionOptionsCallback;
        private readonly Q<object> _acceptQueue;
        private int _pendingConnectionsCapacity;
        public IPEndPoint LocalEndPoint { get; }

        public static bool QUIC_FAILED(ulong Status)
        {
            return Status != 0;
        }

        private QuicListener(QuicListenerOptions options)
        {
            if(QUIC_FAILED(MSQuicFunc.MsQuicListenerOpen(MsQuicApi.Api.Registration, NativeCallback, null, ref _handle)))
            {
                NetLog.LogError("ListenerOpen failed");
            }
            
            _connectionOptionsCallback = options.ConnectionOptionsCallback;
            _acceptQueue = Channel.CreateUnbounded<object>();
            _pendingConnectionsCapacity = options.ListenBacklog;

            MsQuicBuffers alpnBuffers = new MsQuicBuffers();
            alpnBuffers.Initialize(options.ApplicationProtocols, applicationProtocol => applicationProtocol.Protocol);
            QUIC_ADDR address = new QUIC_ADDR(options.ListenEndPoint as IPEndPoint);
            if (options.ListenEndPoint.Address.Equals(IPAddress.IPv6Any))
            {
                address.AddressFamily = AddressFamily.Unspecified;
            }

            if (QUIC_FAILED(MSQuicFunc.MsQuicListenerStart(_handle, alpnBuffers.Buffers, alpnBuffers.Count, address)))
            {
                NetLog.LogError("ListenerStart failed");
            }

            LocalEndPoint = options.ListenEndPoint;
        }
        
        public async ValueTask<QuicConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            GCHandle keepObject = GCHandle.Alloc(this);
            try
            {
                object item = await _acceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                Interlocked.Increment(ref _pendingConnectionsCapacity);

                if (item is QuicConnection connection)
                {
                    return connection;
                }
                ExceptionDispatchInfo.Throw((Exception)item);
                throw null; // Never reached.
            }
            catch (ChannelClosedException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Throw(ex.InnerException);
            }
        }

        private async void StartConnectionHandshake(QuicConnection connection, SslClientHelloInfo clientHello)
        {
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            bool wrapException = false;
            CancellationToken cancellationToken = default;
            TimeSpan handshakeTimeout = QuicDefaults.HandshakeTimeout;
            try
            {
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, connection.ConnectionShutdownToken);
                cancellationToken = linkedCts.Token;
                linkedCts.CancelAfter(handshakeTimeout);

                wrapException = true;
                QuicServerConnectionOptions options = await _connectionOptionsCallback(connection, clientHello, cancellationToken).ConfigureAwait(false);
                wrapException = false;

                options.Validate(nameof(options));
                handshakeTimeout = options.HandshakeTimeout;
                linkedCts.CancelAfter(handshakeTimeout);

                await connection.FinishHandshakeAsync(options, clientHello.ServerName, cancellationToken).ConfigureAwait(false);
                if (!_acceptQueue.Writer.TryWrite(connection))
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested && !connection.ConnectionShutdownToken.IsCancellationRequested)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (wrapException && connection.ConnectionShutdownToken.IsCancellationRequested)
                {
                    ValueTask task = connection.FinishHandshakeAsync(null!, null!, default);
                    Debug.Assert(task.IsCompleted);
                    if (task.AsTask().Exception?.InnerException is Exception handshakeException)
                    {
                        ex = handshakeException;
                        wrapException = false;
                    }
                }

                await connection.DisposeAsync().ConfigureAwait(false);
                if (!_acceptQueue.Writer.TryWrite(wrapException ?
                            ExceptionDispatchInfo.SetCurrentStackTrace(new QuicException(QuicError.CallbackError, null, SR.net_quic_callback_error, ex)) :
                            ex))
                {
                    // Channel has been closed, connection is already disposed, do nothing.
                }
            }
        }

        private ulong HandleEventNewConnection(ref QUIC_LISTENER_EVENT.NEW_CONNECTION_DATA data)
        {
            if (Interlocked.Decrement(ref _pendingConnectionsCapacity) < 0)
            {
                Interlocked.Increment(ref _pendingConnectionsCapacity);
                return MSQuicFunc.QUIC_STATUS_CONNECTION_REFUSED;
            }

            QuicConnection connection = new QuicConnection(data.Connection, data.Info);
            SslClientHelloInfo clientHello = new SslClientHelloInfo(data.Info.ServerName.Length > 0 ? data.Info.ServerName : "", SslProtocols.Tls12);
            StartConnectionHandshake(connection, clientHello);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleEventStopComplete()
        {
            _shutdownTcs.TrySetResult();
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private ulong HandleListenerEvent(ref QUIC_LISTENER_EVENT listenerEvent)
        {
            switch (listenerEvent.Type)
            {
                case QUIC_LISTENER_EVENT_TYPE.QUIC_LISTENER_EVENT_NEW_CONNECTION:
                    HandleEventNewConnection(ref listenerEvent.NEW_CONNECTION);
                    break;
                case QUIC_LISTENER_EVENT_TYPE.QUIC_LISTENER_EVENT_STOP_COMPLETE:
                    HandleEventStopComplete();
                    break;
            }
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        
        private static ulong NativeCallback(QUIC_HANDLE listener, object context, QUIC_LISTENER_EVENT listenerEvent)
        {
            QuicListener instance = (QuicListener)context;

            try
            {
                return instance.HandleListenerEvent(ref listenerEvent);
            }
            catch (Exception ex)
            {
                return MSQuicFunc.QUIC_STATUS_INTERNAL_ERROR;
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, true))
            {
                return;
            }
            
            if (_shutdownTcs.TryInitialize(out ValueTask valueTask, this))
            {
               MSQuicFunc.MsQuicListenerStop(_handle);
            }

            await valueTask.ConfigureAwait(false);
            
            await _disposeCts.CancelAsync().ConfigureAwait(false);
            _acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(GetType().FullName)));
            while (_acceptQueue.Reader.TryRead(out object? item))
            {
                if (item is QuicConnection connection)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}

