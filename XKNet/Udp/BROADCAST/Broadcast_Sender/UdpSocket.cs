using System;
using System.Net;
using System.Net.Sockets;
using XKNetCommon;

namespace XKNetUDP_BROADCAST_Sender
{

    public class SocketUdp_Basic
	{
		private EndPoint remoteSendBroadCastEndPoint = null;
		private Socket mSendBroadCastSocket = null;

		public void InitNet(UInt16 ServerPort)
		{
			mSendBroadCastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			IPEndPoint iep = new IPEndPoint(IPAddress.Broadcast, ServerPort);
			remoteSendBroadCastEndPoint = (EndPoint)iep;

			mSendBroadCastSocket.EnableBroadcast = true;

			NetLog.Log("初始化 广播发送器 成功");
		}

		public void SendNetStream(byte[] msg, int offset, int count)
		{
			try
			{
				mSendBroadCastSocket.SendTo(msg, offset, count, SocketFlags.None, remoteSendBroadCastEndPoint);
			}
			catch { }
		}

		private void CloseNet()
		{
			mSendBroadCastSocket.Close();
		}

		public virtual void Release()
		{
			this.CloseNet();
			NetLog.Log("--------------- BroadcastSender  Release ----------------");
		}
	}
}









