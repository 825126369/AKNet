/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;

namespace XKNet.Udp.BROADCAST.Sender
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









