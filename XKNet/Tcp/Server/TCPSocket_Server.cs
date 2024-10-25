using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Tcp.Common;
using XKNet.Udp.POINTTOPOINT.Server;

namespace XKNet.Tcp.Server
{
    internal class TCPSocket_Server
	{
		private int nPort;
		private Socket mListenSocket = null;
		private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;

		public void InitNet()
		{
			List<int> mPortList = IPAddressHelper.GetAvailableTcpPortList();
			int nTryBindCount = 100;
			while (nTryBindCount-- > 0)
			{
				if (mPortList.Count > 0)
				{
					int nPort = mPortList[RandomTool.RandomArrayIndex(0, mPortList.Count)];
					InitNet(nPort);
					mPortList.Remove(nPort);
					if (GetServerState() == SOCKET_SERVER_STATE.NORMAL)
					{
						break;
					}
				}
			}

			if (GetServerState() != SOCKET_SERVER_STATE.NORMAL)
			{
				NetLog.LogError("Tcp Server 自动查找可用端口 失败！！！");
			}
		}

        public void InitNet(int nPort)
        {
			InitNet(IPAddress.Any, nPort);
        }

		public void InitNet(string Ip, int nPort)
		{
			InitNet(IPAddress.Parse(Ip), nPort);
		}
		
        private void InitNet(IPAddress mIPAddress, int nPort)
		{
			CloseNet();
			try
			{
				this.nPort = nPort;
				mState = SOCKET_SERVER_STATE.NORMAL;
				IPEndPoint localEndPoint = new IPEndPoint(mIPAddress, nPort);

				this.mListenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				this.mListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
				this.mListenSocket.Bind(localEndPoint);
				this.mListenSocket.Listen(Config.numConnections);

				NetLog.Log("服务器 初始化成功: " + mIPAddress + " | " + nPort);

				StartListenClient();
			}
			catch (SocketException ex)
			{
				mState = SOCKET_SERVER_STATE.EXCEPTION;
				NetLog.LogError(ex.SocketErrorCode + " | " + ex.Message + " | " + ex.StackTrace);
				NetLog.LogError("服务器 初始化失败: " + mIPAddress + " | " + nPort);
			}
			catch (Exception ex)
			{
				mState = SOCKET_SERVER_STATE.EXCEPTION;
				NetLog.LogError(ex.Message + " | " + ex.StackTrace);
				NetLog.LogError("服务器 初始化失败: " + mIPAddress + " | " + nPort);
			}
		}

        public int GetPort()
        {
            return this.nPort;
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