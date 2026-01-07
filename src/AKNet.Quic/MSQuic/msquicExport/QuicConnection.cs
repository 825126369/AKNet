/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Common.Channel;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.MSQuic.Common
{
    internal partial class QuicConnection
    {
        public readonly QUIC_CONNECTION _handle;
        public SslConnectionOptions _sslConnectionOptions;
        private QUIC_CONFIGURATION _configuration;
        public readonly QuicConnectionOptions mOption;
        public readonly ConcurrentQueue<QuicStream> mReceiveStreamDataQueue = new ConcurrentQueue<QuicStream>();
        public readonly EndPoint RemoteEndPoint;
        private readonly AKNetChannel<QuicStream> _acceptQueue = new AKNetChannel<QuicStream>(true);
        private int _disposed;

        private readonly KKValueTaskSource _connectedTcs = new KKValueTaskSource();
        private readonly KKResettableValueTaskSource _shutdownTcs = new KKResettableValueTaskSource()
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
                }
            }
        };

        private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();
        internal CancellationToken ConnectionShutdownToken => _shutdownTokenSource.Token;
        private readonly KKValueTaskSource _connectionCloseTcs = new KKValueTaskSource();
        public QuicConnection(QuicConnectionOptions mOption)
        {
            this.mOption = mOption;
            this._handle = null;
            this.RemoteEndPoint = mOption.RemoteEndPoint;
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicConnectionOpen(MsQuicApi.Api.Registration, NativeCallback, this, out _handle)))
            {
                NetLog.LogError("ConnectionOpen failed");
            }
        }
        
        public QuicConnection(QUIC_CONNECTION handle, QUIC_NEW_CONNECTION_INFO info, QuicConnectionOptions mOption)
        {
            this.mOption = mOption;
            this._handle = handle;
            this.RemoteEndPoint = info.RemoteAddress.GetIPEndPoint();
            MSQuicFunc.MsQuicSetCallbackHandler_For_QUIC_CONNECTION(handle, NativeCallback, this);
        }

        public static async ValueTask<QuicConnection> ConnectAsync(QuicConnectionOptions mOption, CancellationToken cancellationToken = default)
        {
            QuicConnection connection = new QuicConnection(mOption);
            await connection.StartConnectAsync(mOption, cancellationToken);
            return connection;
        }

        private async ValueTask StartConnectAsync(QuicConnectionOptions mOption, CancellationToken cancellationToken = default)
        {
            if (!mOption.RemoteEndPoint.TryParse(out string? host, out IPAddress? address, out int port))
            {
                throw new ArgumentException();
            }

            if (address == null)
            {
                Debug.Assert(host != null);
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
                if (addresses.Length == 0)
                {
                    throw new SocketException((int)SocketError.HostNotFound);
                }
                address = addresses[0];
            }
            
            IPEndPoint m = new IPEndPoint(address, port);
            QUIC_ADDR remoteQuicAddress = new QUIC_ADDR(address, port);
            MsQuicHelpers.SetMsQuicParameter(_handle, MSQuicFunc.QUIC_PARAM_CONN_REMOTE_ADDRESS, remoteQuicAddress.ToSSBuffer());
            this._sslConnectionOptions = new SslConnectionOptions(
                this,
                isClient: true,
                mOption.ClientAuthenticationOptions.TargetHost ?? host ?? address.ToString(),
                certificateRequired: true,
                mOption.ClientAuthenticationOptions.CertificateRevocationCheckMode,
                mOption.ClientAuthenticationOptions.RemoteCertificateValidationCallback,
                null);

            _configuration = ClientConfig.Create(true);
            string sni = mOption.ClientAuthenticationOptions.TargetHost ?? host ?? address.ToString();
            remoteQuicAddress.ServerName = sni;

            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicConnectionStart(_handle, _configuration, remoteQuicAddress)))
            {
                NetLog.LogError("ConnectionStart failed");
            }

            await FinishHandshakeAsync();
        }

        internal ValueTask FinishHandshakeAsync()
        {
            if (_connectedTcs.TryInitialize(out ValueTask valueTask, this))
            {
                
            }

            return valueTask;
        }

        private void OnStreamCapacityIncreased(int bidirectionalIncrement, int unidirectionalIncrement)
        {

        }

        public async ValueTask<QuicStream> OpenOutboundStreamAsync(QuicStreamType type, CancellationToken cancellationToken = default)
        {
            if (_disposed > 0)
            {
                throw new ObjectDisposedException(this.ToString());
            }
            
            QuicStream? stream = null;
            try
            {
                stream = new QuicStream(this, type);
                await stream.StartAsync(DecrementStreamCapacity, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                NetLog.LogError(ex.ToString());
                if (stream != null)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                }

                if (_disposed > 0)
                {
                    throw new ObjectDisposedException(this.ToString());
                }

                throw;
            }
            return stream;
        }

        private int _bidirectionalStreamCapacity;
        private int _unidirectionalStreamCapacity;
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

        public async ValueTask<QuicStream> AcceptInboundStreamAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed > 0)
            {
                throw new ObjectDisposedException(this.ToString());
            }

            try
            {
                return await _acceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Throw(ex.InnerException);
                throw;
            }
        }

        public ValueTask CloseAsync(int errorCode, CancellationToken cancellationToken = default)
        {
            if (_disposed > 0)
            {
                throw new ObjectDisposedException(this.ToString());
            }

            if (_shutdownTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
            {
                MSQuicFunc.MsQuicConnectionShutdown(_handle, QUIC_CONNECTION_SHUTDOWN_FLAGS.QUIC_CONNECTION_SHUTDOWN_FLAG_NONE, errorCode);
            }

            return valueTask;
        }

        private int HandleEventConnected(ref QUIC_CONNECTION_EVENT.CONNECTED_DATA data)
        {
            _connectedTcs.TrySetResult();
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventShutdownInitiatedByTransport(ref QUIC_CONNECTION_EVENT.SHUTDOWN_INITIATED_BY_TRANSPORT_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventShutdownInitiatedByPeer(ref QUIC_CONNECTION_EVENT.SHUTDOWN_INITIATED_BY_PEER_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventShutdownComplete()
        {
            Exception exception = null;
            if (_disposed > 0)
            {
                exception = new ObjectDisposedException(GetType().FullName).GetBaseException();
            }
            else
            {
                exception = new Exception();
            }
            _connectionCloseTcs.TrySetException(exception);
            _acceptQueue.Writer.TryComplete(exception);
            _connectedTcs.TrySetException(exception);
            _shutdownTokenSource.Cancel();
            _shutdownTcs.TrySetResult(final: true);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }
        
        private int HandleEventLocalAddressChanged(ref QUIC_CONNECTION_EVENT.LOCAL_ADDRESS_CHANGED_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventPeerAddressChanged(ref QUIC_CONNECTION_EVENT.PEER_ADDRESS_CHANGED_DATA data)
        {
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventPeerStreamStarted(ref QUIC_CONNECTION_EVENT.PEER_STREAM_STARTED_DATA data)
        {
            QuicStream stream = new QuicStream(this, data.Stream, data.Flags);
            if (!_acceptQueue.Writer.TryWrite(stream))
            {
                return MSQuicFunc.QUIC_STATUS_SUCCESS;
            }
            
            data.Flags |= QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_DELAY_ID_FC_UPDATES;
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventStreamsAvailable(ref QUIC_CONNECTION_EVENT.STREAMS_AVAILABLE_DATA data)
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
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventPeerCertificateReceived(ref QUIC_CONNECTION_EVENT.PEER_CERTIFICATE_RECEIVED_DATA data)
        {
            var task = _sslConnectionOptions.StartAsyncCertificateValidation(data.Certificate, data.Chain);
            if (task.IsCompletedSuccessfully)
            {
                return task.Result ? MSQuicFunc.QUIC_STATUS_SUCCESS : MSQuicFunc.QUIC_STATUS_BAD_CERTIFICATE;
            }
            return MSQuicFunc.QUIC_STATUS_PENDING;
        }

        private int HandleConnectionEvent(ref QUIC_CONNECTION_EVENT connectionEvent)
        {
            NetLog.Log("Connection Event: " + connectionEvent.Type.ToString());
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
        
        private static int NativeCallback(QUIC_CONNECTION connection, object context, ref QUIC_CONNECTION_EVENT connectionEvent)
        {
            var _handle = context as QuicConnection;
            return _handle.HandleConnectionEvent(ref connectionEvent);
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            if (_shutdownTcs.TryGetValueTask(out ValueTask valueTask, this))
            {
                MSQuicFunc.MsQuicConnectionShutdown(_handle, QUIC_CONNECTION_SHUTDOWN_FLAGS.QUIC_CONNECTION_SHUTDOWN_FLAG_NONE, 1);
            }
            else if (!valueTask.IsCompletedSuccessfully)
            {
                MSQuicFunc.MsQuicConnectionShutdown(_handle, QUIC_CONNECTION_SHUTDOWN_FLAGS.QUIC_CONNECTION_SHUTDOWN_FLAG_SILENT, 1);
            }
            
            await _shutdownTcs.GetFinalTask(this).ConfigureAwait(false);
            Debug.Assert(_connectedTcs.IsCompleted);
            _acceptQueue.Writer.TryComplete(new Exception());
            while (_acceptQueue.Reader.TryRead(out QuicStream? stream))
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }

    }
}
