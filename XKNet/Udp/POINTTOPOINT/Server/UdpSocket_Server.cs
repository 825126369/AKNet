using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class SocketUdp_Server
	{
        private Socket mSocket = null;
        private SocketAsyncEventArgs ReceiveArgs;
        private SocketAsyncEventArgs SendArgs;
        private NetServer mNetServer = null;

		private bool bSendIOContexUsed = false;
        private object lock_mSocket_object = new object();

        public SocketUdp_Server(NetServer mNetServer)
		{
			this.mNetServer = mNetServer;
		}

		public void InitNet(string ip, int ServerPort)
		{
			if (mSocket != null)
			{
				this.Release();
			}

			mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			EndPoint bindEndPoint = new IPEndPoint(IPAddress.Parse(ip), ServerPort);
			mSocket.Bind(bindEndPoint);

			NetLog.Log("Udp Server 初始化成功:  " + ip + " | " + ServerPort);
			StartReceiveFromAsync();
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









