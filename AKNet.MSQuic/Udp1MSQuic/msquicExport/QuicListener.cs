/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.Udp1MSQuic.Common
{
    internal sealed class QuicListener
    {
        private QUIC_LISTENER _handle = null;
        public QuicListenerOptions mOption;
        public IPEndPoint LocalEndPoint;
        
        private void Init(QUIC_LISTENER _handle, QuicListenerOptions options)
        {
            this._handle = _handle;
            this.mOption = options;
            this.LocalEndPoint = options.ListenEndPoint;
        }

        private static QuicListener Create(QuicListenerOptions options)
        {
            QuicListener mListenerer = new QuicListener();
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
            QUIC_ADDR address = new QUIC_ADDR(options.ListenEndPoint);

#if USE_MSQUIC_2
            if (options.ListenEndPoint.Address.Equals(IPAddress.IPv6Any))
            {
                address.Family = System.Net.Sockets.AddressFamily.Unspecified;
            }
#endif

            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicListenerStart(_handle, mAlpnList.ToArray(), mAlpnList.Count, address)))
            {
                NetLog.LogError("ListenerStart failed");
                return null;
            }
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

        private int HandleEventNewConnection(ref QUIC_LISTENER_EVENT.NEW_CONNECTION_DATA data)
        {
            QuicConnectionOptions options = mOption.GetConnectionOptionFunc();
            QuicConnection connection = new QuicConnection(this, data.Connection, data.Info, options);
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

