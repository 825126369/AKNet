using Google.Protobuf.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
	internal class SocketUdp_Server
	{
		private int nPort = 0;
		private Socket mSocket = null;
		private UdpServer mNetServer = null;
		
        private SocketAsyncEventArgs ReceiveArgs;
        private readonly object lock_mSocket_object = new object();
		private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;
        private readonly IPEndPoint mEndPointEmpty = new IPEndPoint(IPAddress.Any, 0);
        public SocketUdp_Server(UdpServer mNetServer)
		{
			this.mNetServer = mNetServer;

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
				this.Release();
				this.nPort = nPort;

				mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
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

        private void StartReceiveFromAsync()
		{
            lock (lock_mSocket_object)
            {
                if (mSocket != null)
                {
                    if (!mSocket.ReceiveFromAsync(ReceiveArgs))
                    {
                        ProcessReceive(null, ReceiveArgs);
                    }
                }
            }
        }

		private void ProcessReceive(object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
			{
				NetLog.Assert(e.RemoteEndPoint != mEndPointEmpty);
				NetUdpFixedSizePackage mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
				mPackage.CopyFrom(e);
                mPackage.remoteEndPoint = e.RemoteEndPoint;

                mNetServer.GetClientPeerManager().MultiThreadingReceiveNetPackage(mPackage);
				e.RemoteEndPoint = mEndPointEmpty;
            }

			lock (lock_mSocket_object)
			{
				if (mSocket != null)
				{
                    if (!mSocket.ReceiveFromAsync(e))
					{
						ProcessReceive(null, e);
					}
				}
			}
		}

		public void SendNetPackage(SocketAsyncEventArgs e, Action<object, SocketAsyncEventArgs> IO_Completed)
		{
			lock (lock_mSocket_object)
			{
				if (mSocket != null)
				{
					if (!mSocket.SendToAsync(e))
					{
						IO_Completed(null, e);
					}
				}
			}
		}

        public void Release()
		{
            if (mSocket != null)
            {
                try
                {
                    mSocket.Close();
                }
                catch (Exception) { }
                mSocket = null;
            }
        }
	}

}









