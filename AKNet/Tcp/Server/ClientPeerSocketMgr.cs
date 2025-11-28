/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:38
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Tcp.Common;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    internal class ClientPeerSocketMgr
	{
        private readonly IMemoryOwner<byte> mIMemoryOwner_Send = MemoryPool<byte>.Shared.Rent(Config.nIOContexBufferLength);
        private readonly IMemoryOwner<byte> mIMemoryOwner_Receive = MemoryPool<byte>.Shared.Rent(Config.nIOContexBufferLength);
        private readonly SocketAsyncEventArgs mReceiveIOContex = new SocketAsyncEventArgs();
		private readonly SocketAsyncEventArgs mSendIOContex = new SocketAsyncEventArgs();
		private readonly AkCircularManyBuffer mSendStreamList = new AkCircularManyBuffer();
        private readonly object lock_mSocket_object = new object();
        private readonly ClientPeerPrivate mClientPeer;
        private readonly TcpServer mTcpServer;

        private Socket mSocket = null;
        private bool bSendIOContextUsed = false;

        public ClientPeerSocketMgr(ClientPeerPrivate mClientPeer, TcpServer mTcpServer)
		{
			this.mClientPeer = mClientPeer;
			this.mTcpServer = mTcpServer;

            mSendIOContex.Completed += OnIOCompleted;
            mReceiveIOContex.SetBuffer(mIMemoryOwner_Receive.Memory);
            mReceiveIOContex.Completed += OnIOCompleted;
			bSendIOContextUsed = false;

			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		public void HandleConnectedSocket(Socket otherSocket)
		{
			MainThreadCheck.Check();

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
					mClientPeer.mMsgReceiveMgr.MultiThreadingReceiveSocketStream(e);
                    StartReceiveEventArg();
                }
				else
				{
                    DisConnectedWithNormal();
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
                if (e.BytesTransferred > 0)
                {
                    SendNetStream1(e.BytesTransferred);
                }
                else
                {
                    DisConnectedWithNormal();
                    bSendIOContextUsed = false;
                }
            }
			else
			{
                DisConnectedWithSocketError(e.SocketError);
				bSendIOContextUsed = false;
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
				SendNetStream1();
			}
		}

		private void SendNetStream1(int BytesTransferred = 0)
		{
            if (BytesTransferred > 0)
            {
				lock (mSendStreamList)
				{
					mSendStreamList.ClearBuffer(BytesTransferred);
				}
            }
			
			int nLength = mSendStreamList.Length;
			if (nLength > 0)
			{
                var mMemory = mIMemoryOwner_Send.Memory;
                nLength = Math.Min(mMemory.Length, nLength);
                nLength = Math.Min(Config.nIOContexBufferLength * 8, nLength);

                mMemory = mIMemoryOwner_Send.Memory.Slice(0, nLength);
                lock (mSendStreamList)
                {
                    mSendStreamList.CopyTo(mMemory.Span);
                }
                mSendIOContex.SetBuffer(mMemory);
                StartSendEventArg();
            }
			else
			{
				bSendIOContextUsed = false;
			}
		}

        private void DisConnectedWithNormal()
        {
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

        private void DisConnectedWithException(Exception e)
		{
			mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
		}

		private void DisConnectedWithSocketError(SocketError mError)
		{
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
			lock (mSendStreamList)
			{
				mSendStreamList.Reset();
			}
		}

        public void Release()
        {
            CloseSocket();
            lock (mSendStreamList)
            {
                mSendStreamList.Dispose();
            }
			mIMemoryOwner_Send.Dispose();
			mIMemoryOwner_Receive.Dispose();
        }
    }

}