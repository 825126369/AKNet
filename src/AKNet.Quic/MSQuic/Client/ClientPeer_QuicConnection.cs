/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:02
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.MSQuic.Common;
using System.Net;
using System.Net.Security;

namespace AKNet.MSQuic.Client
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

        private QuicConnectionOptions GetQuicClientConnectionOptions(IPEndPoint mIPEndPoint)
        {
            var mCert = X509CertTool.GetCert();
            NetLog.Assert(mCert != null, "GetCert() == null");

            var ApplicationProtocols = new List<SslApplicationProtocol>();
            ApplicationProtocols.Add(SslApplicationProtocol.Http11);
            ApplicationProtocols.Add(SslApplicationProtocol.Http2);
            ApplicationProtocols.Add(SslApplicationProtocol.Http3);

            QuicConnectionOptions mOption = new QuicConnectionOptions();
            mOption.RemoteEndPoint = mIPEndPoint;
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
            await mQuicConnection.CloseAsync();
            NetLog.Log("客户端 主动 断开服务器 Finish......");
        }

        public async void StartProcessReceive()
        {
            try
            {
                while (mQuicConnection != null)
                {
                    QuicStream mQuicStream = await mQuicConnection.AcceptInboundStreamAsync().ConfigureAwait(false);
                    var mStreamHandle = new ClientPeerQuicStream(this, mQuicStream);
                    mStreamHandle.StartProcessStreamReceive();
                    lock (mPendingAcceptStreamQueue)
                    {
                        mPendingAcceptStreamQueue.Enqueue(mStreamHandle);
                    }
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
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
                    mSendStreamEnumDic.Clear();
                }

                lock (mPendingAcceptStreamQueue)
                {
                    while(mPendingAcceptStreamQueue.TryDequeue(out var v))
                    {
                       
                    }
                }

                mAcceptStreamDic.Clear();
                
                QuicConnection mQuicConnection2 = mQuicConnection;
                mQuicConnection = null;
				await mQuicConnection2.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
