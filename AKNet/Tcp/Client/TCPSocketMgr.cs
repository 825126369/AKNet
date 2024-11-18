/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:34
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
        private bool bConnectIOContexUsed = false;
        private bool bDisConnectIOContexUsed = false;
        private bool bSendIOContextUsed = false;
        private bool bReceiveIOContextUsed = false;

        private ClientPeer mClientPeer;
        private readonly AkCircularBuffer<byte> mSendStreamList = null;
		private readonly object lock_mSocket_object = new object();
        private readonly object lock_mSendStreamList_object = new object();
        private readonly SocketAsyncEventArgs mConnectIOContex = null;
        private readonly SocketAsyncEventArgs mDisConnectIOContex = null;
		private readonly SocketAsyncEventArgs mSendIOContex = null;
        private readonly SocketAsyncEventArgs mReceiveIOContex = null;

        public TCPSocketMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
			
            mConnectIOContex = new SocketAsyncEventArgs();
			mDisConnectIOContex = new SocketAsyncEventArgs();
            mSendIOContex = new SocketAsyncEventArgs();
            mReceiveIOContex = new SocketAsyncEventArgs();
			
			mSendIOContex.SetBuffer(new byte[Config.nIOContexBufferLength], 0, Config.nIOContexBufferLength);
			mReceiveIOContex.SetBuffer(new byte[Config.nIOContexBufferLength], 0, Config.nIOContexBufferLength);
            
            mSendIOContex.Completed += OnIOCompleted;
            mReceiveIOContex.Completed += OnIOCompleted;
            mConnectIOContex.Completed += OnIOCompleted;
            mDisConnectIOContex.Completed += OnIOCompleted;

            mSendStreamList = new AkCircularBuffer<byte>(Config.nCircularBufferInitCapacity, Config.nCircularBufferMaxCapacity);

            mClientPeer.SetSocketState(SOCKET_PEER_STATE.NONE);
        }

		public void ReConnectServer()
		{
            bool Connected = false;
            if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
				{
					Connected = mSocket != null && mSocket.Connected;
				}
			}
			else
			{
				try
				{
					Connected = mSocket != null && mSocket.Connected;
				}
				catch { }
			}

            if (Connected)
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
				StartConnectEventArg();
			}
		}

		public bool DisConnectServer()
		{
			NetLog.Log("客户端 主动 断开服务器 Begin......");

			MainThreadCheck.Check();
			if (!bDisConnectIOContexUsed)
			{
				bDisConnectIOContexUsed = true;

				bool Connected = false;
				if (Config.bUseSocketLock)
				{
					lock (lock_mSocket_object)
					{
						Connected = mSocket != null && mSocket.Connected;
					}
				}
				else
				{
					try
					{
						Connected = mSocket != null && mSocket.Connected;
					}
					catch { };
				}

				if (Connected)
				{
					mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTING);
					mDisConnectIOContex.RemoteEndPoint = mIPEndPoint;
					StartDisconnectEventArg();
				}
				else
				{
					NetLog.Log("客户端 主动 断开服务器 Finish......");
					mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
					bDisConnectIOContexUsed = false;

				}
			}

			return mClientPeer.GetSocketState() == SOCKET_PEER_STATE.DISCONNECTED;
		}

		private void StartConnectEventArg()
		{
			bool bIOSyncCompleted = false;
			if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
				{
					if (mSocket != null)
					{
						bIOSyncCompleted = !mSocket.ConnectAsync(mConnectIOContex);
					}
					else
					{
						bConnectIOContexUsed = false;
					}
				}
			}
			else
			{
				if (mSocket != null)
				{
					try
					{
						bIOSyncCompleted = !mSocket.ConnectAsync(mConnectIOContex);
					}
					catch (Exception e)
					{
						bConnectIOContexUsed = false;
						DisConnectedWithException(e);
					}
				}
				else
				{
					bConnectIOContexUsed = false;
				}
			}

			if (bIOSyncCompleted)
			{
				this.ProcessConnect(mConnectIOContex);
			}
		}

		private void StartDisconnectEventArg()
		{
			bool bIOSyncCompleted = false;
			if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
				{
					if (mSocket != null)
					{
                        bIOSyncCompleted = !mSocket.DisconnectAsync(mDisConnectIOContex);
                    }
					else
					{
						bDisConnectIOContexUsed = false;
					}
				}
			}
			else
			{
				if (mSocket != null)
				{
					try
					{
						bIOSyncCompleted = !mSocket.DisconnectAsync(mDisConnectIOContex);
					}
					catch (Exception e)
					{
						bDisConnectIOContexUsed = false;
						DisConnectedWithException(e);
					}
				}
				else
				{
					bDisConnectIOContexUsed = false;
				}
			}

			if (bIOSyncCompleted)
			{
				this.ProcessDisconnect(mDisConnectIOContex);
			}
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
					else
					{
						bReceiveIOContextUsed = false;
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
						bReceiveIOContextUsed = false;
						DisConnectedWithException(e);
					}
				}
				else
				{
					bReceiveIOContextUsed = false;
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

				if (!bReceiveIOContextUsed)
				{
                    bReceiveIOContextUsed = true;
                    StartReceiveEventArg();
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
                DisConnectedWithSocketError(e.SocketError);
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
                    mClientPeer.mMsgReceiveMgr.MultiThreadingReceiveSocketStream(e);
					StartReceiveEventArg();
				}
				else
				{
					bReceiveIOContextUsed = false;
					DisConnectedWithNormal();
				}
			}
			else
			{
                bReceiveIOContextUsed = false;
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

		private void SendNetStream1(int BytesTransferred = 0)
		{
			if (BytesTransferred > 0)
			{
                lock (lock_mSendStreamList_object)
                {
                    mSendStreamList.ClearBuffer(BytesTransferred);
                }
            }
			
			int nLength = mSendStreamList.Length;
			if (nLength > 0)
			{
				if (nLength >= Config.nIOContexBufferLength)
				{
                    nLength = Config.nIOContexBufferLength;
				}

				lock (lock_mSendStreamList_object)
				{
					mSendStreamList.CopyTo(0, mSendIOContex.Buffer, mSendIOContex.Offset, nLength);
				}

				mSendIOContex.SetBuffer(mSendIOContex.Offset, nLength);
                StartSendEventArg();
            }
			else
			{
				bSendIOContextUsed = false;
			}
		}

		private void DisConnectedWithNormal()
		{
			NetLog.Log("客户端 正常 断开服务器 ");
            DisConnectedWithError();
        }

		private void DisConnectedWithException(Exception e)
		{
			if (mSocket != null)
			{
				NetLog.LogException(e);
			}
			DisConnectedWithError();
		}

        private void DisConnectedWithSocketError(SocketError e)
		{
			NetLog.Log("客户端 SocketError 异常 断开服务器: " + e.ToString());
			DisConnectedWithError();
        }

        private void DisConnectedWithError()
        {
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

		public IPEndPoint GetIPEndPoint()
		{
			if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
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
			}
			else
			{
				IPEndPoint mRemoteEndPoint = null;
				try
				{
					if (mSocket != null && mSocket.RemoteEndPoint != null)
					{
						mRemoteEndPoint = mSocket.RemoteEndPoint as IPEndPoint;
					}
				}
				catch { }

				return mRemoteEndPoint;
			}
		}

		private void CloseSocket()
		{
			if (Config.bUseSocketLock)
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

		public void Release()
		{
            CloseSocket();
        }
    }
}
