/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Client
{
    internal class TCPSocketMgr
	{
		private Socket mSocket = null;
		private string ServerIp = "";
		private int nServerPort = 0;
		private IPEndPoint mIPEndPoint = null;

		ReadWriteIOContextPool mReadWriteIOContextPool = null;
		SimpleIOContextPool mSimpleIOContextPool = null;
		
		CircularBuffer<byte> mSendStreamList = null;

		private readonly object lock_mSocket_object = new object();
        private readonly object lock_mSendStreamList_object = new object();

        SocketAsyncEventArgs mConnectIOContex = null;
		SocketAsyncEventArgs mDisConnectIOContex = null;
		SocketAsyncEventArgs mSendIOContex = null;
		SocketAsyncEventArgs mReceiveIOContex = null;

		bool bConnectIOContexUsed = false;
        bool bDisConnectIOContexUsed = false;
        bool bSendIOContexUsed = false;

		private ClientPeer mClientPeer;

        public TCPSocketMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
			BufferManager mBufferManager = new BufferManager(Config.nIOContexBufferLength, 2);
			mReadWriteIOContextPool = new ReadWriteIOContextPool(2, mBufferManager);
			mSimpleIOContextPool = new SimpleIOContextPool(2);

			mConnectIOContex = mSimpleIOContextPool.Pop();
			mDisConnectIOContex = mSimpleIOContextPool.Pop();
			mSendIOContex = mReadWriteIOContextPool.Pop();
			mReceiveIOContex = mReadWriteIOContextPool.Pop();
            mSendIOContex.Completed += OnIOCompleted;
            mReceiveIOContex.Completed += OnIOCompleted;
            mConnectIOContex.Completed += OnIOCompleted;
            mDisConnectIOContex.Completed += OnIOCompleted;

            mSendStreamList = new CircularBuffer<byte>(Config.nSendReceiveCacheBufferInitLength);

            mClientPeer.SetSocketState(SOCKET_PEER_STATE.NONE);
        }

		public void ReConnectServer()
		{
			if (mSocket != null && mSocket.Connected)
			{
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
            }
			else
			{
				ConnectServer(this.ServerIp, this.nServerPort);
			}
		}

		public void ConnectServer(string ServerAddr, int ServerPort)
		{
            this.ServerIp = ServerAddr;
			this.nServerPort = ServerPort;

            mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTING);
            NetLog.Log("Client 正在连接服务器: " + this.ServerIp + " | " + this.nServerPort);

			Reset();
            lock (lock_mSocket_object)
			{
				mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				if (mIPEndPoint == null)
				{
					IPAddress mIPAddress = IPAddress.Parse(ServerAddr);
					mIPEndPoint = new IPEndPoint(mIPAddress, ServerPort);
				}
				
				if (!bConnectIOContexUsed)
				{
                    bConnectIOContexUsed = true;
					mConnectIOContex.RemoteEndPoint = mIPEndPoint;
					if (!mSocket.ConnectAsync(mConnectIOContex))
					{
						ProcessConnect(mConnectIOContex);
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

        public bool DisConnectServer()
		{
			NetLog.Log("客户端 主动 断开服务器 Begin......");

			if (!bDisConnectIOContexUsed)
			{
				bDisConnectIOContexUsed = true;

                lock (lock_mSocket_object)
				{
					if (mSocket != null && mSocket.Connected)
					{
						mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTING);
						mDisConnectIOContex.RemoteEndPoint = mIPEndPoint;
						if (!mSocket.DisconnectAsync(mDisConnectIOContex))
						{
							ProcessDisconnect(mDisConnectIOContex);
						}
					}
					else
					{
						NetLog.Log("客户端 主动 断开服务器 Finish......");
						mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
						bDisConnectIOContexUsed = false;

                    }
				}
			}

			return mClientPeer.GetSocketState() == SOCKET_PEER_STATE.DISCONNECTED;
		}

		private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Connect:
					ProcessConnect(e);
					break;
				case SocketAsyncOperation.Disconnect:
					ProcessDisconnect(e);
					break;
				case SocketAsyncOperation.Receive:
					this.ProcessReceive(e);
					break;
				case SocketAsyncOperation.Send:
					this.ProcessSend(e);
					break;
				default:
					NetLog.LogError("The last operation completed on the socket was not a receive or send");
					break;
			}
		}

		private void ProcessConnect(SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				NetLog.Log(string.Format("Client 连接服务器: {0}:{1} 成功", this.ServerIp, this.nServerPort));
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);

				if (!mSocket.ReceiveAsync(mReceiveIOContex))
				{
					ProcessReceive(mReceiveIOContex);
				}
			}
			else
			{
				NetLog.Log(string.Format("Client 连接服务器: {0}:{1} 失败：{2}", this.ServerIp, this.nServerPort, e.SocketError));
				if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTING)
				{
					mClientPeer.SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
				}
			}

			e.RemoteEndPoint = null;
			bConnectIOContexUsed = false;
		}

		private void ProcessDisconnect(SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
				NetLog.Log("客户端 主动 断开服务器 Finish");
			}
			else
			{
				DisConnectedWithException(e.SocketError);
            }

			e.RemoteEndPoint = null;
            bDisConnectIOContexUsed = false;
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
								ProcessReceive(e);
							}
						}
					}
				}
				else
				{
					DisConnectedWithNormal();
				}
			}
			else
			{
				DisConnectedWithException(e.SocketError);
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
				bSendIOContexUsed = false;
			}
		}

        private void EnSureCircularBufferCapacityOk(ReadOnlySpan<byte> mBufferSegment)
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
        }

		public void SendNetStream(ReadOnlySpan<byte> mBufferSegment)
		{
			if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
			{
				NetLog.Assert(mBufferSegment.Length <= Config.nMsgPackageBufferMaxLength, "发送尺寸超出最大限制" + mBufferSegment.Length + " | " + Config.nMsgPackageBufferMaxLength);

				lock (lock_mSendStreamList_object)
				{
					EnSureCircularBufferCapacityOk(mBufferSegment);
					mSendStreamList.WriteFrom(mBufferSegment);
				}

				if (!bSendIOContexUsed)
				{
					bSendIOContexUsed = true;
					SendNetStream1(mSendIOContex);
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
						bSendIOContexUsed = false;
					}
				}
			}
			else
			{
				bSendIOContexUsed = false;
			}
			
		}

		private void DisConnectedWithNormal()
		{
			NetLog.Log("客户端 正常 断开服务器 ");
			Reset();
		}

		private void DisConnectedWithException(SocketError e)
		{
			NetLog.Log("客户端 异常 断开服务器: " + e.ToString());
			Reset();
			var mSocketPeerState = mClientPeer.GetSocketState();

			if (mSocketPeerState == SOCKET_PEER_STATE.DISCONNECTING)
			{
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
			}
			else if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
			{
				mClientPeer.SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
			}
		}

		private void CloseSocket()
		{
			lock (lock_mSocket_object)
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

		public void Release()
		{
            CloseSocket();
        }
    }
}
