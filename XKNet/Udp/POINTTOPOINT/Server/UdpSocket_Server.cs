using System;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
	internal class SocketUdp_Server
	{
		private SocketAsyncEventArgs ReceiveArgs;
		private Socket mSocket = null;
		
		private NetServer mNetServer = null;
		public SocketUdp_Server(NetServer mNetServer)
		{
			this.mNetServer = mNetServer;
		}

        public void InitNet(string ip, int ServerPort)
		{
			if (mSocket != null)
			{
				this.Reset();
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
			mSocket.ReceiveFromAsync(ReceiveArgs);
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
				int length = e.BytesTransferred;
				NetUdpFixedSizePackage mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
				Array.Copy(e.Buffer, 0, mPackage.buffer, 0, length);
				mPackage.Length = length;

				ClientPeer mPeer = mNetServer.GetClientPeerManager().FindOrAddClient(e.RemoteEndPoint);
				mPeer.mMsgReceiveMgr.ReceiveUdpSocketFixedPackage(mPackage);

				while (!mSocket.ReceiveFromAsync(e))
				{
					ProcessReceive(sender, e);
				}
			}
			else
			{
				DisConnect();
			}
		}

		private void ProcessSend(object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{

			}
			else
			{
				DisConnect();
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

		private void DisConnect()
		{
			//NetLog.LogWarning("Sever: Close");
		}

		private void DisConnectWithException()
		{

		}

		private void CloseSocket()
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

		public void Reset()
		{
			CloseSocket();
			if (ReceiveArgs != null)
			{
				ReceiveArgs.Completed -= IO_Completed;
				ReceiveArgs.Dispose();
				ReceiveArgs = null;
			}
        }

		public void Release()
		{
			this.Reset();
        }
	}

}









