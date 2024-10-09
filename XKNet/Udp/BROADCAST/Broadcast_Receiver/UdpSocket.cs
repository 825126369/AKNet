using System;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Udp.BROADCAST.COMMON;

namespace XKNet.Udp.BROADCAST.Receiver
{
    public class UdpSockek_Basic : SocketReceivePeer
	{
		private SocketAsyncEventArgs ReceiveArgs;
		private EndPoint bindEndPoint = null;
		private EndPoint remoteEndPoint = null;
		private Socket mSocket = null;

		public void InitNet(UInt16 ServerPort)
		{
			mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			IPEndPoint iep = new IPEndPoint(IPAddress.Any, ServerPort);
			bindEndPoint = (EndPoint)iep;
			mSocket.Bind(bindEndPoint);

			iep = new IPEndPoint(IPAddress.Broadcast, ServerPort);
			remoteEndPoint = (EndPoint)iep;

			StartReceiveFromAsync();

            NetLog.Log("广播 接收器 初始化 成功 ！");
		}

		private void StartReceiveFromAsync()
		{
			ReceiveArgs = new SocketAsyncEventArgs();
			ReceiveArgs.Completed += ProcessReceive;
			ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
			ReceiveArgs.RemoteEndPoint = remoteEndPoint;
			mSocket.ReceiveFromAsync(ReceiveArgs);
		}

		private void ProcessReceive(object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success && e.BytesTransferred > 0 && mSocket != null)
			{
				int length = e.BytesTransferred;
				NetUdpFixedSizePackage mPackage = mUdpFixedSizePackagePool.Pop();
				Array.Copy(e.Buffer, 0, mPackage.buffer, 0, e.BytesTransferred);

				if (length > 0)
				{
					mPackage.Length = length;
					ReceiveUdpSocketFixedPackage(mPackage);
				}
				else
				{
					mUdpFixedSizePackagePool.recycle(mPackage);
				}

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

		private void DisConnect()
		{
            NetLog.Log("UDP 广播接收器: DisConnect");
		}

		public override void Release()
		{
			base.Release();

			if (mSocket != null)
			{
				mSocket.Close();
				mSocket = null;
			}

			NetLog.Log("--------------- BroadcastReceiver  Release ----------------");
		}

		~UdpSockek_Basic()
		{
			Release();
		}
	}

}