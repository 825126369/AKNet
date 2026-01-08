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
            this.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
            this.StartProcessReceive();
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
            try
			{
				while (mQuicConnection != null)
				{
					QuicStream mQuicStream = await mQuicConnection.AcceptInboundStreamAsync();
                    var mStreamHandle = FindAcceptStreamHandle(mQuicStream);
                    await mStreamHandle.StartProcessStreamReceive();
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
            if (!mAcceptStreamDic.TryGetValue(mQuicStream.Id, out mStreamHandle))
            {
                mStreamHandle = new ClientPeerQuicStream(mServerMgr, this, mQuicStream);
                mAcceptStreamDic.Add(mQuicStream.Id, mStreamHandle);
            }
            return mStreamHandle;
        }

        public QuicStreamBase GetOrCreateSendStreamHandle(byte nStreamIndex)
        {
            ClientPeerQuicStream mStreamHandle = null;
            if (!mSendStreamEnumDic.TryGetValue(nStreamIndex, out mStreamHandle))
            {
                mStreamHandle = new ClientPeerQuicStream(mServerMgr, this, nStreamIndex);
                mSendStreamEnumDic.Add(nStreamIndex, mStreamHandle);
            }
            return mStreamHandle;
        }

        private async void CloseSocket()
		{
			if (mQuicConnection != null)
			{
                foreach (var v in mSendStreamEnumDic)
                {
                    v.Value.Dispose();
                }
                mSendStreamEnumDic.Clear();

                foreach (var v in mAcceptStreamDic)
                {
                    v.Value.Dispose();
                }
                mAcceptStreamDic.Clear();

                var mQuicConnection2 = mQuicConnection;
				mQuicConnection = null;
				await mQuicConnection2.CloseAsync(0);
			}
		}
    }

}