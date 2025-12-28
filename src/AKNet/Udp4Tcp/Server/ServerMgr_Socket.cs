/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using AKNet.Udp4Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Server
{
    internal partial class ServerMgr
    {
		public void InitNet()
		{
			List<int> mPortList = IPAddressHelper.GetAvailableUdpPortList();
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
				NetLog.LogError("Udp Server 自动查找可用端口 失败！！！");
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
            try
            {
                mState = SOCKET_SERVER_STATE.NORMAL;
                this.nPort = nPort;

                EndPoint bindEndPoint = new IPEndPoint(mIPAddress, nPort);
                mListenSocket.Bind(bindEndPoint);

                NetLog.Log("Udp Server 初始化成功:  " + mIPAddress + " | " + nPort);
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
            mAcceptIOContex.AcceptConnection = null;
            if (mListenSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mListenSocket.AcceptAsync(mAcceptIOContex);
                }
                catch (Exception e)
                {
                    if (mListenSocket != null)
                    {
                        NetLog.LogException(e);
                    }
                }
            }
            
            if (bIOSyncCompleted)
            {
                this.ProcessAccept(mAcceptIOContex);
            }
        }

        private void OnIOCompleted(object sender, ConnectionEventArgs e)
        {
            switch (e.LastOperation)
            {
                case ConnectionAsyncOperation.Accept:
                    this.ProcessAccept(e);
                    break;
                default:
                    break;
            }
        }

        private void ProcessAccept(ConnectionEventArgs e)
        {
            if (e.ConnectionError == ConnectionError.Success)
            {
                ConnectionPeer mClientSocket = e.AcceptConnection;
#if DEBUG
                NetLog.Assert(mClientSocket != null);
#endif
                if (!MultiThreadingHandleConnectedSocket(mClientSocket))
                {
                    HandleConnectFull(mClientSocket);
                }
            }
            else
            {
                NetLog.LogError("ProcessAccept: " + e.ConnectionError);
            }
            StartAcceptEventArg();
        }

        private void HandleConnectFull(ConnectionPeer mClientSocket)
        {
            try
            {
                mClientSocket.Close();
            }
            catch
            {

            }
        }

        public void CloseSocket()
		{
			if (mListenSocket != null)
			{
				Listener mSocket2 = mListenSocket;
                mListenSocket = null;

				try
				{
					mSocket2.Dispose();
				}
				catch (Exception) { }
			}
		}
	}

}









