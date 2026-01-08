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
using AKNet.MSQuic.Common;
using System;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;

namespace AKNet.MSQuic.Client
{
    internal class QuicConnectionMgr
	{
        private readonly Memory<byte> mReceiveBuffer = new byte[1024];
        private readonly Memory<byte> mSendBuffer = new byte[1024];

        private readonly AkCircularBuffer mSendStreamList = new AkCircularBuffer();
        private bool bSendIOContextUsed = false;

        private QuicConnection mQuicConnection = null;
		private string ServerIp = "";
		private int nServerPort = 0;
		private IPEndPoint mIPEndPoint = null;
        private ClientPeer mClientPeer;
        private QuicStream mSendQuicStream;

        public QuicConnectionMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.NONE);
        }

		public void ReConnectServer()
		{
            ConnectServer(this.ServerIp, this.nServerPort);
        }

        public async void ConnectServer(string ServerAddr, int ServerPort)
        {
            this.ServerIp = ServerAddr;
            this.nServerPort = ServerPort;

            mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTING);
            NetLog.Log("Client 正在连接服务器: " + this.ServerIp + " | " + this.nServerPort);

            Reset();

            IPAddress mIPAddress = IPAddress.Parse(ServerAddr);
            mIPEndPoint = new IPEndPoint(mIPAddress, ServerPort);

            try
            {
                mQuicConnection = await QuicConnection.ConnectAsync(GetQuicClientConnectionOptions(mIPEndPoint));
                //mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
                NetLog.Log("Client 连接服务器成功: " + this.ServerIp + " | " + this.nServerPort);
                StartProcessReceive();
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
            }
        }

        private QuicConnectionOptions GetQuicClientConnectionOptions(IPEndPoint mIPEndPoint)
        {
            QuicConnectionOptions mOption = new QuicConnectionOptions();
            mOption.RemoteEndPoint = mIPEndPoint;
            mOption.ClientAuthenticationOptions = new SslClientAuthenticationOptions();
            mOption.ClientAuthenticationOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            return mOption;
        }

        public bool DisConnectServer()
		{
			MainThreadCheck.Check();
            DisConnectServer2();
            return true;
		}

        private async Task DisConnectServer2()
        {
            NetLog.Log("客户端 主动 断开服务器 Begin......");
            await mQuicConnection.CloseAsync(0);
            NetLog.Log("客户端 主动 断开服务器 Finish......");
        }

        private async void StartProcessReceive()
        {
            mSendQuicStream = await mQuicConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional).ConfigureAwait(false);
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);

            try
            {
                while (mQuicConnection != null)
                {
                    QuicStream mQuicStream = await mQuicConnection.AcceptInboundStreamAsync().ConfigureAwait(false);
                    StartProcessStreamReceive(mQuicStream);
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                this.mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
        }

        private async void StartProcessStreamReceive(QuicStream mQuicStream)
        {
            try
            {
                if (mQuicStream != null)
                {
                    while (true)
                    {
                        int nLength = await mQuicStream.ReadAsync(mReceiveBuffer).ConfigureAwait(false);
                        if (nLength > 0)
                        {
                            mClientPeer.mMsgReceiveMgr.MultiThreadingReceiveSocketStream(mReceiveBuffer.Span.Slice(0, nLength));
                        }
                        else
                        {
                            NetLog.LogError($"mQuicStream.ReadAsync Length: {nLength}");
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                this.mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
        }

        public void SendNetStream(ReadOnlySpan<byte> mBufferSegment)
        {
            lock (mSendStreamList)
            {
                mSendStreamList.WriteFrom(mBufferSegment);
            }

            if (!bSendIOContextUsed)
            {
                bSendIOContextUsed = true;
                SendNetStream2();
            }
        }

        private async Task SendNetStream2()
        {
            try
            {
                while (mSendStreamList.Length > 0)
                {
                    int nLength = 0;
                    lock (mSendStreamList)
                    {
                        nLength = mSendStreamList.WriteTo(mSendBuffer.Span);
                    }
                    await mSendQuicStream.WriteAsync(mSendBuffer.Slice(0, nLength)).ConfigureAwait(false);
                }
                bSendIOContextUsed = false;
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
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

		public void Reset()
		{
            CloseSocket();
        }

		public void Release()
		{
            CloseSocket();
        }
    }
}
