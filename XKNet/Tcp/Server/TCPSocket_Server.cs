using System;
using System.Net;
using System.Net.Sockets;
using XKNetCommon;

namespace XKNetTcpServer
{
    public class TCPSocket_Server : ServerBase
	{
		private Socket mListenSocket = null;

		public TCPSocket_Server()
		{

		}

		public void InitNet(string ServerAddr, int ServerPort)
		{
			IPAddress serverAddr = IPAddress.Parse(ServerAddr);
			IPEndPoint localEndPoint = new IPEndPoint(serverAddr, ServerPort);

			this.mListenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			this.mListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
			this.mListenSocket.Bind(localEndPoint);
			this.mListenSocket.Listen(ServerConfig.numConnections * 100);

			NetLog.Log("服务器 初始化成功: " + ServerAddr + " | " + ServerPort);

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
				if(!mClientPeerManager.AddClient(mClientSocket))
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
				mListenSocket.Close();
			}
			catch { }

			mListenSocket = null;
		}
	}

}