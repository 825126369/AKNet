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
using System.Threading;

namespace AKNet.MSQuic.Server
{
    internal class ClientPeerSocketMgr
	{
        private readonly Memory<byte> mReceiveBuffer = new byte[1024];
        private readonly Memory<byte> mSendBuffer = new byte[1024];
        CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();

        private readonly AkCircularBuffer mSendStreamList = new AkCircularBuffer();
        private bool bSendIOContextUsed = false;
        private QuicStream mSendQuicStream;

        private QuicConnection mQuicConnection;
        private ClientPeerPrivate mClientPeer;
		private QuicServer mQuicServer;
		
		public ClientPeerSocketMgr(ClientPeerPrivate mClientPeer, QuicServer mQuicServer)
		{
			this.mClientPeer = mClientPeer;
			this.mQuicServer = mQuicServer;
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		public void HandleConnectedSocket(QuicConnection connection)
		{
			MainThreadCheck.Check();
			this.mQuicConnection = connection;
            this.mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
            StartProcessReceive();
        }

        public IPEndPoint GetIPEndPoint()
        {
			IPEndPoint mRemoteEndPoint = null;
            if (mQuicConnection != null)
            {
                mRemoteEndPoint = mQuicConnection.RemoteEndPoint as IPEndPoint;
            }
            return mRemoteEndPoint;
        }

        private async void StartProcessReceive()
        {
            mSendQuicStream = await mQuicConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional).ConfigureAwait(false);
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

        private async void SendNetStream2()
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

        private async void CloseSocket()
		{
			if (mQuicConnection != null)
			{
				var mQuicConnection2 = mQuicConnection;
				mQuicConnection = null;
				await mQuicConnection2.CloseAsync(0);
			}
		}

		public void Reset()
		{
			CloseSocket();
			lock (mSendStreamList)
			{
				mSendStreamList.Reset();
			}
		}

        public void Release()
        {
            lock (mSendStreamList)
            {
                mSendStreamList.Dispose();
            }
            CloseSocket();
        }
    }

}