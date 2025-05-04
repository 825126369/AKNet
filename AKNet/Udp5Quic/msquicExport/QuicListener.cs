using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp5Quic.Common
{
    internal sealed class QuicListener : IAsyncDisposable
    {
        private readonly QUIC_LISTENER _handle = null;
        private bool _disposed;
        private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();
        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();

        private readonly Func<QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask<QuicServerConnectionOptions>> _connectionOptionsCallback;
        private readonly ConcurrentQueueAsync<QuicConnection> _acceptQueue;
        private int _pendingConnectionsCapacity;
        public IPEndPoint LocalEndPoint { get; }

        public static bool QUIC_FAILED(ulong Status)
        {
            return Status != 0;
        }

        public static ValueTask<QuicListener> ListenAsync(QuicListenerOptions options, CancellationToken cancellationToken = default)
        {
            QuicListener listener = new QuicListener(options);
            NetLog.Log($"{listener} Listener listens on {listener.LocalEndPoint}");
            return new ValueTask<QuicListener>(listener);
        }

        private QuicListener(QuicListenerOptions options)
        {
            if(QUIC_FAILED(MSQuicFunc.MsQuicListenerOpen(MsQuicApi.Api.Registration, NativeCallback, this, ref _handle)))
            {
                NetLog.LogError("ListenerOpen failed");
            }
            
            _connectionOptionsCallback = options.ConnectionOptionsCallback;
            _acceptQueue = new ConcurrentQueueAsync<QuicConnection>();
            _pendingConnectionsCapacity = options.ListenBacklog;

            MsQuicBuffers alpnBuffers = new MsQuicBuffers();
            alpnBuffers.Initialize(options.ApplicationProtocols, applicationProtocol => applicationProtocol.Protocol);
            QUIC_ADDR address = new QUIC_ADDR(options.ListenEndPoint);
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
            try
            {
                QuicConnection item = await _acceptQueue.ReadAsync(cancellationToken).ConfigureAwait(false);
                Interlocked.Increment(ref _pendingConnectionsCapacity);

                if (item is QuicConnection connection)
                {
                    return connection;
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
            }

            return null;
        }

        private async void StartConnectionHandshake(QuicConnection connection, SslClientHelloInfo clientHello)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            CancellationToken cancellationToken = default;
            TimeSpan handshakeTimeout = QuicDefaults.HandshakeTimeout;
            try
            {
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, connection.ConnectionShutdownToken);
                cancellationToken = linkedCts.Token;
                linkedCts.CancelAfter(handshakeTimeout);

                QuicServerConnectionOptions options = await _connectionOptionsCallback(connection, clientHello, cancellationToken).ConfigureAwait(false);

                handshakeTimeout = options.HandshakeTimeout;
                linkedCts.CancelAfter(handshakeTimeout);

                await connection.FinishHandshakeAsync(options, clientHello.ServerName, cancellationToken).ConfigureAwait(false);
                _acceptQueue.Enqueue(connection);
            }
            catch (Exception e)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                NetLog.LogError(e.ToString());
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
            SslClientHelloInfo clientHello = new SslClientHelloInfo(data.Info.ServerName, SslProtocols.Tls12);
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
            if (InterlockedEx.Exchange(ref _disposed, true))
            {
                return;
            }

            if (_shutdownTcs.TryInitialize(out ValueTask valueTask, this))
            {
                MSQuicFunc.MsQuicListenerStop(_handle);
            }

            await valueTask.ConfigureAwait(false);
            _disposeCts.Cancel();
            
            while(_acceptQueue.TryDequeue(out var connection))
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}

