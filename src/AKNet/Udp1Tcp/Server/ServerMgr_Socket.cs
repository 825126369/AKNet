/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using AKNet.Udp1Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp1Tcp.Server
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
				mSocket.Bind(bindEndPoint);

				NetLog.Log($"{NetType.Udp1Tcp.ToString()} 服务器 初始化成功: {bindEndPoint}");
				StartReceiveFromAsync();
			}
			catch (SocketException ex)
			{
				mState = SOCKET_SERVER_STATE.EXCEPTION;
				NetLog.LogError(ex.SocketErrorCode + " | " + ex.Message + " | " + ex.StackTrace);
				NetLog.LogError($"{NetType.Udp1Tcp.ToString()} 服务器 初始化失败: {mIPAddress} | {nPort}");
			}
			catch (Exception ex)
			{
				mState = SOCKET_SERVER_STATE.EXCEPTION;
				NetLog.LogError(ex.Message + " | " + ex.StackTrace);
				NetLog.LogError($"{NetType.Udp1Tcp.ToString()} 服务器 初始化失败: {mIPAddress} | {nPort}");
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

		public Socket GetSocket()
		{
			return mSocket;
		}

		private void StartReceiveFromAsync()
		{
			bool bIOPending = true;
			if (mSocket != null)
			{
				try
				{
                    bIOPending = mSocket.ReceiveFromAsync(ReceiveArgs);
				}
				catch (Exception e)
				{
					if (mSocket != null)
					{
						NetLog.LogException(e);
					}
				}
			}
			
			UdpStatistical.AddReceiveIOCount(!bIOPending);
			if (!bIOPending)
			{
				ProcessReceive(null, ReceiveArgs);
			}
		}

		private void ProcessReceive(object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
			{
                GetFakeSocketMgr().MultiThreadingReceiveNetPackage(e);
                e.RemoteEndPoint = mEndPointEmpty;
			}
			StartReceiveFromAsync();
		}

		public void SendTo(NetUdpFixedSizePackage mPackage)
		{
			try
			{
				int nLength = mSocket.SendTo(mPackage.buffer, 0, mPackage.Length, SocketFlags.None, mPackage.remoteEndPoint);
				NetLog.Assert(nLength > mPackage.Length);
			}
			catch { }
		}

		public bool SendToAsync(SocketAsyncEventArgs e)
		{
			bool bIOPending = mSocket.SendToAsync(e);
			UdpStatistical.AddSendIOCount(!bIOPending);
			return bIOPending;
		}

		public void Release()
		{
			if (mSocket != null)
			{
				Socket mSocket2 = mSocket;
				mSocket = null;

				try
				{
					mSocket2.Close();
				}
				catch (Exception) { }
			}
		}
	}

}









