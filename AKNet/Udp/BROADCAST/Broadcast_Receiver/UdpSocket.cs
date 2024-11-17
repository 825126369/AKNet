/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:35
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Udp.BROADCAST.COMMON;

namespace AKNet.Udp.BROADCAST.Receiver
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