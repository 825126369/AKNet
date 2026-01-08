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

namespace AKNet.Quic.Server
{
    internal partial class ClientPeer
    {
		public void HandleConnectedSocket(QuicConnection connection)
		{
			MainThreadCheck.Check();

			this.mQuicConnection = connection;
			SetSocketState(SOCKET_PEER_STATE.CONNECTED);

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
            mSendQuicStream = await mQuicConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);

            try
			{
				while (mQuicConnection != null)
				{
					QuicStream mQuicStream = await mQuicConnection.AcceptInboundStreamAsync();
                    StartProcessStreamReceive(mQuicStream);
                }
			}
			catch (Exception e)
			{
				NetLog.LogError(e.ToString());
				SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
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
                        int nLength = await mQuicStream.ReadAsync(mReceiveBuffer);
                        if (nLength > 0)
                        {
                            MultiThreadingReceiveSocketStream(mReceiveBuffer.Span.Slice(0, nLength));
                        }
                        else
                        {
                            NetLog.Log($"mQuicStream.ReadAsync Length: {nLength}");
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
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
                    await mSendQuicStream.WriteAsync(mSendBuffer.Slice(0, nLength));
                }
                bSendIOContextUsed = false;
            }
            catch (Exception e)
            {
                //NetLog.LogError(e.ToString());
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
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
    }

}