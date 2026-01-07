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
			this.ServerIp = ServerAddr;
			this.nServerPort = ServerPort;

			SetSocketState(SOCKET_PEER_STATE.CONNECTING);
			NetLog.Log("Client 正在连接服务器: " + this.ServerIp + " | " + this.nServerPort);

            CloseSocket();

            if (!QuicConnection.IsSupported)
			{
				NetLog.LogError("QUIC is not supported.");
				return;
			}

			if (mIPEndPoint == null)
			{
				IPAddress mIPAddress = IPAddress.Parse(ServerAddr);
				mIPEndPoint = new IPEndPoint(mIPAddress, ServerPort);
			}

            try
            {
                mQuicConnection = await QuicConnection.ConnectAsync(GetQuicClientConnectionOptions(mIPEndPoint));
                SetSocketState(SOCKET_PEER_STATE.CONNECTED);

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
            mOption.DefaultCloseErrorCode = 0;
            mOption.DefaultStreamErrorCode = 0;
            mOption.MaxInboundBidirectionalStreams = 1;
            mOption.MaxInboundUnidirectionalStreams = 1;
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

        public async void StartManyStream(int nStreamCount)
        {
            for (int i = 0; i < nStreamCount; i++)
            {
                var mStream = await mQuicConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                ClientPeerQuicStream mStreamObj = new ClientPeerQuicStream(this, mStream);
                mStreamObj.Init();
                mStreamList.Add(mStreamObj);
            }

            try
            {
                while (mQuicConnection != null)
                {
                    QuicStream mQuicStream = await mQuicConnection.AcceptInboundStreamAsync();
                    var mStreamObj = FindStreamObj(mQuicStream);
                    await mStreamObj.StartProcessStreamReceive();
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                this.mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
        }

        public ClientPeerQuicStream FindStreamObj(QuicStream mQuicStream)
        {
            for (int i = 0; i < mStreamList.Count; i++)
            {
                var mStream = mStreamList[i];
                if(mStream.GetStreamId() == mQuicStream.Id)
                {
                    return mStream;
                }
            }

            return null;
        }

        public void SendNetStream(int nStreamIndex, ReadOnlySpan<byte> mBufferSegment)
        {
            var mStreamObj = mStreamList[nStreamIndex];
            mStreamObj.SendNetStream(mBufferSegment);
        }

		public IPEndPoint GetIPEndPoint()
		{
            IPEndPoint mRemoteEndPoint = null;
            try
            {
                if (mQuicConnection != null && mQuicConnection.RemoteEndPoint != null)
                {
                    mRemoteEndPoint = mQuicConnection.RemoteEndPoint as IPEndPoint;
                }
            }
            catch { }

            return mRemoteEndPoint;
        }

		private async void CloseSocket()
		{
            if (mQuicConnection != null)
            {
                QuicConnection mQuicConnection2 = mQuicConnection;
                mQuicConnection = null;
				await mQuicConnection2.CloseAsync(0);
            }
        }
    }
}
