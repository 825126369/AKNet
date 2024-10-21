using System;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
    internal class TCPSocket_Server
	{
		private Socket mListenSocket = null;
		private TcpServer mTcpServer;
		private SOCKET_SERVER_STATE mState;
        public TCPSocket_Server(TcpServer mServer)
		{
			this.mTcpServer = mServer;
        }

		public void InitNet(string ServerAddr, int ServerPort)
		{
			CloseNet();
			try
			{
				mState = SOCKET_SERVER_STATE.NORMAL;
				IPAddress serverAddr = IPAddress.Parse(ServerAddr);
				IPEndPoint localEndPoint = new IPEndPoint(serverAddr, ServerPort);

				this.mListenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				this.mListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
				this.mListenSocket.Bind(localEndPoint);
				this.mListenSocket.Listen(Config.numConnections);

				NetLog.Log("服务器 初始化成功: " + ServerAddr + " | " + ServerPort);

				StartListenClient();
			}
			catch (SocketException ex)
			{
				mState = SOCKET_SERVER_STATE.EXCEPTION;
				NetLog.LogError(ex.SocketErrorCode + " | " + ex.Message + " | " + ex.StackTrace);
				NetLog.LogError("服务器 初始化失败: " + ServerAddr + " | " + ServerPort);
			}
			catch (Exception ex)
			{
				mState = SOCKET_SERVER_STATE.EXCEPTION;
				NetLog.LogError(ex.Message + " | " + ex.StackTrace);
				NetLog.LogError("服务器 初始化失败: " + ServerAddr + " | " + ServerPort);
			}
		}

		public SOCKET_SERVER_STATE GetServerState()
		{
			return mState;
		}

		private void StartListenClient()
		{
			SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
			acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
			acceptEventArg.AcceptSocket = null;

			if (!this.mListenSocket.AcceptAsync(acceptEventArg))
			{
				this.ProcessAccept(acceptEventArg);
			}
		}

        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Accept:
					this.ProcessAccept(e);
					break;
				default:
					break;
			}
		}

		private void ProcessAccept(SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				Socket mClientSocket = e.AcceptSocket;
#if DEBUG
				NetLog.Assert(mClientSocket != null);
#endif
				if (!ServerGlobalVariable.Instance.mClientPeerManager.AddClient(mClientSocket))
				{
					HandleConnectFull(mClientSocket);
				}
			}
			else
			{
				NetLog.LogError("ProcessAccept: " + e.SocketError);
			}

			e.AcceptSocket = null;
			if (!this.mListenSocket.AcceptAsync(e))
			{
				this.ProcessAccept(e);
			}
		}

		private void HandleConnectFull(Socket mClientSocket)
		{
			mClientSocket.Close();
		}

		public void CloseNet()
		{
			try
			{
				if (mListenSocket != null)
				{
					mListenSocket.Close();
				}
			}
			catch { }
			mListenSocket = null;
		}
	}

}