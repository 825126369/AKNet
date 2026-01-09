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

namespace AKNet.Quic.Server
{
    internal partial class ClientPeer
    {
		public void HandleConnectedSocket(QuicConnection connection)
		{
			MainThreadCheck.Check();
			this.mQuicConnection = connection;
            this.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
            this.StartProcessReceive();
		}

        public IPEndPoint GetIPEndPoint()
        {
            if (mQuicConnection != null)
            {
                return mQuicConnection.RemoteEndPoint;
            }
            return null;
        }

        private async void StartProcessReceive()
		{
            try
			{
				while (mQuicConnection != null)
				{
					QuicStream mQuicStream = await mQuicConnection.AcceptInboundStreamAsync().ConfigureAwait(false);
                    var mStreamHandle = new ClientPeerQuicStream(this.mServerMgr, this, mQuicStream);
                    mStreamHandle.StartProcessStreamReceive();
                    lock (mPendingAcceptStreamQueue)
                    {
                        mPendingAcceptStreamQueue.Enqueue(mStreamHandle);
                    }
                }
			}
			catch (Exception e)
			{
				//NetLog.LogError(e.ToString());
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
                    mStreamHandle = new ClientPeerQuicStream(this.mServerMgr, this, nStreamIndex);
                    mSendStreamEnumDic.Add(nStreamIndex, mStreamHandle);
                }
            }
            return mStreamHandle;
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
                    while (mPendingAcceptStreamQueue.TryDequeue(out var v))
                    {
                        
                    }
                }

                mAcceptStreamDic.Clear();
                
                var mQuicConnection2 = mQuicConnection;
                mQuicConnection = null;
                await mQuicConnection2.CloseAsync(Config.DefaultCloseErrorCode).ConfigureAwait(false);
            }
		}
    }

}