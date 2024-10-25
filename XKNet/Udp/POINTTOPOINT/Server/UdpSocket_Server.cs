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
		private NetServer mNetServer = null;

		private bool bSendIOContexUsed = false;
        private SocketAsyncEventArgs ReceiveArgs;
        private SocketAsyncEventArgs SendArgs;
        private readonly object lock_mSocket_object = new object();
		private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;
		public SocketUdp_Server(NetServer mNetServer)
		{
			this.mNetServer = mNetServer;
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
			ReceiveArgs = new SocketAsyncEventArgs();
			ReceiveArgs.Completed += IO_Completed;
			ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
			ReceiveArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

			SendArgs = new SocketAsyncEventArgs();
			SendArgs.Completed += IO_Completed;
			SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            SendArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

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

		void IO_Completed(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.ReceiveFrom:
					ProcessReceive(sender, e);
					break;
				case SocketAsyncOperation.SendTo:
					ProcessSend(sender, e);
					break;
				default:
					NetLog.Log(e.LastOperation.ToString());
					break;
			}
		}

		private void ProcessReceive(object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
			{
				NetUdpFixedSizePackage mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
				mPackage.CopyFrom(e);

				ClientPeer mPeer = mNetServer.GetClientPeerManager().FindOrAddClient(e.RemoteEndPoint);
				mPeer.mMsgReceiveMgr.MultiThreadingReceiveNetPackage(mPackage);
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

		private void ProcessSend(object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				SendNetPackage2(e);
			}
			else
			{
				bSendIOContexUsed = false;
				if (e.RemoteEndPoint != null)
				{
					NetLog.LogError($"Server ProcessSend SocketError: {e.SocketError} {e.RemoteEndPoint}");
					ClientPeer mPeer = mNetServer.GetClientPeerManager().FindClient(e.RemoteEndPoint);
					if (mPeer != null)
					{
						mPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
					}
				}
			}
		}

        readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
        readonly object bSendIOContexUsedObj = new object();
		public void SendNetPackage(NetUdpFixedSizePackage mPackage)
		{
			var mPackage2 = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
			mPackage2.CopyFrom(mPackage);
			mPackage2.remoteEndPoint = mPackage.remoteEndPoint;
			mSendPackageQueue.Enqueue(mPackage2);

			bool bCanGoNext = false;
			lock (bSendIOContexUsedObj)
			{
				bCanGoNext = bSendIOContexUsed == false;
				if (!bSendIOContexUsed)
				{
					bSendIOContexUsed = true;
				}
			}

			if (bCanGoNext)
			{
				SendNetPackage2(SendArgs);
			}
		}

        private void SendNetPackage2(SocketAsyncEventArgs e)
        {
            NetUdpFixedSizePackage mPackage = null;
            if (mSendPackageQueue.TryDequeue(out mPackage))
            {
                Array.Copy(mPackage.buffer, e.Buffer, mPackage.Length);
                e.SetBuffer(0, mPackage.Length);
				e.RemoteEndPoint = mPackage.remoteEndPoint;
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);

                lock (lock_mSocket_object)
                {
                    if (mSocket != null)
                    {
                        if (!mSocket.SendToAsync(e))
                        {
                            IO_Completed(null, e);
                        }
                    }
                    else
                    {
                        bSendIOContexUsed = false;
                    }
                }
            }
            else
            {
                bSendIOContexUsed = false;
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

            if (ReceiveArgs != null)
            {
                ReceiveArgs.Completed -= IO_Completed;
                ReceiveArgs.Dispose();
                ReceiveArgs = null;
            }
        }
	}

}









