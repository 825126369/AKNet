using System;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    internal class ClientPeerSocketMgr
	{
		// 端口号 无法 只对应一个Socket, 所以得自己 分配一个 唯一Id
		private readonly uint nSocketPeerId = 0;

		private SocketAsyncEventArgs receiveIOContext = null;
		private SocketAsyncEventArgs sendIOContext = null;
		private bool bReceiveIOContextUsed = false;
		private bool bSendIOContextUsed = false;
		private CircularBuffer<byte> mSendStreamList = null;

		private Socket mSocket = null;
		private object lock_mSocket_object = new object();

		private ClientPeer mClientPeer;

		public ClientPeerSocketMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
			mSendStreamList = new CircularBuffer<byte>(Config.nIOContexBufferLength);
			receiveIOContext = ServerGlobalVariable.Instance.mReadWriteIOContextPool.Pop();
			sendIOContext = ServerGlobalVariable.Instance.mReadWriteIOContextPool.Pop();
			receiveIOContext.Completed += OnIOCompleted;
			sendIOContext.Completed += OnIOCompleted;
			bReceiveIOContextUsed = false;
			bSendIOContextUsed = false;

			nSocketPeerId = ServerGlobalVariable.Instance.mClientIdManager.Pop();

			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		public void ConnectClient(Socket mAcceptSocket)
		{
			lock (lock_mSocket_object)
			{
#if DEBUG
				NetLog.Assert(mAcceptSocket != null);
				NetLog.Assert(this.mSocket == null);
				NetLog.Assert(nSocketPeerId > 0);
#endif
				this.mSocket = mAcceptSocket;

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
#if DEBUG
			NetLog.Assert(mBufferSegment.Length <= Config.nMsgPackageBufferMaxLength, "发送尺寸超出最大限制: " + mBufferSegment.Length + " | " + Config.nMsgPackageBufferMaxLength);
#endif

			lock (mSendStreamList)
			{
				if (!mSendStreamList.isCanWriteFrom(mBufferSegment.Length))
				{
					CircularBuffer<byte> mOldBuffer = mSendStreamList;

					int newSize = mOldBuffer.Capacity * 2;
					while (newSize < mOldBuffer.Length + mBufferSegment.Length)
					{
						newSize *= 2;
					}

					mSendStreamList = new CircularBuffer<byte>(newSize);
					mSendStreamList.WriteFrom(mOldBuffer, mOldBuffer.Length);

					NetLog.LogWarning("mSendStreamList Size: " + mSendStreamList.Capacity);
				}

				mSendStreamList.WriteFrom(mBufferSegment);
			}

			if (!bSendIOContextUsed)
			{
				bSendIOContextUsed = true;
				SendNetStream1(sendIOContext);
			}
		}

		private void SendNetStream1(SocketAsyncEventArgs e)
		{
			bool bContinueSend = false;
			lock (mSendStreamList)
			{
				if (mSendStreamList.Length >= Config.nIOContexBufferLength)
				{
					int nLength = Config.nIOContexBufferLength;
					mSendStreamList.WriteTo(0, e.Buffer, e.Offset, nLength);
					e.SetBuffer(e.Offset, nLength);
					bContinueSend = true;
				}
				else if (mSendStreamList.Length > 0)
				{
					int nLength = mSendStreamList.Length;
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
			lock(mSendStreamList)
            {
				mSendStreamList.reset();
            }

			CloseSocket();
#if DEBUG
			NetLog.Assert(this.mSocket == null);
#endif
		}
	}

}