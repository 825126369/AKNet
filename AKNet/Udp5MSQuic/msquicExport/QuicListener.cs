using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp5MSQuic.Common
{
    internal sealed class QuicListener : IAsyncDisposable
    {
        private readonly QUIC_LISTENER _handle = null;
        private bool _disposed;
        private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();
        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();

        private readonly QuicListenerOptions mOption;
        private int _pendingConnectionsCapacity;
        public IPEndPoint LocalEndPoint { get; }

        private QuicListener(QuicListenerOptions options)
        {
            this.mOption = options;
            if(MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicListenerOpen(MsQuicApi.Api.Registration, NativeCallback, this, out _handle)))
            {
                NetLog.LogError("ListenerOpen failed");
            }

            List<QUIC_BUFFER> mAlpnList = new List<QUIC_BUFFER>();
            foreach (var v in ServerConfig.ApplicationProtocols)
            {
                mAlpnList.Add(new QUIC_BUFFER(Encoding.ASCII.GetBytes(v)));
            }
            QUIC_ADDR address = new QUIC_ADDR(options.ListenEndPoint);
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicListenerStart(_handle, mAlpnList.ToArray(), mAlpnList.Count, address)))
            {
                NetLog.LogError("ListenerStart failed");
            }

            LocalEndPoint = options.ListenEndPoint;
        }

        public static QuicListener StartListen(QuicListenerOptions options, CancellationToken cancellationToken = default)
        {
            return new QuicListener(options);
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
        }

        private int HandleEventNewConnection(ref QUIC_LISTENER_EVENT.NEW_CONNECTION_DATA data)
        {
            QuicConnection connection = new QuicConnection(data.Connection, data.Info);
            QuicServerConnectionOptions options = mOption.GetConnectionOptionFunc(connection);
            connection._sslConnectionOptions = new QuicConnection.SslConnectionOptions(
                  connection,
                  isClient: false,
                  data.Info.ServerName,
                  options.ServerAuthenticationOptions.ClientCertificateRequired,
                  options.ServerAuthenticationOptions.CertificateRevocationCheckMode,
                  options.ServerAuthenticationOptions.RemoteCertificateValidationCallback, null);

            QUIC_CONFIGURATION _configuration = ServerConfig.Create(options);
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicConnectionSetConfiguration(connection._handle, _configuration)))
            {
                NetLog.LogError("ConnectionSetConfiguration failed");
                return MSQuicFunc.QUIC_STATUS_INTERNAL_ERROR;
            }
            mOption.AcceptConnectionFunc(connection);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventStopComplete()
        {
            _shutdownTcs.TrySetResult();
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleListenerEvent(ref QUIC_LISTENER_EVENT listenerEvent)
        {
            NetLog.Log("HandleListenerEvent: " + listenerEvent.Type);
            switch (listenerEvent.Type)
            {
                case QUIC_LISTENER_EVENT_TYPE.QUIC_LISTENER_EVENT_NEW_CONNECTION:
                    HandleEventNewConnection(ref listenerEvent.NEW_CONNECTION);
                    break;
                case QUIC_LISTENER_EVENT_TYPE.QUIC_LISTENER_EVENT_STOP_COMPLETE:
                    HandleEventStopComplete();
                    break;
                default:
                    break;
            }
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        
        private static int NativeCallback(QUIC_HANDLE listener, object context, ref QUIC_LISTENER_EVENT listenerEvent)
        {
            QuicListener instance = (QuicListener)context;
            return instance.HandleListenerEvent(ref listenerEvent);
        }
      
    }
}

