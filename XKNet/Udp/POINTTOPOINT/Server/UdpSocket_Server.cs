using System;
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
        private NetServer mNetServer = null;

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
			ReceiveArgs.RemoteEndPoint = new IPEndPoint(IPAddress.None, 0);
			
			if (!mSocket.ReceiveFromAsync(ReceiveArgs))
			{
				ProcessReceive(null, ReceiveArgs);
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
				int nLength = e.BytesTransferred;
				NetUdpFixedSizePackage mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
				Array.Copy(e.Buffer, 0, mPackage.buffer, 0, nLength);
				mPackage.Length = nLength;

				ClientPeer mPeer = mNetServer.GetClientPeerManager().FindOrAddClient(e.RemoteEndPoint);
				mPeer.mMsgReceiveMgr.ReceiveUdpSocketFixedPackage(mPackage);

				if (mSocket != null)
				{
					lock (mSocket)
					{
						if (!mSocket.ReceiveFromAsync(e))
						{
							ProcessReceive(null, e);
						}
					}
				}
			}
			else
			{
				if (mSocket != null)
				{
					lock (mSocket)
					{
						if (!mSocket.ReceiveFromAsync(e))
						{
							ProcessReceive(null, e);
						}
					}
				}
			}
		}

		private void ProcessSend(object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{

			}
			else
			{
                NetLog.Log($"Server ProcessSend SocketError: {e.SocketError} {e.RemoteEndPoint}");
                ClientPeer mPeer = mNetServer.GetClientPeerManager().FindClient(e.RemoteEndPoint);
				if (mPeer != null)
				{
					mPeer.SetSocketState(SERVER_SOCKET_PEER_STATE.DISCONNECTED);
				}
			}
		}

		public void SendNetPackage(NetEndPointPackage mEndPointPacakge)
		{
			EndPoint remoteEndPoint = mEndPointPacakge.mRemoteEndPoint;
			NetUdpFixedSizePackage mPackage = mEndPointPacakge.mPackage;

			NetLog.Assert(mPackage != null, "mPackage = null");
			NetLog.Assert(mPackage.Length >= Config.nUdpPackageFixedHeadSize, "发送长度要大于等于 包头： " + mPackage.Length);
			int nSendLength = mSocket.SendTo(mPackage.buffer, 0, mPackage.Length, SocketFlags.None, remoteEndPoint);
			NetLog.Assert(nSendLength >= Config.nUdpPackageFixedHeadSize, nSendLength);

			if (!UdpNetCommand.orNeedCheck(mPackage.nPackageId))
			{
				ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
			}

			mEndPointPacakge.mPackage = null;
			mEndPointPacakge.mRemoteEndPoint = null;
			ObjectPoolManager.Instance.mNetEndPointPackagePool.recycle(mEndPointPacakge);
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









