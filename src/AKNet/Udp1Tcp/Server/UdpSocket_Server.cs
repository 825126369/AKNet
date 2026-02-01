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
    internal class SocketUdp_Server
	{
		private int nPort = 0;
		private Socket mSocket = null;
		private UdpServer mNetServer = null;
		
        private readonly SocketAsyncEventArgs ReceiveArgs;
        private readonly object lock_mSocket_object = new object();
		private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;
        private readonly IPEndPoint mEndPointEmpty = new IPEndPoint(IPAddress.Any, 0);
        public SocketUdp_Server(UdpServer mNetServer)
		{
			this.mNetServer = mNetServer;

            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, int.MaxValue);

            ReceiveArgs = new SocketAsyncEventArgs();
            ReceiveArgs.Completed += ProcessReceive;
            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.RemoteEndPoint = mEndPointEmpty;
        }

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

				NetLog.Log("Udp Server 初始化成功:  " + mIPAddress + " | " + nPort);
				StartReceiveFromAsync();
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

		public Socket GetSocket()
		{
			return mSocket;
		}

		private void StartReceiveFromAsync()
		{
			bool bIOSyncCompleted = false;
			if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
				{
					if (mSocket != null)
					{
						bIOSyncCompleted = !mSocket.ReceiveFromAsync(ReceiveArgs);
					}
				}
			}
			else
			{
				if (mSocket != null)
				{
					try
					{
						bIOSyncCompleted = !mSocket.ReceiveFromAsync(ReceiveArgs);
					}
					catch (Exception e)
					{
						if (mSocket != null)
						{
							NetLog.LogException(e);
						}
					}
				}
			}

            UdpStatistical.AddReceiveIOCount(bIOSyncCompleted);
            if (bIOSyncCompleted)
			{
				ProcessReceive(null, ReceiveArgs);
			}
		}

		private void ProcessReceive(object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
			{
				NetLog.Assert(e.RemoteEndPoint != mEndPointEmpty);
                mNetServer.GetFakeSocketMgr().MultiThreadingReceiveNetPackage(e);
                e.RemoteEndPoint = mEndPointEmpty;
			}
			StartReceiveFromAsync();
		}

		public void SendTo(NetUdpFixedSizePackage mPackage)
		{
			if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
				{
					int nLength = mSocket.SendTo(mPackage.buffer, 0, mPackage.Length, SocketFlags.None, mPackage.remoteEndPoint);
					NetLog.Assert(nLength > mPackage.Length);
				}
			}
			else
			{
				try
				{
					int nLength = mSocket.SendTo(mPackage.buffer, 0, mPackage.Length, SocketFlags.None, mPackage.remoteEndPoint);
                    NetLog.Assert(nLength > mPackage.Length);
                }
				catch { }
			}
		}

		public bool SendToAsync(SocketAsyncEventArgs e)
		{
			bool bIOSyncCompleted = false;
			if (Config.bUseSocketLock)
			{
				lock (lock_mSocket_object)
				{
					if (mSocket != null)
					{
						bIOSyncCompleted = !mSocket.SendToAsync(e);
					}
				}
			}
			else
			{
				if (mSocket != null)
				{
					try
					{
						bIOSyncCompleted = !mSocket.SendToAsync(e);
					}
					catch (Exception ex)
					{
						if (mSocket != null)
						{
							NetLog.LogException(ex);
						}
					}
				}
			}

			UdpStatistical.AddSendIOCount(bIOSyncCompleted);
			return !bIOSyncCompleted;
		}

        public void Release()
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
                            mSocket2.Close();
						}
						catch (Exception) { }
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
                        mSocket2.Close();
                    }
                    catch (Exception) { }
                }
            }
        }
	}

}









