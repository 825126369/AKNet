/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:01
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

#if USE_MSQUIC_2
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.MSQuic.Common
{
    internal sealed class QuicListener
    {
        private QUIC_LISTENER _handle = null;
        public QuicListenerOptions mOption;
        public IPEndPoint LocalEndPoint;
        private bool _disposed = false;
        private readonly ConcurrentQueue<QuicConnection> _acceptQueue = new ConcurrentQueue<QuicConnection>();
        private int currentConnectionsCount;
        private readonly KKResettableValueTaskSource newConnectionTcs = new KKResettableValueTaskSource();

        private void Init(QUIC_LISTENER _handle, QuicListenerOptions options)
        {
            this._handle = _handle;
            this.mOption = options;
            this.LocalEndPoint = options.ListenEndPoint;
        }

        private static QuicListener Create(QuicListenerOptions options)
        {
            QuicListener mListenerer = new QuicListener();
            if (options.ListenBacklog > 0)
            {
                mListenerer.currentConnectionsCount = options.ListenBacklog;
            }
            else
            {
                mListenerer.currentConnectionsCount = ushort.MaxValue;
            }

            QUIC_LISTENER _handle = null;
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicListenerOpen(MsQuicApi.Api.Registration, NativeCallback, mListenerer, out _handle)))
            {
                NetLog.LogError("ListenerOpen failed");
                return null;
            }

            List<QUIC_BUFFER> mAlpnList = new List<QUIC_BUFFER>();
            foreach (var v in ServerConfig.ApplicationProtocols)
            {
                mAlpnList.Add(new QUIC_BUFFER(Encoding.ASCII.GetBytes(v)));
            }

#if USE_MSQUIC_2
            QUIC_ADDR address = new QUIC_ADDR(options.ListenEndPoint);
            if (options.ListenEndPoint.Address.Equals(IPAddress.IPv6Any))
            {
                address.Family = System.Net.Sockets.AddressFamily.Unspecified;
            }
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicListenerStart(_handle, mAlpnList.ToArray(), mAlpnList.Count, address)))
            {
                NetLog.LogError("ListenerStart failed");
                return null;
            }
#else
            IPEndPoint address = options.ListenEndPoint;
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicListenerStart(_handle, mAlpnList.ToArray(), mAlpnList.Count, address)))
            {
                NetLog.LogError("ListenerStart failed");
                return null;
            }
#endif
            mListenerer.Init(_handle, options);
            return mListenerer;
        }

        public static QuicListener StartListen(QuicListenerOptions options, CancellationToken cancellationToken = default)
        {
            return Create(options);
        }

        public void Close()
        {
            MSQuicFunc.MsQuicListenerStop(_handle);
        }
        
        public async ValueTask<QuicConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("QuicListener");
            }

            QuicConnection connection = null;
            do
            {
                if (!newConnectionTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
                {
                    throw new InvalidOperationException();
                }
                if(_acceptQueue.TryDequeue(out connection))
                {
                    newConnectionTcs.TrySetResult();
                }
                await valueTask.ConfigureAwait(false);
            }while (connection == null);

            Interlocked.Increment(ref currentConnectionsCount);
            return connection;
        }

        private async void StartOpNewConnection(QuicConnection connection)
        {
            QuicConnectionOptions options = mOption.GetConnectionOptionFunc();
            QUIC_CONFIGURATION _configuration = ServerConfig.Create(options);
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicConnectionSetConfiguration(connection._handle, _configuration))) //����Ὺʼִ�����Ӳ���
            {
                NetLog.LogError("ConnectionSetConfiguration failed");
            }

            await connection.FinishHandshakeAsync().ConfigureAwait(false);
            _acceptQueue.Enqueue(connection);
            newConnectionTcs.TrySetResult();
        }

        private int HandleEventNewConnection(ref QUIC_LISTENER_EVENT.NEW_CONNECTION_DATA data)
        {
            if (Interlocked.Decrement(ref currentConnectionsCount) < 0)
            {
                Interlocked.Increment(ref currentConnectionsCount);
                return MSQuicFunc.QUIC_STATUS_CONNECTION_REFUSED;
            }

            QuicConnectionOptions options = mOption.GetConnectionOptionFunc();
            QuicConnection connection = new QuicConnection(data.Connection, data.Info, options);
            connection._sslConnectionOptions = new QuicConnection.SslConnectionOptions(
                 connection,
                 isClient: false,
                 data.Info.ServerName,
                 options.ServerAuthenticationOptions.ClientCertificateRequired,
                 options.ServerAuthenticationOptions.CertificateRevocationCheckMode,
                 options.ServerAuthenticationOptions.RemoteCertificateValidationCallback, null);

            StartOpNewConnection(connection);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventStopComplete()
        {
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

