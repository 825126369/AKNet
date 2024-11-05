/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

#define SOCKET_LOCK
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
        private readonly CircularBuffer<byte> mSendStreamList = null;
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

            mSendStreamList = new CircularBuffer<byte>(Config.nIOContexBufferLength);

            mClientPeer.SetSocketState(SOCKET_PEER_STATE.NONE);
        }

		public void ReConnectServer()
		{
			bool Connected = false;
#if SOCKET_LOCK
			lock (lock_mSocket_object)
#endif
			{
				Connected = mSocket != null && mSocket.Connected;
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

#if SOCKET_LOCK
			lock (lock_mSocket_object)
#endif
			{
				mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			}

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
				lock (lock_mSocket_object)
				{
					Connected = mSocket != null && mSocket.Connected;
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
					bIOSyncCompleted = !mSocket.ConnectAsync(mConnectIOContex);
#if !SOCKET_LOCK
					}
					catch (Exception e)
					{
						bConnectIOContexUsed = false;
						DisConnectedWithException(e);
					}
#endif
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
					bIOSyncCompleted = !mSocket.DisconnectAsync(mDisConnectIOContex);
#if !SOCKET_LOCK
					}
					catch (Exception e)
					{
					    bDisConnectIOContexUsed = false;
						DisConnectedWithException(e);
					}
#endif
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
						bReceiveIOContextUsed = false;
						DisConnectedWithException(e);
					}
#endif
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
					ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(e.Buffer, e.Offset, e.BytesTransferred);
                    mClientPeer.mMsgReceiveMgr.MultiThreadingReceiveSocketStream(readOnlySpan);
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
				NetLog.Assert(mBufferSegment.Length <= Config.nMsgPackageBufferMaxLength, "发送尺寸超出最大限制" + mBufferSegment.Length + " | " + Config.nMsgPackageBufferMaxLength);

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

		private void DisConnectedWithNormal()
		{
			NetLog.Log("客户端 正常 断开服务器 ");
            DisConnectedWithError();
        }

		private void DisConnectedWithException(Exception e)
		{
			NetLog.Log("客户端 Exception 异常 断开服务器: " + e.ToString());
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
#if SOCKET_LOCK
            lock (lock_mSocket_object)
#endif
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

        private void CloseSocket()
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
