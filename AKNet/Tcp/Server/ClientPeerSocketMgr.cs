/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/7 21:38:42
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

		private SocketAsyncEventArgs mReceiveIOContex = null;
		private SocketAsyncEventArgs mSendIOContex = null;
		private bool bSendIOContextUsed = false;
		private readonly AkCircularBuffer<byte> mSendStreamList = null;

		private Socket mSocket = null;
		private readonly object lock_mSocket_object = new object();
        private readonly object lock_mSendStreamList_object = new object();

        private ClientPeer mClientPeer;
		private TcpServer mTcpServer;
		
		public ClientPeerSocketMgr(ClientPeer mClientPeer, TcpServer mTcpServer)
		{
			this.mClientPeer = mClientPeer;
			this.mTcpServer = mTcpServer;

			mSendStreamList = new AkCircularBuffer<byte>(Config.nIOContexBufferLength);
			mReceiveIOContex = mTcpServer.mReadWriteIOContextPool.Pop();
			mSendIOContex = mTcpServer.mReadWriteIOContextPool.Pop();
            if (!mTcpServer.mBufferManager.SetBuffer(mSendIOContex))
            {
                mSendIOContex.SetBuffer(new byte[Config.nIOContexBufferLength], 0, Config.nIOContexBufferLength);
            }
            if (!mTcpServer.mBufferManager.SetBuffer(mReceiveIOContex))
            {
                mReceiveIOContex.SetBuffer(new byte[Config.nIOContexBufferLength], 0, Config.nIOContexBufferLength);
            }

            mReceiveIOContex.Completed += OnIOCompleted;
			mSendIOContex.Completed += OnIOCompleted;
			bSendIOContextUsed = false;

			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		public void HandleConnectedSocket(Socket otherSocket)
		{

			this.mSocket = otherSocket;
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
			bSendIOContextUsed = false;

            StartReceiveEventArg();
		}

		private void StartReceiveEventArg()
		{
			bool bIOSyncCompleted = false;
			if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
				{
					if (mSocket != null)
					{
                        bIOSyncCompleted = !mSocket.ReceiveAsync(mReceiveIOContex);
                    }
				}
			}
			else
			{
				if (mSocket != null)
				{
					try
					{
						bIOSyncCompleted = !mSocket.ReceiveAsync(mReceiveIOContex);
					}
					catch (Exception e)
					{
						DisConnectedWithException(e);
					}
				}
			}

			if (bIOSyncCompleted)
			{
				this.ProcessReceive(mReceiveIOContex);
			}

		}

		private void StartSendEventArg()
		{
			bool bIOSyncCompleted = false;
			if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
				{
					if (mSocket != null)
					{
						bIOSyncCompleted = !mSocket.SendAsync(mSendIOContex);
					}
					else
					{
						bSendIOContextUsed = false;
					}
				}
			}
			else
			{
				if (mSocket != null)
				{
					try
					{
						bIOSyncCompleted = !mSocket.SendAsync(mSendIOContex);
					}
					catch (Exception e)
					{
						bSendIOContextUsed = false;
						DisConnectedWithException(e);
					}
				}
				else
				{
					bSendIOContextUsed = false;
				}
			}

			if (bIOSyncCompleted)
			{
				this.ProcessSend(mSendIOContex);
			}
		}

        public IPEndPoint GetIPEndPoint()
        {
			IPEndPoint mRemoteEndPoint = null;

            if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
				{
                    if (mSocket != null && mSocket.RemoteEndPoint != null)
                    {
                        mRemoteEndPoint = mSocket.RemoteEndPoint as IPEndPoint;
                    }
                }
			}
			else
			{
				try
				{
					if (mSocket != null && mSocket.RemoteEndPoint != null)
					{
						mRemoteEndPoint = mSocket.RemoteEndPoint as IPEndPoint;
					}
				}
				catch { }
			}

            return mRemoteEndPoint;
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
					mClientPeer.mMsgReceiveMgr.MultiThreadingReceiveSocketStream(readOnlySpan);
                    StartReceiveEventArg();
                }
				else
				{
					DisConnected();
				}
			}
			else
			{
                DisConnectedWithSocketError(e.SocketError);
			}
		}

		private void ProcessSend(SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				SendNetStream1();
			}
			else
			{
                DisConnectedWithSocketError(e.SocketError);
				bSendIOContextUsed = false;
			}
		}

		public void SendNetStream(ReadOnlySpan<byte> mBufferSegment)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
#if DEBUG
				if (mBufferSegment.Length > Config.nMsgPackageBufferMaxLength)
                {
                    NetLog.LogWarning("发送尺寸超出最大限制" + mBufferSegment.Length + " | " + Config.nMsgPackageBufferMaxLength);
                }
#endif

				lock (lock_mSendStreamList_object)
				{
					mSendStreamList.WriteFrom(mBufferSegment);
				}

				if (!bSendIOContextUsed)
				{
					bSendIOContextUsed = true;
					SendNetStream1();
				}
			}
		}

		private void SendNetStream1()
		{
			bool bContinueSend = false;

			int nLength = mSendStreamList.Length;
			if (nLength > 0)
			{
				if (nLength >= Config.nIOContexBufferLength)
				{
					nLength = Config.nIOContexBufferLength;
				}

				lock (lock_mSendStreamList_object)
				{
					mSendStreamList.WriteTo(0, mSendIOContex.Buffer, mSendIOContex.Offset, nLength);
				}

				mSendIOContex.SetBuffer(mSendIOContex.Offset, nLength);
				bContinueSend = true;
			}

			if (bContinueSend)
			{
				StartSendEventArg();
			}
			else
			{
				bSendIOContextUsed = false;
			}
		}

		private void DisConnected()
		{
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

		private void DisConnectedWithException(Exception e)
		{
#if DEBUG
			if (mSocket != null)
			{
				NetLog.LogException(e);
			}
#endif
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		private void DisConnectedWithSocketError(SocketError mError)
		{
#if DEBUG
            //有可能客户端主动关闭与服务器的链接了
            //NetLog.LogError("异常断开连接: " + mError);
#endif
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

		void CloseSocket()
		{
			if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
				{
					if (mSocket != null)
					{
						Socket mSocket2 = mSocket;
						mSocket = null;

						try
						{
							mSocket2.Shutdown(SocketShutdown.Both);
						}
						catch { }
						finally
						{
							mSocket2.Close();
						}
					}
				}
			}
			else
			{
				if (mSocket != null)
				{
					Socket mSocket2 = mSocket;
					mSocket = null;

					try
					{
						mSocket2.Shutdown(SocketShutdown.Both);
					}
					catch { }
					finally
					{
						mSocket2.Close();
					}
				}
			}
		}

		public void Reset()
		{
			CloseSocket();
			lock (lock_mSendStreamList_object)
			{
				mSendStreamList.reset();
			}
		}
	}

}