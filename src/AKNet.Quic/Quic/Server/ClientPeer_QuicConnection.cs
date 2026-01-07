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
            for (int i = 0; i < 1; i++)
            {
                var mStream = await mQuicConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
                ClientPeerQuicStream mStreamObj = new ClientPeerQuicStream(this, mStream);
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
				SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
			}
        }

        public ClientPeerQuicStream FindStreamObj(QuicStream mQuicStream)
        {
            for (int i = 0; i < mStreamList.Count; i++)
            {
                var mStream = mStreamList[i];
                if (mStream.GetStreamId() == mQuicStream.Id)
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

        private async void CloseSocket()
		{
			if (mQuicConnection != null)
			{
                foreach (var v in mStreamList)
                {
                    v.Dispose();
                }
                mStreamList.Clear();

                var mQuicConnection2 = mQuicConnection;
				mQuicConnection = null;
				await mQuicConnection2.CloseAsync(0);
			}
		}
    }

}