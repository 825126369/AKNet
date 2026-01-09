/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Quic.Common;
using System.Net;
using System.Net.Quic;
using System.Net.Security;

namespace AKNet.Quic.Client
{
    internal partial class ClientPeer
    {
		public void ReConnectServer()
		{
            ConnectServer(this.ServerIp, this.nServerPort);
        }

		public async void ConnectServer(string ServerAddr, int ServerPort)
		{
            if (!QuicConnection.IsSupported)
            {
                throw new NotSupportedException("QUIC is not supported.");
            }

            this.ServerIp = ServerAddr;
            this.nServerPort = ServerPort;

            CloseSocket();
            SetSocketState(SOCKET_PEER_STATE.CONNECTING);
            NetLog.Log("Client 正在连接服务器: " + this.ServerIp + " | " + this.nServerPort);

            if (mIPEndPoint == null)
			{
				IPAddress mIPAddress = IPAddress.Parse(ServerAddr);
				mIPEndPoint = new IPEndPoint(mIPAddress, ServerPort);
			}

            try
            {
                mQuicConnection = await QuicConnection.ConnectAsync(GetQuicClientConnectionOptions(mIPEndPoint)).ConfigureAwait(false);
                SetSocketState(SOCKET_PEER_STATE.CONNECTED);
                StartProcessReceive();
                NetLog.Log("Client 连接服务器成功: " + this.ServerIp + " | " + this.nServerPort);
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
            }
		}

        private QuicClientConnectionOptions GetQuicClientConnectionOptions(IPEndPoint mIPEndPoint)
        {
            var mCert = X509CertTool.GetCert();
            NetLog.Assert(mCert != null, "GetCert() == null");

            var ApplicationProtocols = new List<SslApplicationProtocol>();
            ApplicationProtocols.Add(SslApplicationProtocol.Http3);

            QuicClientConnectionOptions mOption = new QuicClientConnectionOptions();
            mOption.RemoteEndPoint = mIPEndPoint;
            mOption.DefaultCloseErrorCode = Config.DefaultCloseErrorCode;
            mOption.DefaultStreamErrorCode = Config.DefaultStreamErrorCode;
            mOption.MaxInboundBidirectionalStreams = byte.MaxValue;
            mOption.MaxInboundUnidirectionalStreams = byte.MaxValue;
            mOption.ClientAuthenticationOptions = new SslClientAuthenticationOptions();
            mOption.ClientAuthenticationOptions.ApplicationProtocols = ApplicationProtocols;
            mOption.ClientAuthenticationOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            return mOption;
        }

        public bool DisConnectServer()
		{
			MainThreadCheck.Check();
            DisConnectServer2();
            return true;
		}

        private async void DisConnectServer2()
        {
            NetLog.Log("客户端 主动 断开服务器 Begin......");
            await mQuicConnection.CloseAsync(0);
            NetLog.Log("客户端 主动 断开服务器 Finish......");
        }

        public async void StartProcessReceive()
        {
            try
            {
                while (mQuicConnection != null)
                {
                    QuicStream mQuicStream = await mQuicConnection.AcceptInboundStreamAsync().ConfigureAwait(false);
                    var mStreamHandle = FindAcceptStreamHandle(mQuicStream);
                    mStreamHandle.StartProcessStreamReceive();
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
        }

        private ClientPeerQuicStream FindAcceptStreamHandle(QuicStream mQuicStream)
        {
            ClientPeerQuicStream mStreamHandle = null;
            lock (mAcceptStreamDic)
            {
                if (!mAcceptStreamDic.TryGetValue(mQuicStream.Id, out mStreamHandle))
                {
                    mStreamHandle = new ClientPeerQuicStream(this, mQuicStream);
                    mAcceptStreamDic.Add(mQuicStream.Id, mStreamHandle);
                }
            }
            return mStreamHandle;
        }

        private ClientPeerQuicStream GetOrCreateSendStreamHandle(byte nStreamIndex)
        {
            ClientPeerQuicStream mStreamHandle = null;
            lock (mSendStreamEnumDic)
            {
                if (!mSendStreamEnumDic.TryGetValue(nStreamIndex, out mStreamHandle))
                {
                    mStreamHandle = new ClientPeerQuicStream(this, nStreamIndex);
                    mSendStreamEnumDic.Add(nStreamIndex, mStreamHandle);
                }
            }
            return mStreamHandle;
        }

        public IPEndPoint GetIPEndPoint()
        {
            if (mQuicConnection != null)
            {
                return mQuicConnection.RemoteEndPoint;
            }

            return null;
        }

		private async void CloseSocket()
		{
            if (mQuicConnection != null)
            {
                lock (mSendStreamEnumDic)
                {
                    foreach (var v in mSendStreamEnumDic)
                    {
                        v.Value.Dispose();
                    }
                    mSendStreamEnumDic.Clear();
                }

                lock (mAcceptStreamDic)
                {
                    foreach (var v in mAcceptStreamDic)
                    {
                        v.Value.Dispose();
                    }
                    mAcceptStreamDic.Clear();
                }
                
                QuicConnection mQuicConnection2 = mQuicConnection;
                mQuicConnection = null;
				await mQuicConnection2.CloseAsync(Config.DefaultCloseErrorCode).ConfigureAwait(false);
            }
        }
    }
}
