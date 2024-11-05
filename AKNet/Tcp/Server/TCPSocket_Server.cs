/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
//#define SOCKET_LOCK

using AKNet.Common;
using AKNet.Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
	internal class TCPSocket_Server
	{
		private int nPort;
		private Socket mListenSocket = null;
		private readonly object lock_mSocket_object = new object();

		private bool bAccetpEventArgCanUse = false;
		private readonly SocketAsyncEventArgs mAcceptIOContex = new SocketAsyncEventArgs();

		private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;
		private TcpServer mTcpServer;

		public TCPSocket_Server(TcpServer mTcpServer)
		{
			this.mTcpServer = mTcpServer;
			mAcceptIOContex.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
			mAcceptIOContex.AcceptSocket = null;
			bAccetpEventArgCanUse = true;

		}

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

				StartAcceptEventArg();
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

		private void StartAcceptEventArg()
		{
			bool bIOSyncCompleted = false;
			mAcceptIOContex.AcceptSocket = null;
#if SOCKET_LOCK
			lock (lock_mSocket_object)
#endif
			{
				if (mListenSocket != null)
				{
#if !SOCKET_LOCK
					try
					{
#endif
						bIOSyncCompleted = !mListenSocket.AcceptAsync(mAcceptIOContex);
#if !SOCKET_LOCK
					}
					catch (Exception e)
					{
						if (mListenSocket != null)
						{
							NetLog.LogException(e);
						}
					}
#endif
				}
			}

			if (bIOSyncCompleted)
			{
				this.ProcessAccept(mAcceptIOContex);
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
				if (!mTcpServer.mClientPeerManager.MultiThreadingHandleConnectedSocket(mClientSocket))
				{
					HandleConnectFull(mClientSocket);
				}
			}
			else
			{
				NetLog.LogError("ProcessAccept: " + e.SocketError);
			}
			StartAcceptEventArg();
		}

		private void HandleConnectFull(Socket mClientSocket)
		{
			try
			{
				mClientSocket.Shutdown(SocketShutdown.Both);
			}
			catch
			{

			}
			finally
			{
				mClientSocket.Close();
			}
		}

		public void CloseNet()
		{
			MainThreadCheck.Check();
#if SOCKET_LOCK
			lock (lock_mSocket_object)
#endif
			{
				if (mListenSocket != null)
				{
					Socket mSocket = mListenSocket;
					mListenSocket = null;

					try
					{
						mSocket.Close();
					}
					catch { }
					finally
					{
						mSocket.Close();
					}
				}
			}
		}
	}

}