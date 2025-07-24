/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2MSQuic.Common;
using System;
using System.Net;
using System.Threading;

namespace AKNet.Udp2MSQuic.Server
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
        private ClientPeer mClientPeer;
		private QuicServer mQuicServer;
		
		public ClientPeerSocketMgr(ClientPeer mClientPeer, QuicServer mQuicServer)
		{
			this.mClientPeer = mClientPeer;
			this.mQuicServer = mQuicServer;
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		public void HandleConnectedSocket(QuicConnection connection)
		{
			MainThreadCheck.Check();
			this.mQuicConnection = connection;
            this.mQuicConnection.mOption.ReceiveStreamDataFunc = ReceiveStreamDataFunc;
            this.mQuicConnection.mOption.SendFinishFunc = SendFinishFunc;
            this.mQuicConnection.RequestReceiveStreamData();
            mSendQuicStream = mQuicConnection.OpenSendStream(QuicStreamType.Unidirectional);

            this.mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
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
        
        private void ReceiveStreamDataFunc(QuicStream mQuicStream)
        {
            if (mQuicStream != null)
            {
                do
                {
                    int nLength = mQuicStream.Read(mReceiveBuffer);
                    if (nLength > 0)
                    {
                        mClientPeer.mMsgReceiveMgr.MultiThreadingReceiveSocketStream(mReceiveBuffer.Span.Slice(0, nLength));
                    }
                    else
                    {
                        break;
                    }
                } while (true);
            }
        }

        private void SendFinishFunc(QuicStream mQuicStream)
        {
            if (mQuicStream == mSendQuicStream)
            {
                SendNetStream2();
            }
            else
            {
                NetLog.LogError("SendFinishFunc Error");
            }
        }

        public void SendNetStream(ReadOnlyMemory<byte> mBufferSegment)
        {
            lock (mSendStreamList)
            {
                mSendStreamList.WriteFrom(mBufferSegment.Span);
            }

            if (!bSendIOContextUsed)
            {
                bSendIOContextUsed = true;
                SendNetStream2();
            }
        }

        private void SendNetStream2()
        {
            try
            {
                if (mSendStreamList.Length > 0)
                {
                    int nLength = 0;
                    lock (mSendStreamList)
                    {
                        nLength = mSendStreamList.WriteToMax(0, mSendBuffer.Span);
                    }
                    mSendQuicStream.Send(mSendBuffer.Slice(0, nLength));
                }
                else
                {
                    bSendIOContextUsed = false;
                }
            }
            catch (Exception e)
            {
                //NetLog.LogError(e.ToString());
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
        }

        private void CloseSocket()
		{
			if (mQuicConnection != null)
			{
				var mQuicConnection2 = mQuicConnection;
				mQuicConnection = null;
				mQuicConnection2.StartClose();
			}
		}

		public void Reset()
		{
			CloseSocket();
			lock (mSendStreamList)
			{
				mSendStreamList.reset();
			}
		}
	}

}