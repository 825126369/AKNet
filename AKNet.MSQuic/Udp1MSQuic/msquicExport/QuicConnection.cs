using AKNet.Common;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.Udp1MSQuic.Common
{
    internal partial class QuicConnection
    {
        public readonly QUIC_CONNECTION _handle;
        public SslConnectionOptions _sslConnectionOptions;
        private QUIC_CONFIGURATION _configuration;
        public readonly QuicConnectionOptions mOption;
        public readonly ConcurrentQueue<QuicStream> mReceiveStreamDataQueue = new ConcurrentQueue<QuicStream>();
        public readonly EndPoint RemoteEndPoint;
        private QuicListener mQuicListener;

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
        
        public QuicConnection(QuicListener mQuicListener, QUIC_CONNECTION handle, QUIC_NEW_CONNECTION_INFO info, QuicConnectionOptions mOption)
        {
            this.mQuicListener = mQuicListener;
            this.mOption = mOption;
            this._handle = handle;
            this.RemoteEndPoint = info.RemoteAddress.GetIPEndPoint();
            MSQuicFunc.MsQuicSetCallbackHandler_For_QUIC_CONNECTION(handle, NativeCallback, this);
        }

        public static QuicConnection StartConnect(QuicConnectionOptions mOption)
        {
            QuicConnection connection = new QuicConnection(mOption);
            Task.Run(() =>
            {
                connection.StartConnectAsync();
            });
            return connection;
        }

        private async void StartConnectAsync()
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
        }
        
        public QuicStream OpenSendStream(QuicStreamType nType)
        {
            QuicStream stream = new QuicStream(this, nType);
            stream.Start();
            return stream;
        }

        public void RequestReceiveStreamData()
        {
            foreach (var v in mReceiveStreamDataQueue)
            {
                if (v.orHaveReceiveData())
                {
                    mOption.ReceiveStreamDataFunc(v);
                }
            }
        }

        public void StartClose()
        {
            CloseAsync();
        }

        public async Task CloseAsync()
        {
            await Task.CompletedTask;
            MSQuicFunc.MsQuicConnectionShutdown(_handle, QUIC_CONNECTION_SHUTDOWN_FLAGS.QUIC_CONNECTION_SHUTDOWN_FLAG_NONE, 0);
            mOption.CloseFinishFunc?.Invoke();
        }

        private int HandleEventConnected(ref QUIC_CONNECTION_EVENT.CONNECTED_DATA data)
        {
            mOption.ConnectFinishFunc?.Invoke();
            if (mQuicListener != null)
            {
                mQuicListener.mOption.AcceptConnectionFunc(this);
            }
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
            data.Flags |= QUIC_STREAM_OPEN_FLAGS.QUIC_STREAM_OPEN_FLAG_DELAY_ID_FC_UPDATES;
            mReceiveStreamDataQueue.Enqueue(stream);
            return MSQuicFunc.QUIC_STATUS_SUCCESS;
        }

        private int HandleEventStreamsAvailable(ref QUIC_CONNECTION_EVENT.STREAMS_AVAILABLE_DATA data)
        {
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
    }
}
