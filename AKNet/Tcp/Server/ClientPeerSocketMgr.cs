/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
//#define SOCKET_LOCK

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
#if SOCKET_LOCK
			lock (lock_mSocket_object)
#endif
			{
				if (mSocket != null)
				{
#if !SOCKET_LOCK
					try
					{
#endif
						bIOSyncCompleted = !mSocket.ReceiveAsync(mReceiveIOContex);
#if !SOCKET_LOCK
					}
					catch (Exception e)
					{
						DisConnectedWithException(e);
					}
#endif
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
#if SOCKET_LOCK
			lock (lock_mSocket_object)
#endif
			{
				if (mSocket != null)
				{
#if !SOCKET_LOCK
					try
					{
#endif
						bIOSyncCompleted = !mSocket.SendAsync(mSendIOContex);
#if !SOCKET_LOCK
					}
					catch (Exception e)
					{
                        bSendIOContextUsed = false;
                        DisConnectedWithException(e);
					}
#endif
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
				NetLog.Assert(mBufferSegment.Length <= Config.nMsgPackageBufferMaxLength, "发送尺寸超出最大限制: " + mBufferSegment.Length + " | " + Config.nMsgPackageBufferMaxLength);

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
#if DEBUG
            NetLog.Log("正常断开连接");
#endif
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

		private void DisConnectedWithException(Exception e)
		{
#if DEBUG
			//有可能客户端主动关闭与服务器的链接了
			//NetLog.LogError("异常断开连接: " + mError);
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
#if SOCKET_LOCK
			lock (lock_mSocket_object)
#endif
			{
				if (mSocket != null)
				{
					try
					{
						mSocket.Shutdown(SocketShutdown.Both);
					}
					catch { }
					finally
					{
						mSocket.Close();
					}
					mSocket = null;
                    bSendIOContextUsed = false;
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