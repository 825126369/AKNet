/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Server
{
    internal class ClientPeerSocketMgr
	{
		// 端口号 无法 只对应一个Socket, 所以得自己 分配一个 唯一Id
		private readonly uint nSocketPeerId = 0;

		private SocketAsyncEventArgs receiveIOContext = null;
		private SocketAsyncEventArgs sendIOContext = null;
		private bool bReceiveIOContextUsed = false;
		private bool bSendIOContextUsed = false;
		private readonly CircularBuffer<byte> mSendStreamList = null;

		private Socket mSocket = null;
		private readonly object lock_mSocket_object = new object();
        private readonly object lock_mSendStreamList_object = new object();

        private ClientPeer mClientPeer;
		private TcpServer mTcpServer;
		
		public ClientPeerSocketMgr(ClientPeer mClientPeer, TcpServer mTcpServer)
		{
			this.mClientPeer = mClientPeer;
			this.mTcpServer = mTcpServer;

			mSendStreamList = new CircularBuffer<byte>(Config.nIOContexBufferLength);
			receiveIOContext = mTcpServer.mReadWriteIOContextPool.Pop();
			sendIOContext = mTcpServer.mReadWriteIOContextPool.Pop();
			receiveIOContext.Completed += OnIOCompleted;
			sendIOContext.Completed += OnIOCompleted;
			bReceiveIOContextUsed = false;
			bSendIOContextUsed = false;

			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		public void HandleConnectedSocket(Socket otherSocket)
		{
			lock (lock_mSocket_object)
			{
				this.mSocket = otherSocket;

				mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
				if (!bReceiveIOContextUsed)
				{
					bReceiveIOContextUsed = true;
					if (!mSocket.ReceiveAsync(receiveIOContext))
					{
						this.ProcessReceive(receiveIOContext);
					}
				}
			}
		}

        public IPEndPoint GetIPEndPoint()
        {
            if (mSocket != null && mSocket.RemoteEndPoint != null)
            {
                IPEndPoint mRemoteEndPoint = mSocket.RemoteEndPoint as IPEndPoint;
                return mRemoteEndPoint;
            }
            else
            {
                return null;
            }
        }

        public uint GetUUID()
		{
			return nSocketPeerId;
		}

		private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Receive:
					this.ProcessReceive(e);
					break;
				case SocketAsyncOperation.Send:
					this.ProcessSend(e);
					break;
				default:
					throw new ArgumentException("The last operation completed on the socket was not a receive or send");
			}
		}

		private void ProcessReceive(SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				if (e.BytesTransferred > 0)
				{
					ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(e.Buffer, e.Offset, e.BytesTransferred);
					mClientPeer.mMsgReceiveMgr.ReceiveSocketStream(readOnlySpan);

					lock (lock_mSocket_object)
					{
						if (mSocket != null)
						{
							if (!mSocket.ReceiveAsync(e))
							{
								this.ProcessReceive(e);
							}
						}
					}
				}
				else
				{
					DisConnected();
					bReceiveIOContextUsed = false;
				}
			}
			else
			{
				DisConnectedWithException(e.SocketError);
				bReceiveIOContextUsed = false;
			}
		}

		private void ProcessSend(SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				SendNetStream1(e);
			}
			else
			{
				DisConnectedWithException(e.SocketError);
				bSendIOContextUsed = false;
			}
		}

		public void SendNetStream(ReadOnlySpan<byte> mBufferSegment)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(mBufferSegment.Length <= Config.nMsgPackageBufferMaxLength, "发送尺寸超出最大限制: " + mBufferSegment.Length + " | " + Config.nMsgPackageBufferMaxLength);

				lock (lock_mSendStreamList_object)
				{
					mSendStreamList.WriteFrom(mBufferSegment);
				}

				if (!bSendIOContextUsed)
				{
					bSendIOContextUsed = true;
					SendNetStream1(sendIOContext);
				}
			}
		}

		private void SendNetStream1(SocketAsyncEventArgs e)
		{
			bool bContinueSend = false;
			lock (lock_mSendStreamList_object)
			{
				int nLength = mSendStreamList.Length;
				if (nLength > 0)
				{
					if (nLength >= Config.nIOContexBufferLength)
					{
						nLength = Config.nIOContexBufferLength;
					}

					mSendStreamList.WriteTo(0, e.Buffer, e.Offset, nLength);
					e.SetBuffer(e.Offset, nLength);
					bContinueSend = true;
				}
			}

			if (bContinueSend)
			{
				lock (lock_mSocket_object)
				{
					if (mSocket != null)
					{
						if (!mSocket.SendAsync(e))
						{
							ProcessSend(e);
						}
					}
					else
                    {
						bSendIOContextUsed = false;
					}
				}
			}
			else
			{
				bSendIOContextUsed = false;
			}
		}

		private void DisConnected()
		{
#if DEBUG
            NetLog.Log("正常断开连接");
#endif
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

		private void DisConnectedWithException(SocketError mError)
		{
#if DEBUG
            //有可能客户端主动关闭与服务器的链接了
            //NetLog.LogError("异常断开连接: " + mError);
#endif
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

		void CloseSocket()
		{
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);

			lock (lock_mSocket_object)
			{
				if (mSocket != null)
				{
					try
					{
						mSocket.Shutdown(SocketShutdown.Both);
					}
					catch (Exception ex)
					{
						//NetLog.LogError("Error shutting down socket: " + ex.Message);
					}
					finally
					{
						mSocket.Close();
					}

					mSocket = null;
				}
			}
		}
		
		public void Reset()
		{
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            CloseSocket();

            lock (lock_mSendStreamList_object)
            {
				mSendStreamList.reset();
            }
		}
	}

}